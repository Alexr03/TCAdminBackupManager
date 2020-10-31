using System.Collections.Generic;
using System.Linq;
using TCAdmin.Interfaces.Database;
using TCAdmin.SDK.Objects;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.Models.Objects
{
    public class Backup : ObjectBase
    {
        public Backup()
        {
            this.TableName = "tcmodule_backups";
            this.KeyColumns = new[] {"backupId"};
            this.SetValue("backupId", -1);
            this.UseApplicationDataField = true;
        }

        public Backup(int id) : this()
        {
            this.SetValue("backupId", id);
            this.ValidateKeys();
            if (!this.Find())
            {
                throw new KeyNotFoundException("Cannot find backup with ID: " + id);
            }
        }

        public int BackupId
        {
            get => this.GetIntegerValue("backupId");
            set => this.SetValue("backupId", value);
        }
        
        public int ServiceId
        {
            get => this.GetIntegerValue("serviceId");
            set => this.SetValue("serviceId", value);
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

        public string FileName
        {
            get => this.GetStringValue("fileName");
            set => this.SetValue("fileName", value);
        }
        
        public long FileSize
        {
            get => long.Parse(this.CustomFields["SIZE"].ToString());
            set => this.CustomFields["SIZE"] = value;
        }
        
        public static List<Backup> GetBackupsForService(Service service)
        {
            var whereList = new WhereList
            {
                {"serviceId", service.ServiceId}
            };
            return new Backup().GetObjectList(whereList).Cast<Backup>().ToList();
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
            return new Backup().GetObjectList(whereList).Cast<Backup>().ToList();
        }
    }
}