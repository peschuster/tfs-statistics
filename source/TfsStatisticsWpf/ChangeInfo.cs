namespace TfsStatisticsWpf
{
    public class ChangeInfo : IChangeInfo
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Author { get; set; }

        public int FileCount { get; set; }

        public int AddedLines { get; set; }

        public int RemovedLines { get; set; }
    }
}
