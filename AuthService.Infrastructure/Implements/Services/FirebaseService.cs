using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Shared.Contracts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Implements.Services
{
    public class FirebaseService : IStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;

        public FirebaseService(IConfiguration configuration)
        {
            var credentialPath = configuration["Firebase:CredentialPath"];
            _bucketName = configuration["Firebase:BucketName"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(credentialPath) || !File.Exists(credentialPath))
            {
                Console.WriteLine($"[WARNING] FirebaseService: credential file not found ('{credentialPath}'). Upload/Delete will throw at runtime.");
                _storageClient = null!;
                return;
            }

            var credential = GoogleCredential.FromFile(credentialPath);
            _storageClient = StorageClient.Create(credential);
        }
        public async Task<string> UploadFileAsync(
        string fileName,
        Stream fileStream)
        {
            //await _storageClient.UploadObjectAsync(
            //    _bucketName,
            //    fileName,
            //    contentType,
            //    fileStream);
            //await _storageClient.UpdateObjectAsync(new Google.Apis.Storage.v1.Data.Object
            //{
            //    Bucket = _bucketName,
            //    Name = fileName,
            //    Acl = new List<ObjectAccessControl>
            //    {
            //        new ObjectAccessControl
            //        {
            //            Entity = "allUsers",
            //            Role = "READER"
            //        }
            //    }
            //});

            //return $"https://storage.googleapis.com/{_bucketName}/{fileName}";


            //var extension = Path.GetExtension(fileName).ToLower();
            //var safeFileName = $"avatars/{Guid.NewGuid()}{extension}";

            //// 2️⃣ Set Content-Type chuẩn (KHÔNG tin client)
            //string contentType = extension switch
            //{
            //    ".png" => "image/png",
            //    ".jpg" => "image/jpeg",
            //    ".jpeg" => "image/jpeg",
            //    ".webp" => "image/webp",
            //    ".gif" => "image/gif",
            //    _ => "application/octet-stream"
            //};

            //// 3️⃣ Upload với metadata chuẩn
            //var obj = new Google.Apis.Storage.v1.Data.Object
            //{
            //    Bucket = _bucketName,
            //    Name = safeFileName,
            //    ContentType = contentType,
            //    ContentDisposition = "inline"
            //};

            //await _storageClient.UploadObjectAsync(obj, fileStream);

            //// 4️⃣ Set public
            //await _storageClient.UpdateObjectAsync(new Google.Apis.Storage.v1.Data.Object
            //{
            //    Bucket = _bucketName,
            //    Name = safeFileName,
            //    Acl = new List<ObjectAccessControl>
            //    {
            //        new ObjectAccessControl
            //        {
            //            Entity = "allUsers",
            //            Role = "READER"
            //        }
            //    }
            //});

            //// 5️⃣ Trả về URL đúng chuẩn Firebase (KHÔNG dùng storage.googleapis.com)
            //return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(safeFileName)}?alt=media";

            try
            {
                // 1️ Tạo tên file duy nhất
                var extension = Path.GetExtension(fileName).ToLower();
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";

                // 2️ Check có phải ảnh không
                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp"
        };

                if (!allowedExtensions.Contains(extension))
                    throw new NotSupportedException("Unsupported file type.");

                // 3️ Tự set Content-Type (KHÔNG tin client)
                string contentType = extension switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".tiff" => "image/tiff",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                var objectName = $"Avatar/{uniqueFileName}";

                // 4️ Upload
                var storageObject = await _storageClient.UploadObjectAsync(
                    _bucketName,
                    objectName,
                    contentType,
                    fileStream
                );

                // 5️ Set public
                storageObject.Acl = new List<ObjectAccessControl>
        {
            new ObjectAccessControl
            {
                Entity = "allUsers",
                Role = "READER"
            }
        };

                await _storageClient.UpdateObjectAsync(storageObject);

                // 6️ Trả URL chuẩn Firebase (KHÔNG bị download)
                return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error uploading file to Firebase Storage.", ex);
            }
        }

        public async Task DeleteFileFromUrlAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileUrl))
                    throw new ArgumentException("File URL is required.");

                var uri = new Uri(fileUrl);

                // Lấy path sau /b/
                // Ví dụ: /v0/b/mentorbookingproject.appspot.com/o/avatars%2FRevoland%2Fabc.jpg
                var segments = uri.AbsolutePath.Split('/');

                var bucketIndex = Array.IndexOf(segments, "b") + 1;
                var objectIndex = Array.IndexOf(segments, "o") + 1;

                if (bucketIndex == 0 || objectIndex == 0)
                    throw new InvalidOperationException("Invalid Firebase Storage URL.");

                var bucketName = segments[bucketIndex];

                var encodedObjectName = string.Join("/", segments.Skip(objectIndex));

                // Remove query string nếu có
                encodedObjectName = encodedObjectName.Split('?')[0];

                // Decode %2F thành /
                var objectName = Uri.UnescapeDataString(encodedObjectName);

                await _storageClient.DeleteObjectAsync(bucketName, objectName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting file from Firebase Storage.", ex);
            }
        }
    }
}
