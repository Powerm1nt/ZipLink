using Microsoft.AspNetCore.Components.Forms;
using Shared.DTOs;
using Shared.Entities;
using System.Net.Http.Json;
using Web.Common;

namespace Web.Services
{
    public class MediaService : IMediaService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthValidator _authValidator;
        private readonly IApiHostService _apiHostService;

        public MediaService(HttpClient httpClient, IAuthValidator authValidator, IApiHostService apiHostService)
        {
            _httpClient = httpClient;
            _authValidator = authValidator;
            _apiHostService = apiHostService;
        }

        public async Task<Media> Upload(IBrowserFile file, string? description, string apiHostName)
        {
            await _authValidator.EnsureIsAuthenticated();

            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 2L * 1024 * 1024 * 1024));
            
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);
            
            if (!string.IsNullOrEmpty(description))
                content.Add(new StringContent(description), "description");
                
            content.Add(new StringContent(apiHostName), "apiHostName");

            var response = await _httpClient.PostAsync("/Media/upload", content);
            response.EnsureSuccessStatusCode();

            var media = await response.Content.ReadFromJsonAsync<Media>();
            return media ?? throw new InvalidOperationException("Media upload response returned null");
        }

        public async Task<IEnumerable<Media>> GetAll()
        {
            await _authValidator.EnsureIsAuthenticated();

            var response = await _httpClient.GetFromJsonAsync<IEnumerable<Media>>("/Media");
            return response ?? throw new InvalidOperationException("Media list response returned null");
        }

        public async Task<Media> Get(string id)
        {
            await _authValidator.EnsureIsAuthenticated();

            var response = await _httpClient.GetFromJsonAsync<Media>($"/Media/{id}");
            return response ?? throw new InvalidOperationException("Media response returned null");
        }

        public async Task<Media> Edit(string id, string? description, string apiHostName)
        {
            await _authValidator.EnsureIsAuthenticated();

            var dto = new MediaUpdateDto
            {
                Description = description,
                ApiHostName = apiHostName
            };

            var response = await _httpClient.PostAsJsonAsync($"/Media/{id}", dto);
            response.EnsureSuccessStatusCode();

            var media = await response.Content.ReadFromJsonAsync<Media>();
            return media ?? throw new InvalidOperationException("Media edit response returned null");
        }

        public async Task<bool> Delete(string id)
        {
            await _authValidator.EnsureIsAuthenticated();

            var response = await _httpClient.DeleteAsync($"/Media/{id}");
            response.EnsureSuccessStatusCode();

            return response.IsSuccessStatusCode;
        }

        public string GetMediaUrl(Media media)
        {
            var hostUrl = _apiHostService.GetApiHostUrl(media.ApiHostName);
            return $"{hostUrl}/media/{media.Id}";
        }

        public string GetDownloadUrl(Media media)
        {
            var hostUrl = _apiHostService.GetApiHostUrl(media.ApiHostName);
            return $"{hostUrl}/media/{media.Id}/download";
        }
    }
}