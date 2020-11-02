using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.Objects;
using TCAdmin.SDK.Web.FileManager;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Controllers;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupProviders
{
    public class LocalProvider : BackupSolution
    {
        public readonly LocalProviderConfiguration Configuration = new BackupProvider()
            .FindByType(typeof(LocalProvider)).Configuration.Parse<LocalProviderConfiguration>();

        public LocalProvider()
        {
            this.AllowsDirectDownload = true;
        }

        public override async Task<BackupResponse> Backup(Backup backup, BackupRequest request)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var backupLocation = FileSystem.CombinePath(server.OperatingSystem,
                Configuration.LocalDirectory.ReplaceVariables(service));
            var originalPath = FileSystem.FixAbsoluteFilePath(
                FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, request.Path,
                    Compress(backup, request)), server.OperatingSystem);
            var newPath = FileSystem.CombinePath(server.OperatingSystem, backupLocation, backup.ZipFullName);
            var fileSize = fileSystemService.GetFileSize(originalPath);
            BackupManagerController.ThrowExceedQuota(backup, request, fileSize);
            backup.FileSize = fileSize;

            if (!fileSystemService.DirectoryExists(Path.GetDirectoryName(backupLocation)))
            {
                fileSystemService.CreateDirectory(Path.GetDirectoryName(backupLocation));
            }

            fileSystemService.CopyFile(originalPath, newPath);
            fileSystemService.DeleteFile(originalPath);

            return new BackupResponse(true);
        }

        public override Task<byte[]> DownloadBytes(Backup backup)
        {
            throw new NotImplementedException();
        }

        public override Task<string> DirectDownloadLink(Backup backup)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var saveTo = FileSystem.CombinePath(server.OperatingSystem,
                Configuration.LocalDirectory.ReplaceVariables(service), backup.ZipFullName);
            var remoteDownload = new RemoteDownload(server)
            {
                DirectorySecurity = service.GetDirectorySecurityForCurrentUser(),
                FileName = saveTo
            };

            return System.Threading.Tasks.Task.FromResult(remoteDownload.GetDownloadUrl());
        }

        public override Task<bool> Delete(Backup backup)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var saveTo = Path.Combine(Configuration.LocalDirectory.ReplaceVariables(service), backup.ZipFullName);
            fileSystemService.DeleteFile(saveTo);
            return System.Threading.Tasks.Task.FromResult(true);
        }

        public override string Compress(Backup backup, BackupRequest request)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var directorySecurity = GenerateVirtualDirectorySecurity(service);
            var baseDirectory =
                FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, request.Path);
            var fileSystem = server.FileSystemService;
            var toCompress = request.Directories.Select(x => x.Name).ToList();
            toCompress.AddRange(request.Files.Select(x => x.Name + x.Extension));
            return fileSystem.CompressFiles(baseDirectory, toCompress.ToArray(),
                ObjectXml.ObjectToXml(directorySecurity), 500000000);
        }
    }
}