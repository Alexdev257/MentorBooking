using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedContracts.Interfaces
{
    public interface IFileStorageServices
    {
        Task<string> UploadAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default);

        Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
