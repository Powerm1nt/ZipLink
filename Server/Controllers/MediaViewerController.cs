using Microsoft.AspNetCore.Mvc;
using Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Server.Controllers
{
    [ApiController]
    [Route("/media")]
    public class MediaViewerController : ControllerBase
    {
        private readonly MediaService _mediaService;
        private readonly IApiHostService _apiHostService;

        public MediaViewerController(MediaService mediaService, IApiHostService apiHostService)
        {
            _mediaService = mediaService;
            _apiHostService = apiHostService;
        }

        [SwaggerOperation(
            Summary = "Serve media file",
            Description = "Get a media file by providing the MediaId and serve it directly.")]
        [SwaggerResponse(200, "Return the media file")]
        [SwaggerResponse(404, "Media not found")]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                var media = await _mediaService.Get(id);
                if (media == null)
                    return NotFound("Media not found");

                // Validate that the request comes from a configured API host
                var requestHost = Request.Host.Value;
                var validHosts = _apiHostService.GetApiHosts();
                var isValidHost = validHosts.Any(host => 
                {
                    var hostUri = new Uri(host.Url);
                    return hostUri.Host.Equals(requestHost, StringComparison.OrdinalIgnoreCase) ||
                           $"{hostUri.Host}:{hostUri.Port}".Equals(requestHost, StringComparison.OrdinalIgnoreCase);
                });

                if (!isValidHost)
                {
                    return NotFound("Media not found");
                }

                var filePath = _mediaService.GetPhysicalFilePath(media);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Media file not found on disk");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                // Set appropriate headers
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{media.OriginalFileName}\"");
                Response.Headers.Add("Cache-Control", "public, max-age=31536000"); // Cache for 1 year
                
                return File(fileBytes, media.ContentType);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error serving media file");
            }
        }

        [SwaggerOperation(
            Summary = "Download media file",
            Description = "Download a media file by providing the MediaId.")]
        [SwaggerResponse(200, "Return the media file as download")]
        [SwaggerResponse(404, "Media not found")]
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(string id)
        {
            try
            {
                var media = await _mediaService.Get(id);
                if (media == null)
                    return NotFound("Media not found");

                var filePath = _mediaService.GetPhysicalFilePath(media);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound("Media file not found on disk");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                
                // Force download
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{media.OriginalFileName}\"");
                
                return File(fileBytes, media.ContentType, media.OriginalFileName);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "Error downloading media file");
            }
        }
    }
}