using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentFTP;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.VirtualFileSystem;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupSolutions
{
    public class FtpSolution : BackupSolution
    {
        public readonly FtpClient FtpClient;

        public FtpSolution()
        {
            var settings = new BackupProvider().FindByType(typeof(FtpSolution)).Configuration
                .Parse<FtpProviderConfiguration>();
            var host = settings.Host;
            var username = settings.Username;
            var password = settings.Password;

            FtpClient = new FtpClient(host, settings.Port, new NetworkCredential(username, password));
            FtpClient.Connect();
        }

        public override Task<bool> BackupFile(Backup backup, string targetPath)
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
            fileSystemService.DeleteFile(combinePath);
            var fileLocation = $"/TCAdminBackupManager/{service.UserId}/{backup.Guid}.zip";
            var ftpStatus = FtpClient.Upload(contents, fileLocation, createRemoteDir: true, existsMode: FtpRemoteExists.Overwrite);
            return System.Threading.Tasks.Task.FromResult(ftpStatus == FtpStatus.Success);
        }

        public override Task<bool> BackupDirectory(Backup backup, string targetPath)
        {
            throw new System.NotImplementedException();
        }

        public override Task<byte[]> DownloadBytes(Backup backup)
        {
            var service = new Service(backup.ServiceId);
            var fileLocation = $"/TCAdminBackupManager/{service.UserId}/{backup.Guid}.zip";
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
            var fileLocation = $"/TCAdminBackupManager/{service.UserId}/{backup.Guid}.zip";
            FtpClient.DeleteFile(fileLocation);
            return System.Threading.Tasks.Task.FromResult(true);
        }
    }
}