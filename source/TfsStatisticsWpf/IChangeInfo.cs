namespace TfsStatisticsWpf
{
    public interface IChangeInfo
    {
        string Id { get; }

        string Name { get; }

        string Author { get; }

        int FileCount { get; }

        int AddedLines { get; }
        
        int RemovedLines { get; }
    }
}
