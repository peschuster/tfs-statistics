using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Graphite;
using Graphite.Infrastructure;
using TfsStatisticsWpf.Models;

namespace TfsStatisticsWpf.Export
{
    internal class GraphiteExport : IDisposable
    {
        private readonly ChannelFactory factory;

        private readonly IHistoryMonitoringChannel channel;

        private readonly string metricFormat;

        private readonly string projectName;

        private readonly string baseKey;

        private bool disposed;

        public event ProgressChangedEventHandler ProgressChanged;

        public GraphiteExport(string host, string metricFormat, string projectName, string baseKey)
        {
            this.baseKey = baseKey;
            this.projectName = projectName;
            this.metricFormat = metricFormat;
            string[] addressParts = host.Split(':');
            int port = 2003;

            if (addressParts.Length > 1)
            {
                port = int.Parse(addressParts[1]);
            }

            this.factory = new Graphite.ChannelFactory(new GraphiteConfiguration
                {
                    Address = addressParts.First(),
                    Port = port,
                    Transport = TransportType.Tcp,
                },
                null);

            this.channel = this.factory.CreateHistoryChannel("gauge", "graphite");
        }

        public void Export(ICollection<ChangesetViewModel> items)
        {
            string baseFormat = this.metricFormat
                .Replace("${baseKey}", "{0}")
                .Replace("${projectKey}", "{1}")
                .Replace("${author}", "{2}")
                .Replace("${type}", "{3}");

            Dictionary<string, string> authors = new Dictionary<string, string>();

            foreach (var group in items.GroupBy(x => x.Author))
            {
                string author = group.Key.ToLower().Split('\\').Last().Replace(" ", "_").Replace(".", "_").Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");

                authors.Add(group.Key, author);
            }

            double total = items.Count;
            int pos = 0;
            foreach (var item in items)
            {
                pos++;

                if (item.AddedLines.HasValue || item.RemovedLines.HasValue)
                {
                    this.channel.Report(string.Format(baseFormat, this.baseKey, this.projectName, authors[item.Author], "added"), item.AddedLines ?? 0, item.Datum);
                    this.channel.Report(string.Format(baseFormat, this.baseKey, this.projectName, authors[item.Author], "removed"), item.RemovedLines ?? 0, item.Datum);
                    this.channel.Report(string.Format(baseFormat, this.baseKey, this.projectName, authors[item.Author], "total"), item.AddedLines ?? 0 + item.RemovedLines ?? 0, item.Datum);

                    this.TriggerProgressChange((int)(100 * pos / total));
                }
            }
        }

        public Task ExportAsync(ICollection<ChangesetViewModel> items)
        {
            return Task.Run(() => this.Export(items));
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.disposed)
            {
                if (this.factory != null)
                    this.factory.Dispose();

                this.disposed = true;
            }
        }

        private void TriggerProgressChange(int percentage)
        {
            if (this.ProgressChanged == null)
                return;

            this.ProgressChanged(this, new ProgressChangedEventArgs(percentage, null));
        }

        private class GraphiteConfiguration : Graphite.Configuration.IGraphiteConfiguration
        {
            public int Port { get; set; }

            public string Address { get; set; }

            public TransportType Transport { get; set; }

            public string PrefixKey { get; set; }
        }
    }
}
