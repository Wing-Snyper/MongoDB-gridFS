using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MongoDBExample.Settings;

namespace MongoDBExample.Services;

public class GridFsService
{
    private readonly IMongoDatabase _database;

    public GridFsService(MongoDbSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    private IGridFSBucket GetBucket(string bucketName, int chunkSizeBytes = 261120)
    {
        return new GridFSBucket(_database, new GridFSBucketOptions
        {
            BucketName = bucketName,
            ChunkSizeBytes = chunkSizeBytes
        });
    }

    public async Task<ObjectId> UploadFileAsync(
        string bucketName, 
        string fileName, 
        Stream stream, 
        string contentType,
        int chunkSizeBytes)
    {
        var bucket = GetBucket(bucketName, chunkSizeBytes);
        
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "ContentType", contentType },
                { "OriginalFileName", fileName }
            }
        };

        return await bucket.UploadFromStreamAsync(fileName, stream, options);
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadFileAsync(string bucketName, string idStr)
    {
        var bucket = GetBucket(bucketName);
        var objectId = new ObjectId(idStr);
        
        var stream = await bucket.OpenDownloadStreamAsync(objectId);
        
        var contentType = stream.FileInfo.Metadata.Contains("ContentType") 
            ? stream.FileInfo.Metadata["ContentType"].AsString 
            : "application/octet-stream";
            
        var fileName = stream.FileInfo.Metadata.Contains("OriginalFileName")
            ? stream.FileInfo.Metadata["OriginalFileName"].AsString
            : stream.FileInfo.Filename;

        return (stream, contentType, fileName);
    }

    public async Task DeleteFileAsync(string bucketName, string idStr)
    {
        var bucket = GetBucket(bucketName);
        await bucket.DeleteAsync(new ObjectId(idStr));
    }
}
