using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace FreeswitchListenerServer.Class
{
    internal class MongoDb
    {
        public readonly IMongoDatabase db;
        public MongoDb(string dbName, string host, string port)
        {
            try
            {
                var client = new MongoClient($"mongodb://{host}:{port}");
                db = client.GetDatabase(dbName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task CreateCollection(string name)
        {
            if (db != null)
                await db.CreateCollectionAsync(name);
        }

        public async Task Insert<T>(IMongoCollection<T> collection, T newItem)
        {
            await collection.InsertOneAsync(newItem);
        }

        public async Task InsertMany<T>(IMongoCollection<T> collection, List<T> list)
        {
            await collection.InsertManyAsync(list);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return db?.GetCollection<T>(name);
        }
    }
}
