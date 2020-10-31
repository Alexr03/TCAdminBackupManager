using System.Net;
using System.Threading.Tasks;
using FluentFTP;
using TCAdmin.SDK.Objects;

namespace TCAdminBackupManager.BackupSolutions
{
    public class FtpBackup : BackupSolution
    {
        public readonly FtpClient FtpClient;
        public readonly string BucketName;
        
        public FtpBackup(FileServer fileServer, string bucketName)
        {
            var host = fileServer.FtpIpAddress;
            var username = fileServer.FtpUser;
            var password = fileServer.FtpPassword;

            this.BucketName = bucketName;
            FtpClient = new FtpClient(host, fileServer.FtpPort, new NetworkCredential(username, password));
            FtpClient.Connect();
        }

        public override Task<bool> Backup(string fileName, byte[] contents, string contentType)
        {
            var fileLocation = this.BucketName + "/" + fileName;
            var ftpStatus = FtpClient.Upload(contents, fileLocation, createRemoteDir: true);
            return System.Threading.Tasks.Task.FromResult(ftpStatus == FtpStatus.Success);
        }

        public override Task<byte[]> DownloadBytes(string fileName)
        {
            var fileLocation = this.BucketName + "/" + fileName;

            FtpClient.Download(out var content, fileLocation);

            return System.Threading.Tasks.Task.FromResult(content);
        }

        public override Task<string> DirectDownloadLink(string fileName)
        {
            throw new System.NotImplementedException();
        }

        public override Task<bool> Delete(string fileName)
        {
            var fileLocation = this.BucketName + "/" + fileName;
            
            FtpClient.DeleteFile(fileLocation);

            return System.Threading.Tasks.Task.FromResult(true);
        }
    }
}