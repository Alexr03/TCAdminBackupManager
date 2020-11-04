using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Alexr03.Common.TCAdmin.Extensions;
using Newtonsoft.Json;
using TCAdmin.Interfaces.Database;
using TCAdmin.SDK.Objects;
using TCAdminBackupManager.BackupProviders;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.Models.Objects
{
    public class Backup : ObjectBase
    {
        public Backup()
        {
            this.TableName = "tcmodule_backupmanager_service_backups";
            this.KeyColumns = new[] {"id"};
            this.SetValue("id", -1);
            this.UseApplicationDataField = true;
        }

        public Backup(int id) : this()
        {
            this.SetValue("id", id);
            this.ValidateKeys();
            if (!this.Find())
            {
                throw new KeyNotFoundException("Cannot find backup with ID: " + id);
            }
        }

        public static Backup Create(Service service, BackupRequest request)
        {
            return CreateAsync(service, request).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async System.Threading.Tasks.Task<Backup> CreateAsync(Service service, BackupRequest request)
        {
            var backup = new Backup
            {
                ServiceId = service.ServiceId,
                OwnerId = service.UserId,
                Name = request.Name,
                ProviderId = request.ProviderId,
                Guid = System.Guid.NewGuid().ToString("D"),
                Request = request
            };
            backup.GenerateKey();
            var provider = backup.Provider.Create<BackupSolution>();
            await provider.Backup(backup, request);
            return backup;
        }

        public int BackupId
        {
            get => this.GetIntegerValue("id");
            set => this.SetValue("id", value);
        }

        public string Name
        {
            get => this.GetStringValue("name");
            set => this.SetValue("name", value);
        }

        public string Guid
        {
            get => this.GetStringValue("guid");
            set => this.SetValue("guid", value);
        }

        public string ZipFullName => Guid + ".zip";

        public int ServiceId
        {
            get => this.GetIntegerValue("serviceId");
            set => this.SetValue("serviceId", value);
        }

        public int OwnerId
        {
            get => this.GetIntegerValue("ownerId");
            set => this.SetValue("ownerId", value);
        }

        private int ProviderId
        {
            get => this.GetIntegerValue("providerId");
            set => this.SetValue("providerId", value);
        }

        public BackupProvider Provider
        {
            get => new BackupProvider(ProviderId);
            set => ProviderId = value.Id;
        }

        public long FileSize
        {
            get => this.AppData.HasValueAndSet("SIZE") ? long.Parse(this.CustomFields["SIZE"].ToString()) : 0L;
            set => this.CustomFields["SIZE"] = value;
        }

        public BackupRequest Request
        {
            get => this.AppData.HasValue("REQUEST")
                ? JsonConvert.DeserializeObject<BackupRequest>(this.AppData["REQUEST"].ToString())
                : null;
            set => this.AppData["REQUEST"] = JsonConvert.SerializeObject(value);
        }

        public string Path => Request.Path;

        public string FriendlyFileSize => GetFileSize(FileSize);

        public static List<Backup> GetBackupsForService(Service service)
        {
            var whereList = new WhereList
            {
                {"serviceId", service.ServiceId}
            };
            var objectList = new Backup().GetObjectList(whereList);
            return objectList.Count == 0 ? new List<Backup>() : objectList.Cast<Backup>().ToList();
        }

        public static List<Backup> GetBackupsForService(Service service, BackupProvider provider)
        {
            return GetBackupsForService(service, provider.Id);
        }

        public static List<Backup> GetBackupsForService(Service service, int providerId)
        {
            var whereList = new WhereList
            {
                {"serviceId", service.ServiceId},
                {"providerId", providerId}
            };
            var objectList = new Backup().GetObjectList(whereList);
            return objectList.Count == 0 ? new List<Backup>() : objectList.Cast<Backup>().ToList();
        }

        public static string GetFileSize(long byteCount)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1000)));
            var num = Math.Round(bytes / Math.Pow(1000, place), 1);
            return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + suf[place];
        }
    }
}