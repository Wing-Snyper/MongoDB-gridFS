using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDBExample.Models;
using MongoDBExample.Services;

namespace MongoDBExample.Controllers;

[ApiController]
[Route("api/admin/buckets")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly BucketConfigurationService _configService;

    public AdminController(BucketConfigurationService configService)
    {
        _configService = configService;
    }

    [HttpGet]
    public async Task<IActionResult> ListBuckets()
    {
        var configs = await _configService.GetAllConfigurationsAsync();
        return Ok(configs);
    }

    [HttpGet("{bucketName}")]
    public async Task<IActionResult> GetBucketConfig(string bucketName)
    {
        var config = await _configService.GetConfigurationAsync(bucketName);
        if (config == null) return NotFound("Bucket configuration not found.");
        
        return Ok(config);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateBucket([FromBody] BucketConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.BucketName))
        {
            return BadRequest("Bucket name is required.");
        }

        await _configService.CreateOrUpdateConfigurationAsync(config);
        
        return Ok("Bucket configuration saved successfully.");
    }
}
