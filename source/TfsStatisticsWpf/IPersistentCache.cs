namespace TfsStatisticsWpf
{
    public interface IPersistentCache<T>
    {
        T GetById(string id);

        void Save(T item);

        void Remove(string id);
    }
}
