using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDBExample.Models
{
    public class ApplicationDbContext
    {
        public IMongoDatabase Database;

        public GridFSBucket ImagesBucket { get; set; }

        public ApplicationDbContext()
        {
            var connectionString = "mongodb://localhost:27017";
            var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            var client = new MongoClient(settings);
            Database = client.GetDatabase("Images");
            ImagesBucket = new GridFSBucket(Database);
        }
    }

}
