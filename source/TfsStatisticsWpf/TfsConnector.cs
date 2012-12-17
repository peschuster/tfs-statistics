using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace TfsStatisticsWpf
{
    internal class TfsConnector
    {
        private readonly TfsTeamProjectCollection tfs;

        private readonly ConcurrentDictionary<string, IList<Changeset>> cache = new ConcurrentDictionary<string, IList<Changeset>>();

        public TfsConnector(string uri)
        {
            this.tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(uri));    
        }

        public TfsTeamProjectCollection Tfs
        {
            get { return this.tfs; }
        }

        public IEnumerable<TeamProject> GeTeamProjects()
        {
            var vcs = tfs.GetService<VersionControlServer>();

            return vcs.GetAllTeamProjects(true);
        }

        public IEnumerable<Changeset> GetLatestCheckins(string url, int count)
        {
            var vcs = tfs.GetService<VersionControlServer>();

            var list = vcs.QueryHistory(
                url,
                VersionSpec.Latest,
                0,
                RecursionType.Full,
                null,
                new ChangesetVersionSpec(1),
                VersionSpec.Latest,
                count,
                true,
                false);

            return list.OfType<Changeset>()
                .OrderByDescending(x => x.ChangesetId);
        }

        public Changeset GetLatestCheckin(TeamProject project)
        {
            var list = project.VersionControlServer.QueryHistory(
                project.ServerItem,
                VersionSpec.Latest,
                0,
                RecursionType.Full,
                null,
                new ChangesetVersionSpec(1),
                VersionSpec.Latest,
                1,
                true,
                false);

            Changeset result = list.OfType<Changeset>().FirstOrDefault();

            return result;
        }

        public Task<IList<Changeset>> GetCheckinsAsync(string url)
        {
            return Task<IList<Changeset>>.Run(() =>
            {
                return this.GetCheckins(url);
            });
        }

        public IList<Changeset> GetCheckins(string url)
        {
            string key = "checkins:" + url;

            return this.cache.GetOrAdd(
                key,
                k => this.GetCheckinsCore(url));
        }

        private List<Changeset> GetCheckinsCore(string url)
        {
            var vcs = tfs.GetService<VersionControlServer>();

            var list = vcs.QueryHistory(
                url,
                VersionSpec.Latest,
                0,
                RecursionType.Full,
                null,
                new ChangesetVersionSpec(1),
                VersionSpec.Latest,
                int.MaxValue,
                true,
                false);

            return list.OfType<Changeset>().OrderByDescending(x => x.ChangesetId).ToList();
        }
    }
}
