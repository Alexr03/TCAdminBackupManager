using System;
using System.IO;
using System.Threading.Tasks;
using Minio;
using TCAdmin.SDK.Misc;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Controllers;
using TCAdminBackupManager.Exceptions;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.GameHosting.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupProviders
{
    public class S3Provider : BackupSolution
    {
        public readonly MinioClient MinioClient;
        private readonly string _region;

        public S3Provider()
        {
            var settings = new BackupProvider().FindByType(typeof(S3Provider)).Configuration
                .Parse<S3ProviderConfiguration>();
            var host = settings.Host;
            var username = settings.AccessId;
            var password = settings.AccessSecret;
            _region = settings.Region;

            MinioClient = new MinioClient(host, username, password, _region).WithSSL();
            MinioClient.SetTraceOn();
            this.AllowsDirectDownload = true;
        }

        public override async Task<BackupResponse> Backup(Backup backup, BackupRequest request)
        {
            var bucket = $"backups-service-{backup.ServiceId}";
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var combinePath = FileSystem.FixAbsoluteFilePath(
                FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, request.Path,
                    Compress(backup, request)), server.OperatingSystem);
            var fileSize = fileSystemService.GetFileSize(combinePath);
            BackupManagerController.ThrowExceedQuota(backup, request, fileSize);
            backup.FileSize = fileSize;

            if (!await MinioClient.BucketExistsAsync(bucket))
            {
                await MinioClient.MakeBucketAsync(bucket, _region);
            }

            var contents = fileSystemService.ReadBinary(combinePath);
            Stream stream = new MemoryStream(contents);
            await MinioClient.PutObjectAsync(bucket, backup.ZipFullName, stream, stream.Length, "application/zip");
            return new BackupResponse(true);
        }

        public override Task<byte[]> DownloadBytes(Backup backup)
        {
            throw new NotImplementedException();
        }

        public override async Task<string> DirectDownloadLink(Backup backup)
        {
            var bucket = $"backups-service-{backup.ServiceId}";
            if (!await MinioClient.BucketExistsAsync(bucket))
            {
                throw new Exception("No bucket exists.");
            }

            var downloadUrl = await MinioClient.PresignedGetObjectAsync(bucket, backup.ZipFullName, 300);
            return downloadUrl;
        }

        public override async Task<bool> Delete(Backup backup)
        {
            var bucket = $"backups-service-{backup.ServiceId}";

            if (!await MinioClient.BucketExistsAsync(bucket))
            {
                throw new Exception("No bucket exists.");
            }

            await MinioClient.RemoveObjectAsync(bucket, backup.ZipFullName);
            return true;
        }
    }
}