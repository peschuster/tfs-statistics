using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace TfsStatisticsWpf
{
    internal class TfsAnalytics
    {
        private readonly TfsConnector tfs;

        private readonly IPersistentCache<ChangeInfo> changesCache;

        private readonly ConcurrentDictionary<int, string[]> branchDeletes = new ConcurrentDictionary<int, string[]>();

        public TfsAnalytics(TfsConnector tfs, IPersistentCache<ChangeInfo> changesCache)
        {
            this.changesCache = changesCache;
            this.tfs = tfs;
        }

        public IChangeInfo DiffFiles(Item item1, Item item2)
        {
            if (item1.ItemType != ItemType.File)
                return null;

            DiffItemVersionedFile diffItem1 = new DiffItemVersionedFile(item1, new ChangesetVersionSpec(item1.ChangesetId));
            DiffItemVersionedFile diffItem2 = new DiffItemVersionedFile(item2, new ChangesetVersionSpec(item2.ChangesetId));

            DiffOptions options = new DiffOptions();
            options.UseThirdPartyTool = false;

            options.Flags = DiffOptionFlags.EnablePreambleHandling | DiffOptionFlags.IgnoreWhiteSpace;
            options.OutputType = DiffOutputType.Unified;
            options.TargetEncoding = Console.OutputEncoding;
            options.SourceEncoding = Console.OutputEncoding;

            using (var statistics = new DiffStatisticsReader())
            {
                options.StreamWriter = statistics.CreateWriter();
                options.StreamWriter.AutoFlush = true;

                Difference.DiffFiles(item1.VersionControlServer, diffItem1, diffItem2, options, item1.ServerItem, true);

                return statistics.GetStatistics(null);
            }
        }

        public async void VisualDiff(Change change)
        {
            if (!change.ChangeType.HasFlag(ChangeType.Edit))
                return;

            var others = await this.tfs.GetCheckinsAsync(change.Item.ServerItem);

            Changeset previous = null;

            if (others.Any())
            {
                previous = others
                    .SkipWhile(x => x.ChangesetId != change.Item.ChangesetId)
                    .Skip(1)
                    .FirstOrDefault();
            }

            if (previous == null)
            {
                return;
            }
            else
            {
                Change previousFile = change.ChangeType.HasFlag(ChangeType.Rename) && previous.Changes.Count() == 1
                    ? previous.Changes.Single()
                    : previous.Changes.FirstOrDefault(s => s.Item.ServerItem == change.Item.ServerItem);

                if (previousFile == null)
                    return;

                this.VisualDiff(change.Item, previousFile.Item);
            }
        }

        public void VisualDiff(Item item1, Item item2)
        {
            if (item1.ItemType != ItemType.File)
                return;

            var sourceVersion = new ChangesetVersionSpec(item1.ChangesetId);
            var targetVersion = new ChangesetVersionSpec(item2.ChangesetId);
            
            try
            {
                Difference.VisualDiffItems(
                    item1.VersionControlServer,
                    Difference.CreateTargetDiffItem(item1.VersionControlServer, item2.ServerItem, targetVersion, 0, targetVersion),
                    Difference.CreateTargetDiffItem(item1.VersionControlServer, item1.ServerItem, sourceVersion, 0, sourceVersion),
                    true);
            }
            catch (Exception exception)
            {
            }
        }

        public IChangeInfo GetDiff(Changeset changeset, bool force)
        {
            string key = changeset.ChangesetId.ToString();

            if (!force)
            {
                IChangeInfo stored = this.changesCache.GetById(key);

                if (stored != null)
                    return stored;
            }

            var infos = new List<IChangeInfo>();

            IEnumerable<Change> changes;

            string[] excludedUrls = this.HasBranchDeletes(changeset);

            if (excludedUrls != null)
            {
                // Exclude deletions of complete branches.
                changes = changeset.Changes.Where(c => excludedUrls.All(e => !c.Item.ServerItem.StartsWith(e))).ToList();

                foreach (var change in changeset.Changes.Except(changes))
                {
                    string changeKey = changeset.ChangesetId.ToString() + change.Item.ItemId;
                    this.changesCache.Remove(changeKey);
                }
            }
            else
            {
                changes = changeset.Changes;
            }

            foreach (Change change in changes)
            {
                IChangeInfo info = this.GetDiffCore(changeset, change, force);

                if (info != null)
                {
                    infos.Add(info);
                }
            }

            var result = new ChangeInfo
            {
                Id = changeset.ChangesetId.ToString(),
                Author = changeset.Committer,
                Name = changeset.ChangesetId.ToString(),
                FileCount = infos.Count,
                AddedLines = infos.Sum(i => i.AddedLines),
                RemovedLines = infos.Sum(i => i.RemovedLines),
            };

            this.changesCache.Save(result);

            return result;
        }

        private string[] HasBranchDeletes(Changeset changeset)
        {
            return branchDeletes.GetOrAdd(
                changeset.ChangesetId,
                i =>
                {
                    if (changeset.Changes.Any(this.IsBranchDelete))
                    {
                        return changeset.Changes
                            .Where(this.IsBranchDelete)
                            .Select(c => c.Item.ServerItem)
                            .ToArray();
                    }

                    return null;
                });
        }

        private bool IsBranchDelete(Change change)
        {
            if (!change.ChangeType.HasFlag(ChangeType.Delete) || change.Item.ItemType != ItemType.Folder)
                return false;

            if (change.Item.IsBranch)
                return true;

            string cleanUrl = change.Item.ServerItem.TrimEnd('/');

            if (cleanUrl.IndexOf("/RELEASE/") + "/RELEASE".Length == cleanUrl.LastIndexOf("/")
                || cleanUrl.IndexOf("/DEV/") + "/DEV".Length == cleanUrl.LastIndexOf("/")
                || cleanUrl.IndexOf("/DEVELOPMENT/") + "/DEVELOPMENT".Length == cleanUrl.LastIndexOf("/"))
                return true;

            return false;
        }

        private bool IsPartOfBranchDelete(Changeset changeset, Change change)
        {
            string[] excludedUrls = this.HasBranchDeletes(changeset);

            if (excludedUrls == null)
                return false;
            
            return excludedUrls.Any(e => change.Item.ServerItem.StartsWith(e));
        }

        public IChangeInfo GetDiff(Changeset changeset, Change change, bool force)
        {
            if (change.Item.ItemType != ItemType.File
                || !HasOneFlag(change.ChangeType, ChangeType.Add, ChangeType.Delete, ChangeType.Edit, ChangeType.Undelete, ChangeType.Rollback))
                return null;

            if (this.IsPartOfBranchDelete(changeset, change))
                return null;

            return this.GetDiffCore(changeset, change, force);
        }

        private IChangeInfo GetDiffCore(Changeset changeset, Change change, bool force)
        {
            if (change.Item.ItemType != ItemType.File
                || !HasOneFlag(change.ChangeType, ChangeType.Add, ChangeType.Delete, ChangeType.Edit, ChangeType.Undelete, ChangeType.Rollback))
                return null;

            IChangeInfo info = null;
            string key = changeset.ChangesetId.ToString() + change.Item.ItemId;

            if (!force)
            {
                info = this.changesCache.GetById(key);
            }

            if (info == null)
            {
                info = this.Analyze(change);
            }

            if (info != null)
            {
                ((ChangeInfo)info).Id = key;
                this.changesCache.Save((ChangeInfo)info);
            }
            else
            {
                this.changesCache.Remove(key);
            }

            return info;
        }

        public bool IsLibFile(Item item)
        {
            return item != null 
                && (item.ServerItem.IndexOf("/lib/", StringComparison.InvariantCultureIgnoreCase) > -1 
                    || item.ServerItem.IndexOf("/packages/", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        private bool HasOneFlag(Enum instance, params Enum[] selection)
        {
            foreach (Enum item in selection)
            {
                if ((instance).HasFlag(item))
                    return true;
            }

            return false;
        }
  
        private IChangeInfo Analyze(Change change)
        {
            if (this.IsLibFile(change.Item))
                return null;

            IChangeInfo info = null;

            string url = change.Item.ServerItem;
            Lazy<byte[]> fileContent = new Lazy<byte[]>(() => this.Download(change.Item));

            if (BinaryHelper.IsProbablyBinary(url, fileContent))
                return null;

            if (change.ChangeType.HasFlag(ChangeType.Add)
                || change.ChangeType.HasFlag(ChangeType.Undelete))
            {
                info = new ChangeInfo
                {
                    RemovedLines = 0,
                    AddedLines = this.CountLines(fileContent.Value),
                };
            }
            else if (change.ChangeType.HasFlag(ChangeType.Delete) 
                && !change.ChangeType.HasFlag(ChangeType.SourceRename)
                && !change.ChangeType.HasFlag(ChangeType.Rename))
            {
                info = new ChangeInfo
                {
                    AddedLines = 0,
                    RemovedLines = this.CountLines(fileContent.Value),
                };
            }
            else if (change.ChangeType.HasFlag(ChangeType.Edit) 
                || change.ChangeType.HasFlag(ChangeType.Rollback))
            {
                var others = this.tfs.GetCheckins(url);

                Changeset previous = null;

                if (others.Any())
                {
                    previous = others
                        .SkipWhile(x => x.ChangesetId != change.Item.ChangesetId)
                        .Skip(1)
                        .FirstOrDefault();
                }

                if (previous == null)
                {
                    info = new ChangeInfo
                    {
                        RemovedLines = 0,
                        AddedLines = this.CountLines(fileContent.Value),
                    };
                }
                else
                {
                    Change previousFile = change.ChangeType.HasFlag(ChangeType.Rename) && previous.Changes.Count() == 1
                        ? previous.Changes.Single()
                        : previous.Changes.FirstOrDefault(s => s.Item.ServerItem == url);

                    if (previousFile == null)
                        return null;

                    info = this.DiffFiles(previousFile.Item, change.Item);
                }
            }

            var local = info as ChangeInfo;
            if (local != null)
            {
                local.FileCount = 1;
                local.Name = change.Item.ServerItem;
            }

            return info;
        }

        private int CountLines(byte[] data)
        {
            Encoding encoding = BinaryHelper.GuessEncodingForBytes(data);
            
            string content = encoding.GetString(data);

            return Regex.Split(content, "\r\n|\r|\n").Length;
        }

        private byte[] Download(Item item1)
        {
            using (var stream = item1.DownloadFile())
            {
                using (var memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);

                    return memStream.ToArray();
                }
            }
        }
    }
}
