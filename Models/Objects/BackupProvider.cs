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
        public BackupProvider() : base("tcmodule_backupmanager_providers")
        {
        }

        public BackupProvider(int id) : this()
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
            var config = this.Configuration.Parse<BackupProviderConfiguration>();
            long quota;
            if (service != null)
            {
                var key = $"{this.Name}:LIMIT";
                quota = service.AppData.HasValueAndSet(key)
                    ? Convert.ToInt64(service.AppData[key])
                    : config.Quota;
            }
            else
            {
                quota = config.Quota;
            }

            switch (config.QuotaType)
            {
                case QuotaType.Kb:
                    quota *= 1_000;
                    break;
                case QuotaType.Mb:
                    quota *= 1_000_000;
                    break;
                case QuotaType.Gb:
                    quota *= 1_000_000_000;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return quota;
        }
    }
}