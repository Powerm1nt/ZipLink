using Microsoft.AspNetCore.Components.Forms;
using Shared.Entities;

namespace Web.Services
{
    public interface IMediaService
    {
        Task<Media> Upload(IBrowserFile file, string? description, string apiHostName);
        Task<IEnumerable<Media>> GetAll();
        Task<Media> Get(string id);
        Task<Media> Edit(string id, string? description, string apiHostName);
        Task<bool> Delete(string id);
        string GetMediaUrl(Media media);
        string GetDownloadUrl(Media media);
    }
}