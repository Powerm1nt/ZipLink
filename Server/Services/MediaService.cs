using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Server.Common;
using Server.Contexts;
using Shared.DTOs;
using Shared.Entities;

namespace Server.Services
{
    public class MediaService : GenericService<AppDbContext, Media, MediaCreateDto, MediaUpdateDto>
    {
        private readonly IApiHostService _apiHostService;
        private readonly IConfiguration _configuration;

        public MediaService(AppDbContext context, IMapper mapper, IApiHostService apiHostService, IConfiguration configuration) 
            : base(context, mapper)
        {
            _apiHostService = apiHostService;
            _configuration = configuration;
        }

        public override async Task<Media> Create(MediaCreateDto dto)
        {
            // Validate that the ApiHostName is configured
            if (!_apiHostService.IsValidApiHost(dto.ApiHostName))
            {
                throw new ArgumentException($"Invalid API Host: {dto.ApiHostName}");
            }

            // Validate file size
            var maxFileSize = _configuration.GetValue<long>("MediaUpload:MaxFileSizeBytes");
            if (dto.FileSize > maxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize} bytes");
            }

            return await base.Create(dto);
        }

        public override async Task<Media> Edit(string id, MediaUpdateDto dto)
        {
            // Validate that the ApiHostName is configured
            if (!_apiHostService.IsValidApiHost(dto.ApiHostName))
            {
                throw new ArgumentException($"Invalid API Host: {dto.ApiHostName}");
            }

            return await base.Edit(id, dto);
        }

        public override async Task<Media> Get(string id)
        {
            var entity = await _context.MediaFiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (entity is null)
                throw new KeyNotFoundException($"Media with ID {id} not found.");

            return entity;
        }

        public async Task<Media> Get(string id, string userId)
        {
            var entity = await _context.MediaFiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(m => m.User.Id == userId && m.Id == id);

            if (entity is null)
                throw new KeyNotFoundException($"Media with ID {id} not found.");

            return entity;
        }

        public async Task<IEnumerable<Media>> GetAll(string userId)
        {
            return await _context.MediaFiles
                .Include(e => e.User)
                .Where(m => m.User.Id == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> Delete(string id, string userId)
        {
            var entity = await _context.MediaFiles
                .FirstOrDefaultAsync(e => e.User.Id == userId && e.Id == id);

            if (entity is null)
                throw new KeyNotFoundException($"Media with ID {id} not found.");

            // Delete physical file
            var fullPath = Path.Combine(GetUploadPath(), entity.FilePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            _context.MediaFiles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string userId)
        {
            var allowedExtensions = _configuration.GetSection("MediaUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var allowedMimeTypes = _configuration.GetSection("MediaUpload:AllowedMimeTypes").Get<string[]>() ?? Array.Empty<string>();
            
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                throw new ArgumentException($"File type '{fileExtension}' is not allowed.");
            }

            if (!allowedMimeTypes.Contains(file.ContentType))
            {
                throw new ArgumentException($"MIME type '{file.ContentType}' is not allowed.");
            }

            var maxFileSize = _configuration.GetValue<long>("MediaUpload:MaxFileSizeBytes");
            if (file.Length > maxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {maxFileSize} bytes");
            }

            var uploadPath = GetUploadPath();
            var userFolder = Path.Combine(uploadPath, userId);
            
            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(userFolder, fileName);
            var relativePath = Path.Combine(userId, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return relativePath;
        }

        public string GetFullMediaUrl(Media media)
        {
            if (media is null)
                throw new ArgumentNullException(nameof(media));

            var hostUrl = _apiHostService.GetApiHostUrl(media.ApiHostName);
            return $"{hostUrl}/media/{media.Id}";
        }

        private string GetUploadPath()
        {
            var storagePath = _configuration.GetValue<string>("MediaUpload:StoragePath") ?? "uploads";
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), storagePath);
            
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        public string GetPhysicalFilePath(Media media)
        {
            return Path.Combine(GetUploadPath(), media.FilePath);
        }
    }
}