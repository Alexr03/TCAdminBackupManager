using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Alexr03.Common.TCAdmin.Objects;
using Alexr03.Common.Web.Extensions;
using Alexr03.Common.Web.HttpResponses;
using TCAdmin.SDK.Web.FileManager;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdmin.Web.MVC;
using TCAdminBackupManager.BackupSolutions;
using TCAdminBackupManager.Configuration;
using TCAdminBackupManager.Models.Objects;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.Controllers
{
    [ExceptionHandler]
    [Authorize]
    public class BackupManagerController : BaseServiceController
    {
        [ParentAction("Service", "Home")]
        [HttpPost]
        public async Task<ActionResult> BackupFile(int id, string file)
        {
            var backupProvider = DynamicTypeBase.GetCurrent<BackupProvider>("backupProvider");
            var service = Service.GetSelectedService();
            var server = TCAdmin.SDK.Objects.Server.GetSelectedServer();
            var directorySecurity = service.GetDirectorySecurityForCurrentUser();
            var virtualDirectory =
                new TCAdmin.SDK.VirtualFileSystem.VirtualDirectory(server.OperatingSystem, directorySecurity);

            this.EnforceFeaturePermission("FileManager");
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
            if (GetBackupsSize(service, backupProvider) + fileSize > GetBackupsLimit(service, backupProvider))
            {
                return new JsonHttpStatusResult(new
                {
                    Message = "Backing up this file will exceed your assigned capacity."
                }, HttpStatusCode.BadRequest);
            }

            var remoteDownload = new RemoteDownload(server)
            {
                DirectorySecurity = service.GetDirectorySecurityForCurrentUser(),
                FileName = filePath
            };

            var backupName = $"{realFileName}";
            var contents = GetFileContents(remoteDownload.GetDownloadUrl());

            try
            {
                await backupSolution.Backup(backupName, contents, MimeMapping.GetMimeMapping(realFileName));
                var backup = new Backup
                {
                    ServiceId = service.ServiceId,
                    FileName = backupName,
                    Provider = backupProvider
                };
                backup.CustomFields["SIZE"] = fileSize;
                backup.GenerateKey();
                backup.Save();
            }
            catch (Exception e)
            {
                return this.SendException(e, "Failed to backup: " + e.Message);
            }

            return this.SendSuccess($"Backed up <strong>{backupName}</strong>");
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
            var dirsec = service.GetDirectorySecurityForCurrentUser();
            var vdir = new TCAdmin.SDK.VirtualFileSystem.VirtualDirectory(server.OperatingSystem, dirsec);
            var fileSystem = TCAdmin.SDK.Objects.Server.GetSelectedServer().FileSystemService;
            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            try
            {
                var targetpath = vdir.CombineWithPhysicalPath(target);
                var saveTo =
                    TCAdmin.SDK.Misc.FileSystem.CombinePath(targetpath, backup.FileName, server.OperatingSystem);

                if (backupSolution.AllowsDirectDownload)
                {
                    var downloadUrl = await backupSolution.DirectDownloadLink(backup.FileName);
                    fileSystem.DownloadFile(saveTo, downloadUrl);
                }
                else
                {
                    var bytes = await backupSolution.DownloadBytes(backup.FileName);
                    var memoryStream = new MemoryStream(bytes);
                    var byteBuffer = new byte[1024 * 1024 * 2];
                    int bytesread;
                    memoryStream.Position = 0;

                    if (fileSystem.FileExists(saveTo))
                    {
                        fileSystem.DeleteFile(saveTo);
                    }

                    while ((bytesread = memoryStream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                    {
                        fileSystem.AppendFile(saveTo, byteBuffer.Take(bytesread).ToArray());
                    }

                    fileSystem.SetOwnerAutomatically(saveTo);
                }

                return this.SendSuccess($"Restored <strong>{backup.FileName}</strong>");
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
                return new JsonHttpStatusResult(new
                {
                    Message = "No backup selected to delete."
                }, HttpStatusCode.InternalServerError);
            }

            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            try
            {
                await backupSolution.Delete(backup.FileName);
                backup.Delete();
                return new JsonHttpStatusResult(new
                {
                    Message = $"Deleted <strong>{backup.FileName}</strong>"
                }, HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return new JsonHttpStatusResult(new
                {
                    Message = "An error occurred: " + e.Message
                }, HttpStatusCode.InternalServerError);
            }
        }

        [ParentAction("Service", "Home")]
        public ActionResult List(int id)
        {
            this.EnforceFeaturePermission("FileManager");
            var service = Service.GetSelectedService();
            var backups = Backup.GetBackupsForService(service).ToList();
            return new JsonNetResult(backups);
        }

        [ParentAction("Service", "Home")]
        public async Task<ActionResult> Download(int id, int backupId)
        {
            this.EnforceFeaturePermission("FileManager");
            var backup = new Backup(backupId);
            var backupSolution = backup.Provider.Create<BackupSolution>();

            if (backupSolution.AllowsDirectDownload)
            {
                var downloadUrl = await backupSolution.DirectDownloadLink(backup.FileName);
                return Redirect(downloadUrl);
            }

            var bytes = await backupSolution.DownloadBytes(backup.FileName);
            return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, backup.FileName);
        }

        [ParentAction("Service", "Home")]
        public async Task<ActionResult> Capacity(int id, BackupProvider provider)
        {
            this.EnforceFeaturePermission("FileManager");
            var user = TCAdmin.SDK.Session.GetCurrentUser();
            var service = Service.GetSelectedService();
            var value = GetBackupsSize(service, provider);
            var limit = GetBackupsLimit(service, provider);

            if (limit == -1)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            return Json(new
            {
                limit,
                value
            }, JsonRequestBehavior.AllowGet);
        }

        [ParentAction("Service", "Home")]
        public static IEnumerable<BackupProvider> AccessibleSolutions(int id)
        {
            var service = Service.GetSelectedService();
            var backupProviders = new BackupProvider().GetAll<BackupProvider>();
            foreach (var backupProvider in from backupProvider in backupProviders
                let config = backupProvider.Configuration.Parse<BackupProviderConfiguration>()
                where config.Enabled && backupProvider.GetQuota(service) > 0
                select backupProvider)
            {
                yield return backupProvider;
            }
        }

        private static long GetBackupsSize(Service service, BackupProvider provider)
        {
            var backups = Backup.GetBackupsForService(service);
            backups.RemoveAll(x => x.BackupId != provider.Id);
            var value = backups.Sum(backup => backup.FileSize);
            return value;
        }

        private static long GetBackupsLimit(Service service, BackupProvider provider)
        {
            return provider.GetQuota(service);
        }

        private static byte[] GetFileContents(string downloadUrl)
        {
            using (var wc = new WebClient())
            {
                return wc.DownloadData(downloadUrl);
            }
        }
    }
}