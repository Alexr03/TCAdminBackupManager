using System;
using System.IO;
using System.Threading.Tasks;
using TCAdmin.SDK.Objects;
using TCAdmin.SDK.Web.FileManager;
using TCAdmin.SDK.Web.References.FileSystem;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Models.Objects;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.BackupSolutions
{
    public class LocalBackup : BackupSolution
    {
        public readonly LocalProviderConfiguration Configuration = new BackupProvider()
            .FindByType(typeof(LocalBackup)).Configuration.Parse<LocalProviderConfiguration>();
        private readonly Service _service;
        private readonly FileSystem _fileSystemService;

        public LocalBackup()
        {
            
        }

        public LocalBackup(Service service)
        {
            this._service = service;
            _fileSystemService = new Server(this._service.ServerId).FileSystemService;
        }
        
        public override Task<bool> Backup(string fileName, byte[] contents, string contentType)
        {
            var saveTo = Path.Combine(Configuration.LocalDirectory.ReplaceVariables(_service), fileName);
            if (!_fileSystemService.DirectoryExists(Path.GetDirectoryName(saveTo)))
            {
                _fileSystemService.CreateDirectory(Path.GetDirectoryName(saveTo));
            }
            
            var memoryStream = new MemoryStream(contents);
            var byteBuffer = new byte[1024 * 1024 * 2];
            memoryStream.Position = 0;
            while (memoryStream.Read(byteBuffer, 0, byteBuffer.Length) > 0)
            {
                _fileSystemService.AppendFile(saveTo, byteBuffer);
            }

            return System.Threading.Tasks.Task.FromResult(true);
        }

        public override Task<byte[]> DownloadBytes(string fileName)
        {
            throw new NotImplementedException();
        }

        public override Task<string> DirectDownloadLink(string fileName)
        {
            var saveTo = Path.Combine(Configuration.LocalDirectory.ReplaceVariables(_service), fileName);

            var remoteDownload = new RemoteDownload(new Server(this._service.ServerId))
            {
                DirectorySecurity = this._service.GetDirectorySecurityForCurrentUser(),
                FileName = saveTo
            };

            return System.Threading.Tasks.Task.FromResult(remoteDownload.GetDownloadUrl());
        }

        public override Task<bool> Delete(string fileName)
        {
            var saveTo = Path.Combine(Configuration.LocalDirectory.ReplaceVariables(_service), fileName);
            _fileSystemService.DeleteFile(saveTo);
            
            return System.Threading.Tasks.Task.FromResult(true);
        }
    }
}