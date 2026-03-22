using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDBExample.Models;

public class BucketConfiguration
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public int ChunkSizeBytes { get; set; } = 261120; // 255 KB default
    
    public bool IsCacheEnabled { get; set; } = true;
    
    public long MaxCacheableSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB default

    public List<string> AllowedContentTypes { get; set; } = new();
}
