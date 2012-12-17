using System;
using System.IO;
using System.Linq;
using System.Text;

namespace TfsStatisticsWpf
{
    public class DiffStatisticsReader : IDisposable
    {
        private readonly MemoryStream stream = new MemoryStream();

        private bool disposed;

        public StreamWriter CreateWriter()
        { 
            return new StreamWriter(this.stream, Encoding.UTF8);            
        }

        public IChangeInfo GetStatistics(string author)
        {
            string content = Encoding.UTF8.GetString(stream.ToArray());

            return new ChangeInfo
            {
                Author = author,
                AddedLines = content.Split('\n').Count(s => s.StartsWith("+") && !s.StartsWith("++")),
                RemovedLines = content.Split('\n').Count(s => s.StartsWith("-") && !s.StartsWith("--")),
            };
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
                if (this.stream != null)
                    this.stream.Dispose();

                this.disposed = true;
            }
        }

    }
}
