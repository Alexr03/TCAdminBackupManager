using System.Linq;
using System.Threading.Tasks;
using TCAdmin.GameHosting.SDK.Objects;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.VirtualFileSystem;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;
using Server = TCAdmin.GameHosting.SDK.Objects.Server;

namespace TCAdminBackupManager.BackupProviders
{
    public abstract class BackupSolution
    {
        public bool AllowsDirectDownload = false;

        /// <summary>
        /// Backup a file with specific file name, contents and content type
        /// </summary>
        /// <param name="backup"></param>
        /// <param name="request">The backup request, contains path, directories and files to be backed up.</param>
        /// <returns></returns>
        public abstract Task<BackupResponse> Backup(Backup backup, BackupRequest request);

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

        public virtual string Compress(Backup backup, BackupRequest request)
        {
            var service = new Service(backup.ServiceId);
            var server = new Server(service.ServerId);
            var fileSystemService = server.FileSystemService;
            var baseDir = FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, request.Path);
            var toCompress = request.Directories.Select(x => x.Name).ToList();
            toCompress.AddRange(request.Files.Select(x => x.Name + x.Extension));

            return fileSystemService.CompressFiles(baseDir, toCompress.ToArray(),
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