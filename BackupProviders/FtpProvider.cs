using System.Net;
using System.Threading.Tasks;
using FluentFTP;
using TCAdmin.SDK.Misc;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Controllers;
using TCAdminBackupManager.Exceptions;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupProviders
{
    public class FtpProvider : BackupSolution
    {
        public readonly FtpClient FtpClient;

        public FtpProvider()
        {
            var settings = new BackupProvider().FindByType(typeof(FtpProvider)).Configuration
                .Parse<FtpProviderConfiguration>();
            var host = settings.Host;
            var username = settings.Username;
            var password = settings.Password;

            FtpClient = new FtpClient(host, settings.Port, new NetworkCredential(username, password));
            FtpClient.Connect();
        }

        public override async Task<BackupResponse> Backup(Backup backup, BackupRequest request)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var combinePath = FileSystem.FixAbsoluteFilePath(
                FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, request.Path,
                    Compress(backup, request)), server.OperatingSystem);
            var fileSize = fileSystemService.GetFileSize(combinePath);
            BackupManagerController.ThrowExceedQuota(backup, request, fileSize);
            backup.FileSize = fileSize;

            var contents = fileSystemService.ReadBinary(combinePath);
            fileSystemService.DeleteFile(combinePath);
            var fileLocation = $"/TCAdminBackupManager/{service.UserId}/{backup.ZipFullName}";
            var ftpStatus = FtpClient.Upload(contents, fileLocation, createRemoteDir: true,
                existsMode: FtpRemoteExists.Overwrite);
            return new BackupResponse(ftpStatus == FtpStatus.Success);
        }

        public override Task<byte[]> DownloadBytes(Backup backup)
        {
            var service = new Service(backup.ServiceId);
            var fileLocation = $"/TCAdminBackupManager/{service.UserId}/{backup.ZipFullName}";
            FtpClient.Download(out var content, fileLocation);
            return System.Threading.Tasks.Task.FromResult(content);
        }

        public override Task<string> DirectDownloadLink(Backup backup)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> Delete(Backup backup)
        {
            var service = new Service(backup.ServiceId);
            var fileLocation = $"/TCAdminBackupManager/{service.UserId}/{backup.ZipFullName}";
            FtpClient.DeleteFile(fileLocation);
            return System.Threading.Tasks.Task.FromResult(true);
        }
    }
}