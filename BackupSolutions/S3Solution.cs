using System;
using System.IO;
using System.Threading.Tasks;
using Minio;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.VirtualFileSystem;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.GameHosting.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupSolutions
{
    public class S3Solution : BackupSolution
    {
        public readonly MinioClient MinioClient;
        private readonly string _region;

        public S3Solution()
        {
            var settings = new BackupProvider().FindByType(typeof(S3Solution)).Configuration.Parse<S3ProviderConfiguration>();
            var host = settings.Host;
            var username = settings.AccessId;
            var password = settings.AccessSecret;
            _region = settings.Region;

            MinioClient = new MinioClient(host, username, password, _region).WithSSL();
            MinioClient.SetTraceOn();
            this.AllowsDirectDownload = true;
        }

        public override async Task<bool> BackupFile(Backup backup, string targetPath)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var baseDir = FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory,
                Path.GetDirectoryName(targetPath));
            var compressedFileName = fileSystemService.CompressFiles(baseDir, new[] {Path.GetFileName(targetPath)},
                ObjectXml.ObjectToXml(new VirtualDirectorySecurity()), 5000000000);
            var combinePath = FileSystem.CombinePath(server.OperatingSystem, baseDir, compressedFileName);
            var contents = fileSystemService.ReadFile(combinePath);
            var bucket = $"backups-service-{backup.ServiceId}";
            if (!await MinioClient.BucketExistsAsync(bucket))
            {
                await MinioClient.MakeBucketAsync(bucket, _region);
            }

            Stream stream = new MemoryStream(contents);
            await MinioClient.PutObjectAsync(bucket, backup.Guid + ".zip", stream, stream.Length, "application/zip");
            return true;
        }

        public override Task<bool> BackupDirectory(Backup backup, string targetPath)
        {
            throw new NotImplementedException();
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

            var downloadUrl = await MinioClient.PresignedGetObjectAsync(bucket, backup.Guid + ".zip", 300);
            return downloadUrl;
        }

        public override async Task<bool> Delete(Backup backup)
        {
            var bucket = $"backups-service-{backup.ServiceId}";

            if (!await MinioClient.BucketExistsAsync(bucket))
            {
                throw new Exception("No bucket exists.");
            }

            await MinioClient.RemoveObjectAsync(bucket, backup.Guid + ".zip");
            return true;
        }
    }
}