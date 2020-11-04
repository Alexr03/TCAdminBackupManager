using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Alexr03.Common.Web.Extensions;
using TCAdmin.SDK.Misc;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdminBackupManager.BackupProviders;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Exceptions;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;
using WebClient = System.Net.WebClient;

namespace TCAdminBackupManager.Controllers
{
    public class BackupManagerController : BaseServiceController
    {
        public ActionResult Index(int id)
        {
            var service = Service.GetSelectedService();
            var model = new BackupManagerIndexModel
            {
                ServiceId = service.ServiceId,
                UsedQuota = GetBackupsQuotaUsed(service),
                MaxQuota = GetBackupsQuota(service)
            };
            return View(model);
        }

        [ParentAction("Service", "FileManager")]
        [HttpPost]
        public async Task<ActionResult> Backup(int id, BackupRequest backupRequest)
        {
            this.EnforceFeaturePermission("FileManager");
            var service = Service.GetSelectedService();

            if (!backupRequest.Directories.Any() && !backupRequest.Files.Any())
            {
                return this.SendError("Please choose at least <strong>1</strong> file or directory to backup.");
            }

            try
            {
                var backup = await Models.Objects.Backup.CreateAsync(service, backupRequest);
                backup.Save();
                return this.SendSuccess($"Backed up <strong>{backupRequest.Name}</strong>");
            }
            catch (Exception e)
            {
                return this.SendException(e, "Failed to backup: " + e.Message);
            }
        }

        [ParentAction("Service", "Home")]
        [HttpPost]
        public async Task<ActionResult> Restore(int id, int backupId = 0)
        {
            this.EnforceFeaturePermission("FileManager");
            if (backupId == 0)
            {
                return this.SendError("No backup selected to restore.");
            }

            var service = Service.GetSelectedService();
            var server = TCAdmin.SDK.Objects.Server.GetSelectedServer();
            var directorySecurity = service.GetDirectorySecurityForCurrentUser();
            var fileSystem = TCAdmin.SDK.Objects.Server.GetSelectedServer().FileSystemService;
            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            try
            {
                var randomFileName = TCAdmin.SDK.Misc.Random.RandomString(8, true, true) + ".zip";
                var saveTo = FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, backup.Path, randomFileName);

                if (backupSolution.AllowsDirectDownload)
                {
                    var downloadUrl = await backupSolution.DirectDownloadLink(backup);
                    fileSystem.DownloadFile(saveTo, downloadUrl);
                }
                else
                {
                    var bytes = await backupSolution.DownloadBytes(backup);
                    var memoryStream = new MemoryStream(bytes);
                    var byteBuffer = new byte[1024 * 1024 * 2];
                    int bytesRead;
                    memoryStream.Position = 0;

                    if (fileSystem.FileExists(saveTo))
                    {
                        fileSystem.DeleteFile(saveTo);
                    }

                    while ((bytesRead = memoryStream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                    {
                        fileSystem.AppendFile(saveTo, byteBuffer.Take(bytesRead).ToArray());
                    }

                    fileSystem.SetOwnerAutomatically(saveTo);
                }

                fileSystem.Extract(saveTo,
                    FileSystem.CombinePath(server.OperatingSystem, service.RootDirectory, backup.Path),
                    ObjectXml.ObjectToXml(directorySecurity));
                fileSystem.DeleteFile(saveTo);

                return this.SendSuccess($"Restored <strong>{backup.Name}</strong>");
            }
            catch (Exception e)
            {
                return this.SendException(e, "Unable to restore backup - " + e.Message);
            }
        }

        [ParentAction("Service", "Home")]
        [HttpPost]
        public async Task<ActionResult> Delete(int id, int backupId = 0)
        {
            this.EnforceFeaturePermission("FileManager");
            if (backupId == 0)
            {
                return this.SendError("No backup selected.");
            }

            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            try
            {
                await backupSolution.Delete(backup);
                backup.Delete();
                return this.SendSuccess($"Deleted <strong>{backup.Name}</strong> backup.");
            }
            catch (Exception e)
            {
                return this.SendException(e, $"Failed to delete backup <strong>{backup.Name}</strong>.");
            }
        }

        [ParentAction("Service", "Home")]
        public async Task<ActionResult> Download(int id, int backupId)
        {
            this.EnforceFeaturePermission("FileManager");
            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            if (backupSolution.AllowsDirectDownload)
            {
                var downloadUrl = await backupSolution.DirectDownloadLink(backup);
                return Redirect(downloadUrl);
            }

            var bytes = await backupSolution.DownloadBytes(backup);
            return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, backup.Guid + ".zip");
        }

        [ParentAction("Service", "Home")]
        public static List<BackupProvider> AccessibleProviders(Service service)
        {
            var backupProviders = new BackupProvider().GetAll<BackupProvider>();
            return (from backupProvider in backupProviders
                let config = backupProvider.Configuration.Parse<BackupProviderConfiguration>()
                where config.Enabled && backupProvider.GetQuota(service) > 0
                select backupProvider).ToList();
        }

        public static long GetBackupsQuotaUsed(Service service)
        {
            var backups = Models.Objects.Backup.GetBackupsForService(service);
            var value = backups.Sum(backup => backup.FileSize);
            return value;
        }

        public static long GetBackupsQuotaUsed(Service service, BackupProvider provider)
        {
            var backups = Models.Objects.Backup.GetBackupsForService(service);
            backups.RemoveAll(x => x.Provider.Id != provider.Id);
            var value = backups.Sum(backup => backup.FileSize);
            return value;
        }

        public static long GetBackupsQuota(Service service)
        {
            var providers = AccessibleProviders(service);
            return providers.Sum(provider => GetBackupsQuota(service, provider));
        }

        public static long GetBackupsQuota(Service service, BackupProvider provider)
        {
            return provider.GetQuota(service);
        }

        public static void ThrowExceedQuota(Backup backup, BackupRequest request, long fileSize)
        {
            var service = new Service(backup.ServiceId);
            var t = GetBackupsQuotaUsed(service, backup.Provider);
            var t2 = GetBackupsQuota(service, backup.Provider);
            var newSize = t + fileSize;
            Console.WriteLine(t);
            Console.WriteLine(t2);
            Console.WriteLine(newSize);
            Console.WriteLine(newSize > t2);
            if (newSize > t2)
            {
                throw new QuotaException(backup, request);
            }
        }

        public static byte[] GetFileContents(string downloadUrl)
        {
            using (var wc = new WebClient())
            {
                return wc.DownloadData(downloadUrl);
            }
        }
    }
}