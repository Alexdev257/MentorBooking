using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(string fileName, Stream fileStream);
        Task DeleteFileFromUrlAsync(string fileUrl);
    }
}
