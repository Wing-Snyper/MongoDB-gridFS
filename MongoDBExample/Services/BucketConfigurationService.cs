using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using MongoDBExample.Models;
using MongoDBExample.Settings;

namespace MongoDBExample.Services;

public class BucketConfigurationService
{
    private readonly IMongoCollection<BucketConfiguration> _configCollection;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "BucketConfig_";

    public BucketConfigurationService(MongoDbSettings settings, IMemoryCache cache)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _configCollection = database.GetCollection<BucketConfiguration>("GridFsBucketConfigs");
        _cache = cache;
        
        EnsureDefaultBuckets();
    }

    private void EnsureDefaultBuckets()
    {
        // On startup, we can ensure that default buckets "images" and "documents" exist.
        if (!_configCollection.AsQueryable().Any(b => b.BucketName == "images"))
        {
            _configCollection.InsertOne(new BucketConfiguration
            {
                BucketName = "images",
                IsCacheEnabled = true,
                MaxCacheableSizeBytes = 5 * 1024 * 1024,
                AllowedContentTypes = new List<string> { "image/jpeg", "image/png", "image/gif", "image/webp" }
            });
        }
        
        if (!_configCollection.AsQueryable().Any(b => b.BucketName == "documents"))
        {
            _configCollection.InsertOne(new BucketConfiguration
            {
                BucketName = "documents",
                IsCacheEnabled = false, // Maybe we don't cache docs by default or increase limit
                AllowedContentTypes = new List<string> { "application/pdf", "text/plain" }
            });
        }
    }

    public async Task<BucketConfiguration?> GetConfigurationAsync(string bucketName)
    {
        string cacheKey = $"{CacheKeyPrefix}{bucketName.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out BucketConfiguration? cachedConfig))
        {
            return cachedConfig;
        }

        var filter = Builders<BucketConfiguration>.Filter.Eq(x => x.BucketName, bucketName.ToLowerInvariant());
        var config = await _configCollection.Find(filter).FirstOrDefaultAsync();

        if (config != null)
        {
            // Cache for 1 hour, or whatever is appropriate for native serving
            _cache.Set(cacheKey, config, TimeSpan.FromHours(1));
        }

        return config;
    }

    public async Task CreateOrUpdateConfigurationAsync(BucketConfiguration config)
    {
        config.BucketName = config.BucketName.ToLowerInvariant();
        var filter = Builders<BucketConfiguration>.Filter.Eq(x => x.BucketName, config.BucketName);
        var options = new ReplaceOptions { IsUpsert = true };
        
        await _configCollection.ReplaceOneAsync(filter, config, options);
        
        // Invalidate cache
        _cache.Remove($"{CacheKeyPrefix}{config.BucketName}");
    }

    public async Task<List<BucketConfiguration>> GetAllConfigurationsAsync()
    {
        return await _configCollection.Find(_ => true).ToListAsync();
    }
}
