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
        private readonly StorageClient? _storageClient;
        private readonly string _bucketName;

        public FirebaseService(IConfiguration configuration)
        {
            _bucketName = configuration["Firebase:BucketName"] ?? string.Empty;

            var credential = TryLoadGoogleCredential(configuration);
            if (credential == null)
            {
                Console.WriteLine(
                    "[WARNING] FirebaseService: no credentials. Set Firebase__CredentialJson, Firebase__CredentialJsonBase64, Firebase__CredentialPath (existing file), or GOOGLE_APPLICATION_CREDENTIALS. Upload/delete will fail until configured.");
                _storageClient = null;
                return;
            }

            _storageClient = StorageClient.Create(credential);
        }

        /// <summary>
        /// Thứ tự: JSON trong config (Render secret) → Base64 JSON → file CredentialPath → biến môi trường GOOGLE_APPLICATION_CREDENTIALS (đường dẫn file).
        /// </summary>
        private static GoogleCredential? TryLoadGoogleCredential(IConfiguration configuration)
        {
            var json = configuration["Firebase:CredentialJson"];
            if (!string.IsNullOrWhiteSpace(json))
                return GoogleCredential.FromJson(json.Trim());

            var b64 = configuration["Firebase:CredentialJsonBase64"];
            if (!string.IsNullOrWhiteSpace(b64))
            {
                var bytes = Convert.FromBase64String(b64.Trim());
                return GoogleCredential.FromJson(Encoding.UTF8.GetString(bytes));
            }

            var credentialPath = configuration["Firebase:CredentialPath"];
            if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
                return GoogleCredential.FromFile(credentialPath);

            const string defaultLocalFileName = "mentorbookingproject-firebase-adminsdk-fbsvc-a7290ef766.json";
            if (File.Exists(defaultLocalFileName))
                return GoogleCredential.FromFile(defaultLocalFileName);

            var gac = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (!string.IsNullOrWhiteSpace(gac) && File.Exists(gac))
                return GoogleCredential.FromFile(gac);

            return null;
        }

        private void EnsureStorageConfigured()
        {
            if (_storageClient == null)
                throw new InvalidOperationException(
                    "Firebase Storage chưa được cấu hình. Trên Render: thêm secret Firebase__CredentialJson (toàn bộ JSON service account) hoặc Firebase__CredentialJsonBase64 (file JSON đã base64), và Firebase__BucketName. Không cần đẩy file .json lên repo.");

            if (string.IsNullOrWhiteSpace(_bucketName))
                throw new InvalidOperationException("Firebase Storage: thiếu Firebase__BucketName.");
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
                EnsureStorageConfigured();

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
                var storageObject = await _storageClient!.UploadObjectAsync(
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

                await _storageClient!.UpdateObjectAsync(storageObject);

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
                EnsureStorageConfigured();

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

                await _storageClient!.DeleteObjectAsync(bucketName, objectName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting file from Firebase Storage.", ex);
            }
        }
    }
}
