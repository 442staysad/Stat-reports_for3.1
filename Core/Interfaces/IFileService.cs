using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string baseFolder, string branchName = null, int year = 0, string templateName = null);
        Task<byte[]> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
    }
}