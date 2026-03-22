using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace MongoDBExample.Services;

public class FileCacheService
{
    private readonly IDistributedCache _redisCache;
    
    public FileCacheService(IDistributedCache redisCache)
    {
        _redisCache = redisCache;
    }

    private string GetCacheKey(string bucketName, string fileId) => $"FileCache_{bucketName}_{fileId}";

    public async Task<byte[]?> GetCachedFileBytesAsync(string bucketName, string fileId)
    {
        return await _redisCache.GetAsync(GetCacheKey(bucketName, fileId));
    }

    public async Task SetCachedFileBytesAsync(string bucketName, string fileId, byte[] fileBytes)
    {
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // Cache for max 24 hrs
        };

        await _redisCache.SetAsync(GetCacheKey(bucketName, fileId), fileBytes, options);
    }
    
    // Also cache the metadata to reconstruct the response completely from Redis
    public async Task<FileMetadataCache?> GetCachedMetadataAsync(string bucketName, string fileId)
    {
        var metaStr = await _redisCache.GetStringAsync($"{GetCacheKey(bucketName, fileId)}_meta");
        if (string.IsNullOrEmpty(metaStr)) return null;
        
        return JsonSerializer.Deserialize<FileMetadataCache>(metaStr);
    }
    
    public async Task SetCachedMetadataAsync(string bucketName, string fileId, string contentType, string fileName)
    {
        var meta = new FileMetadataCache { ContentType = contentType, FileName = fileName };
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(1),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
        await _redisCache.SetStringAsync($"{GetCacheKey(bucketName, fileId)}_meta", JsonSerializer.Serialize(meta), options);
    }

    public async Task RemoveCachedFileAsync(string bucketName, string fileId)
    {
        await _redisCache.RemoveAsync(GetCacheKey(bucketName, fileId));
        await _redisCache.RemoveAsync($"{GetCacheKey(bucketName, fileId)}_meta");
    }
}

public class FileMetadataCache
{
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
