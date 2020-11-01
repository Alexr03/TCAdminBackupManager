using System;
using System.IO;
using System.Threading.Tasks;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.VirtualFileSystem;
using TCAdmin.SDK.Web.FileManager;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.SDK.Objects.Server;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupSolutions
{
    public class LocalSolution : BackupSolution
    {
        public readonly LocalProviderConfiguration Configuration = new BackupProvider()
            .FindByType(typeof(LocalSolution)).Configuration.Parse<LocalProviderConfiguration>();

        public LocalSolution()
        {
            this.AllowsDirectDownload = true;
        }

        public override Task<bool> BackupFile(Backup backup, string targetPath)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var backupLocation = FileSystem.CombinePath(server.OperatingSystem, Configuration.LocalDirectory.ReplaceVariables(service));
            var baseDir = FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory,
                Path.GetDirectoryName(targetPath));
            Console.WriteLine("Basedir: " + baseDir);
            Console.WriteLine("file: " + Path.GetFileName(targetPath));
            var compressedFileName = fileSystemService.CompressFiles(baseDir, new[] {Path.GetFileName(targetPath)},
                ObjectXml.ObjectToXml(GenerateVirtualDirectorySecurity(service)), 5000000000);
            Console.WriteLine("compress name: " + compressedFileName);
            var originalPath = FileSystem.CombinePath(server.OperatingSystem, baseDir, compressedFileName);
            var newPath = FileSystem.CombinePath(server.OperatingSystem, backupLocation, backup.Guid + ".zip");
            
            if (!fileSystemService.DirectoryExists(Path.GetDirectoryName(backupLocation)))
            {
                fileSystemService.CreateDirectory(Path.GetDirectoryName(backupLocation));
            }
            fileSystemService.CopyFile(originalPath, newPath);
            fileSystemService.DeleteFile(originalPath);

            return System.Threading.Tasks.Task.FromResult(true);
        }

        public override Task<bool> BackupDirectory(Backup backup, string targetPath)
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> DownloadBytes(Backup backup)
        {
            throw new NotImplementedException();
        }

        public override Task<string> DirectDownloadLink(Backup backup)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var saveTo = TCAdmin.SDK.Misc.FileSystem.CombinePath(server.OperatingSystem, Configuration.LocalDirectory.ReplaceVariables(service), backup.Guid + ".zip");
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
            var saveTo = Path.Combine(Configuration.LocalDirectory.ReplaceVariables(service), backup.Guid + ".zip");
            fileSystemService.DeleteFile(saveTo);
            
            return System.Threading.Tasks.Task.FromResult(true);
        }
    }
}