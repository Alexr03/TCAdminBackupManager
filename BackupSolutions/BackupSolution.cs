using System.IO;
using System.Threading.Tasks;
using TCAdmin.GameHosting.SDK.Objects;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.VirtualFileSystem;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.GameHosting.SDK.Objects.Server;

namespace TCAdminBackupManager.BackupSolutions
{
    public abstract class BackupSolution
    {
        public bool AllowsDirectDownload = false;

        /// <summary>
        /// Backup a file with specific file name, contents and content type
        /// </summary>
        /// <param name="backup"></param>
        /// <param name="targetPath">Name of the file</param>
        /// <returns></returns>
        public abstract Task<bool> BackupFile(Backup backup, string targetPath);

        public abstract Task<bool> BackupDirectory(Backup backup, string targetPath);

        /// <summary>
        /// Download the bytes of the file.
        /// </summary>
        /// <param name="backup">The backup</param>
        /// <returns>Byte array of the file</returns>
        public abstract Task<byte[]> DownloadBytes(Backup backup);

        /// <summary>
        /// If the backup type supports a direct download link to the file. Return it here.
        /// Set "AllowsDirectDownload" to true.
        /// </summary>
        /// <param name="backup">The backup</param>
        /// <returns>URL for direct download of the file</returns>
        public abstract Task<string> DirectDownloadLink(Backup backup);

        /// <summary>
        /// Delete the file from the backup server.
        /// </summary>
        /// <param name="backup">The backup</param>
        /// <returns>True if deleted successfully.</returns>
        public abstract Task<bool> Delete(Backup backup);

        public virtual string CompressFile(Backup backup, string targetPath)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var backupLocation = FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, "BackupManager");
            var baseDir = FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory,
                Path.GetDirectoryName(targetPath));
            return fileSystemService.CompressFiles(baseDir, new[] {Path.GetFileName(targetPath)},
                ObjectXml.ObjectToXml(GenerateVirtualDirectorySecurity(service)), 5000000000);
        }

        protected static VirtualDirectorySecurity GenerateVirtualDirectorySecurity(Service service)
        {
            return new TCAdmin.SDK.VirtualFileSystem.VirtualDirectorySecurity
            {
                PermissionMode = PermissionMode.Basic,
                Permissions = Permission.Read | Permission.Write | Permission.Delete,
                PermissionType = PermissionType.Root,
                RootPhysicalPath = service.RootDirectory,
                DisableOwnerCheck = true,
                DisableSymlinkCheck = true
            };
        }
    }
}