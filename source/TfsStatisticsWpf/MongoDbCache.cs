using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace TfsStatisticsWpf
{
    internal abstract class MongoDbCache
    {
        public const string UserName = "tfsStatistics";

        public const string Password = "128_tfs!";

        public const string DatabaseName = "tfsStats";
    }

    internal class MongoDbCache<T> : MongoDbCache, IPersistentCache<T>
    {
        private readonly MongoClient mongo;

        private readonly MongoCollection<T> collection;

        private readonly MongoDatabase db;

        public MongoDbCache(string connectionString, string dbName, string collectionName)
        {
            this.mongo = new MongoClient(connectionString);

            this.db = this.mongo
                .GetServer()
                .GetDatabase(dbName, new MongoCredentials(UserName, Password));

            if (!this.db.CollectionExists(collectionName))
            {
                this.db.CreateCollection(collectionName);
            }

            this.collection = this.db.GetCollection<T>(collectionName);
        }

        public T GetById(string id)
        {
            var query = Query.EQ("_id", id);

            return this.collection.FindOne(query);
        }

        public void Save(T item)
        {
            this.collection.Save(item);
        }

        public void Remove(string id)
        {
            var query = Query.EQ("_id", id);

            this.collection.Remove(query);
        }
    }
}
