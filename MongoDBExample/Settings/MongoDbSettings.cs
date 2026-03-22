namespace MongoDBExample.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "GridFsStore";
}
