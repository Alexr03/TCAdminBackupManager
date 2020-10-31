using System;
using System.Collections.Generic;
using Alexr03.Common.TCAdmin.Extensions;
using Alexr03.Common.TCAdmin.Objects;
using TCAdminBackupManager.Configuration;
using Service = TCAdmin.GameHosting.SDK.Objects.Service;

namespace TCAdminBackupManager.Models.Objects
{
    public class BackupProvider : DynamicTypeBase
    {
        public BackupProvider() : base("tcmodule_backup_providers")
        {
        }

        public BackupProvider(int id)
        {
            this.SetValue("id", id);
            this.ValidateKeys();
            if (!this.Find())
            {
                throw new KeyNotFoundException("Cannot find Backup Provider with ID: " + id);
            }
        }

        public string Name
        {
            get => this.GetStringValue("name");
            set => this.SetValue("name", value);
        }

        public long GetQuota(Service service = null)
        {
            var defaultQuota = this.Configuration.Parse<BackupProviderConfiguration>().QuotaBytes;
            if (service == null)
            {
                return defaultQuota;
            }

            return service.Variables.HasValueAndSet($"{this.Name}:LIMIT")
                ? Convert.ToInt64(service.Variables[$"{this.Name}:LIMIT"])
                : defaultQuota;
        }
    }
}