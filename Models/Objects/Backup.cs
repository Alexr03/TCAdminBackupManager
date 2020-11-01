using System;
using System.Collections.Generic;
using System.Linq;
using Alexr03.Common.TCAdmin.Extensions;
using TCAdmin.Interfaces.Database;
using TCAdmin.SDK.Objects;
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
            this.SetValue("backupId", id);
            this.ValidateKeys();
            if (!this.Find())
            {
                throw new KeyNotFoundException("Cannot find backup with ID: " + id);
            }
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
    }
}