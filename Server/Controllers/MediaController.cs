using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.DTOs;
using Server.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly MediaService _mediaService;
        private readonly UserAccessValidator _userAccessValidator;

        public MediaController(MediaService mediaService, UserAccessValidator userAccessValidator)
        {
            _mediaService = mediaService;
            _userAccessValidator = userAccessValidator;
        }

        [SwaggerOperation(
            Summary = "Gets all media files created by the user",
            Description = "GetAll will return an array of media files that belongs to the right user.")]
        [SwaggerResponse(200, "Return an array of media files")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userClaim = _userAccessValidator.GetUserClaimStatus(User);
            _userAccessValidator.ValidateUser(User, userClaim.UserId, needsAdminPrivileges: false);

            if (userClaim.Role is not UserRole.Admin)
            {
                return Ok(await _mediaService.GetAll(userClaim.UserId));
            }
            return Ok(await _mediaService.GetAll());
        }

        [SwaggerOperation(
            Summary = "Get an existing Media file",
            Description = "Get a media file by providing the MediaId and UserId.")]
        [SwaggerResponse(200, "Return the object found")]
        [SwaggerResponse(404, "Media not found")]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            try
            {
                var userClaim = _userAccessValidator.GetUserClaimStatus(User);
                _userAccessValidator.ValidateUser(User, userClaim.UserId, needsAdminPrivileges: false);

                if (userClaim.Role is UserRole.Admin)
                    return Ok(await _mediaService.Get(id));
                else
                    return Ok(await _mediaService.Get(id, userClaim.UserId));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [SwaggerOperation(
            Summary = "Upload a new media file",
            Description = "Upload a new media file and store it to the database and file system.")]
        [SwaggerResponse(200, "Return the created media object")]
        [SwaggerResponse(400, "Invalid file or file too large")]
        [SwaggerResponse(500, "Server error")]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(IFormFile file, string? description = null, string apiHostName = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                var userClaim = _userAccessValidator.GetUserClaimStatus(User);
                _userAccessValidator.ValidateUser(User, userClaim.UserId, needsAdminPrivileges: false);

                if (string.IsNullOrEmpty(apiHostName))
                {
                    return BadRequest("ApiHostName is required.");
                }

                // Save file to disk
                var filePath = await _mediaService.SaveFileAsync(file, userClaim.UserId);

                // Create media record
                var mediaDto = new MediaCreateDto
                {
                    UserId = userClaim.UserId,
                    FileName = Path.GetFileNameWithoutExtension(file.FileName),
                    OriginalFileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    FilePath = filePath,
                    ApiHostName = apiHostName,
                    Description = description
                };

                var media = await _mediaService.Create(mediaDto);
                return Ok(media);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [SwaggerOperation(
            Summary = "Edit an existing Media file",
            Description = "Edit a media file by providing the MediaId and update its metadata.")]
        [SwaggerResponse(200, "Return the updated object")]
        [SwaggerResponse(404, "Media not found")]
        [SwaggerResponse(500, "Server error")]
        [HttpPost("{id}")]
        public async Task<IActionResult> Edit(string id, [FromBody] MediaUpdateDto dto)
        {
            try
            {
                var userClaim = _userAccessValidator.GetUserClaimStatus(User);
                _userAccessValidator.ValidateUser(User, userClaim.UserId, needsAdminPrivileges: false);

                if (userClaim.Role == UserRole.Admin)
                    await _mediaService.Edit(id, dto);
                else
                {
                    var media = await _mediaService.Get(id, userClaim.UserId);
                    if (media is null)
                        return BadRequest("Failed to edit the Media, Permission Denied");

                    await _mediaService.Edit(id, dto);
                }

                return await Get(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [SwaggerOperation(
            Summary = "Delete a Media file",
            Description = "Delete a media file by providing the MediaId and delete it from database and file system.")]
        [SwaggerResponse(200, "Return 200 code if success")]
        [SwaggerResponse(404, "Media not found")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var userClaim = _userAccessValidator.GetUserClaimStatus(User);
                _userAccessValidator.ValidateUser(User, userClaim.UserId, needsAdminPrivileges: false);

                if (userClaim.Role == UserRole.Admin)
                    return Ok(await _mediaService.Delete(id));
                else
                    return Ok(await _mediaService.Delete(id, userClaim.UserId));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}