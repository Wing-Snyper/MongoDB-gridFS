using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.GridFS;
using MongoDBExample.Services;

namespace MongoDBExample.Controllers;

[ApiController]
[Route("api/files/{bucketName}")]
[Authorize] // Require basic authentication (or make public based on requirements)
public class FilesController : ControllerBase
{
    private readonly GridFsService _gridFsService;
    private readonly BucketConfigurationService _bucketConfigService;
    private readonly FileCacheService _cacheService;

    public FilesController(
        GridFsService gridFsService, 
        BucketConfigurationService bucketConfigService, 
        FileCacheService cacheService)
    {
        _gridFsService = gridFsService;
        _bucketConfigService = bucketConfigService;
        _cacheService = cacheService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(string bucketName, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        var config = await _bucketConfigService.GetConfigurationAsync(bucketName);
        if (config == null) return NotFound($"Bucket '{bucketName}' does not exist or is not configured.");

        // Content Type validation
        if (config.AllowedContentTypes != null && config.AllowedContentTypes.Any())
        {
            if (!config.AllowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest($"File type '{file.ContentType}' is not allowed in bucket '{bucketName}'.");
            }
        }

        using var stream = file.OpenReadStream();
        var id = await _gridFsService.UploadFileAsync(
            bucketName,
            file.FileName,
            stream,
            file.ContentType,
            config.ChunkSizeBytes
        );

        return Ok(new { FileId = id.ToString() });
    }

    [HttpGet("download/{id}")]
    [AllowAnonymous] // Assuming downloads could be public, or use policies
    public async Task<IActionResult> Download(string bucketName, string id)
    {
        var config = await _bucketConfigService.GetConfigurationAsync(bucketName);
        if (config == null) return NotFound($"Bucket '{bucketName}' not found.");

        if (config.IsCacheEnabled)
        {
            // 1. Try Redis
            var cachedBytes = await _cacheService.GetCachedFileBytesAsync(bucketName, id);
            if (cachedBytes != null)
            {
                var cachedMeta = await _cacheService.GetCachedMetadataAsync(bucketName, id);
                if (cachedMeta != null)
                {
                    return File(cachedBytes, cachedMeta.ContentType, cachedMeta.FileName);
                }
            }
        }

        // 2. Fallback to GridFS
        try
        {
            var (stream, contentType, fileName) = await _gridFsService.DownloadFileAsync(bucketName, id);
            
            // Check if we should cache this file (only cache if it's smaller than the max cacheable size)
            if (config.IsCacheEnabled && stream.Length <= config.MaxCacheableSizeBytes)
            {
                // We need to read the stream into memory to serve it AND cache it
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Fire & forget cache population
                _ = Task.Run(async () =>
                {
                    await _cacheService.SetCachedFileBytesAsync(bucketName, id, fileBytes);
                    await _cacheService.SetCachedMetadataAsync(bucketName, id, contentType, fileName);
                });

                return File(fileBytes, contentType, fileName);
            }

            // For larger files, or no-cache, just stream straight from Mongo to Client
            return File(stream, contentType, fileName);
        }
        catch (GridFSFileNotFoundException)
        {
            return NotFound("File not found in GridFS.");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string bucketName, string id)
    {
        try
        {
            await _gridFsService.DeleteFileAsync(bucketName, id);
            
            // Invalidate cache
            await _cacheService.RemoveCachedFileAsync(bucketName, id);

            return NoContent();
        }
        catch (GridFSFileNotFoundException)
        {
            return NotFound("File not found.");
        }
    }
}
