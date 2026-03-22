# MongoDB GridFS .NET 10 Microservice

A state-of-the-art, high-performance, and headless .NET 10 Web API microservice for storing and serving files securely using MongoDB's GridFS.

## Overview
This service elevates GridFS file storage by introducing an **In-Memory Cache for Bucket Configurations** and a **Redis Cache-Aside** layer for serving hot files, achieving near-native file serving speeds without overwhelming the MongoDB infrastructure. It is designed to act as a unified file backend for multiple applications or front-end services.

## Architecture Highlights
- **.NET 10 Web API**: Utilizing the modern minimal hosting model and dependency injection.
- **Dynamic GridFS Buckets**: Admin users can create and manage multiple targeted file buckets (e.g., `images`, `documents`, `profile_pictures`) dynamically without code changes.
- **In-Memory Configuration Caching**: Bucket configurations are cached locally in memory at runtime to ensure O(1) config resolution and avoid blocking database roundtrips during file requests.
- **Redis File Caching (Cache-Aside)**: Files smaller than a defined `MaxCacheableSizeBytes` limit are lazily cached into Redis. Subsequent hits serve directly from Redis cache, saving MongoDB bandwidth and drastically lowering TTFB (Time To First Byte).
- **OIDC/JWT Authentication**: Integration with JWT-based Authentication to secure file uploads and Admin operations via role-based policies.
- **Metadata Management**: Properly logs, records, and streams accurate `Content-Type` and original `FileName` associated with GridFS Chunks.

## Getting Started

### Prerequisites
- .NET 10 SDK
- MongoDB Server (Local or Atlas)
- Redis Server (Local or Managed)
- Configure `appsettings.json` with your corresponding Connection Strings.

### Running the API
1. Open terminal at the project root `MongoDBExample/`.
2. Run `dotnet restore`
3. Run `dotnet run` or `dotnet watch` to start the .NET 10 host.
4. Navigate to `https://localhost:xxxx/swagger` to explore the API endpoints in the Swagger interface.

## Endpoints

### 1. Admin API (Secured via `AdminOnly` Policy)
- `GET /api/admin/buckets`: Lists all dynamic GridFS bucket configurations.
- `GET /api/admin/buckets/{bucketName}`: Retrieves details of a specific bucket.
- `POST /api/admin/buckets`: Creates or updates a dynamic bucket specifying parameters like `ChunkSizeBytes`, `IsCacheEnabled`, `MaxCacheableSizeBytes`, and restricting `AllowedContentTypes`.

### 2. Files API
- `POST /api/files/{bucketName}/upload`: (Requires Auth) Accepts an `multipart/form-data` payload containing an `IFormFile`. Automatically validates against the specified bucket's constraints.
- `GET /api/files/{bucketName}/download/{id}`: Returns the requested file stream. If `IsCacheEnabled` is true for this bucket, it checks Redis. Cache misses are streamed from GridFS and recursively cached to Redis up to the size limit.
- `DELETE /api/files/{bucketName}/{id}`: (Requires Auth) Purges the file from GridFS and invalidates its Redis chunks.

## Performance Tuning
You can modify the caching approach granularly:
- An **"images"** bucket containing small avatars might have `IsCacheEnabled = true` and `MaxCacheableSizeBytes = 5MB` to maintain ultra-fast UI loading speeds.
- A **"documents"** bucket handling massive multi-gigabyte zip files or videos could be set to `IsCacheEnabled = false` to avoid overflowing Redis and rely strictly on GridFS streaming.
