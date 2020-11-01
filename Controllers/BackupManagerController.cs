using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using Alexr03.Common.TCAdmin.Objects;
using Alexr03.Common.Web.Extensions;
using Alexr03.Common.Web.HttpResponses;
using TCAdmin.SDK.Database;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdminBackupManager.BackupSolutions;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.Controllers
{
    public class BackupManagerController : BaseServiceController
    {
        public ActionResult Index(int id)
        {
            var service = Service.GetSelectedService();
            // return new JsonNetResult(service);
            // return Json(service, JsonRequestBehavior.AllowGet);
            var model = new BackupManagerIndexModel
            {
                ServiceId = service.ServiceId,
                UsedQuota = GetBackupsQuotaUsed(service),
                MaxQuota = GetBackupsQuota(service)
            };
            return View(model);
        }

        [ParentAction("Service", "Home")]
        [HttpPost]
        public async Task<ActionResult> BackupFile(int id, string name, string file)
        {
            this.EnforceFeaturePermission("FileManager");
            var backupProvider = DynamicTypeBase.GetCurrent<BackupProvider>("backupProvider");
            var service = Service.GetSelectedService();
            var server = TCAdmin.SDK.Objects.Server.GetSelectedServer();
            var directorySecurity = service.GetDirectorySecurityForCurrentUser();
            var virtualDirectory =
                new TCAdmin.SDK.VirtualFileSystem.VirtualDirectory(server.OperatingSystem, directorySecurity);

            if (string.IsNullOrEmpty(file))
            {
                return new JsonHttpStatusResult(new
                {
                    Message = "Please choose a file to backup."
                }, HttpStatusCode.BadRequest);
            }

            var realFileName = Path.GetFileName(file);

            if (realFileName.Any(Path.GetInvalidFileNameChars().Contains) ||
                !Regex.IsMatch(realFileName, @"^[\w\-.@ ]+$"))
            {
                return new JsonHttpStatusResult(new
                {
                    Message = "File contains invalid characters."
                }, HttpStatusCode.BadRequest);
            }

            var fileSystem = server.FileSystemService;
            var backupSolution = backupProvider.Create<BackupSolution>();
            var filePath = virtualDirectory.CombineWithPhysicalPath(file);
            var fileSize = fileSystem.GetFileSize(filePath);
            if (GetBackupsQuotaUsed(service, backupProvider) + fileSize > GetBackupsQuota(service, backupProvider))
            {
                return new JsonHttpStatusResult(new
                {
                    Message = "Backing up this file will exceed your assigned capacity."
                }, HttpStatusCode.BadRequest);
            }

            try
            {
                var backup = new Backup
                {
                    ServiceId = service.ServiceId,
                    OwnerId = service.UserId,
                    Name = name,
                    Provider = backupProvider,
                    Guid = Guid.NewGuid().ToString("D"),
                    FileSize = fileSize
                };
                backup.GenerateKey();
                backup.Save();
                if (!await backupSolution.BackupFile(backup, realFileName))
                {
                    backup.Delete();
                    throw new Exception("Backup File Failed!");
                }
            }
            catch (Exception e)
            {
                return this.SendException(e, "Failed to backup: " + e.Message);
            }

            return this.SendSuccess($"Backed up <strong>{realFileName}</strong>");
        }

        [ParentAction("Service", "Home")]
        public async Task<ActionResult> Restore(int id, string target, int backupId = 0)
        {
            this.EnforceFeaturePermission("FileManager");
            if (backupId == 0)
            {
                return this.SendError("No backup selected to restore.");
            }

            var service = Service.GetSelectedService();
            var server = TCAdmin.GameHosting.SDK.Objects.Server.GetSelectedServer();
            var directorySecurity = service.GetDirectorySecurityForCurrentUser();
            var virtualDirectory =
                new TCAdmin.SDK.VirtualFileSystem.VirtualDirectory(server.OperatingSystem, directorySecurity);
            var fileSystem = TCAdmin.SDK.Objects.Server.GetSelectedServer().FileSystemService;
            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            try
            {
                var targetPath = virtualDirectory.CombineWithPhysicalPath(target);

                var randomFileName = TCAdmin.SDK.Misc.Random.RandomString(8, true, true);
                var saveTo =
                    TCAdmin.SDK.Misc.FileSystem.CombinePath(server.OperatingSystem, targetPath, randomFileName);

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

                return this.SendSuccess($"Restored <strong>{backup.Name}</strong>");
            }
            catch (Exception e)
            {
                return this.SendException(e, "Unable to restore backup - " + e.Message);
            }
        }

        [ParentAction("Service", "Home")]
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
            var backups = Backup.GetBackupsForService(service);
            var value = backups.Sum(backup => backup.FileSize);
            return value;
        }

        public static long GetBackupsQuotaUsed(Service service, BackupProvider provider)
        {
            var backups = Backup.GetBackupsForService(service);
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

        public static byte[] GetFileContents(string downloadUrl)
        {
            using (var wc = new WebClient())
            {
                return wc.DownloadData(downloadUrl);
            }
        }
    }
}