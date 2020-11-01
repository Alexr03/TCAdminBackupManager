namespace TCAdminBackupManager.Configuration
{
    public class BackupProviderConfiguration
    {
        public bool Enabled { get; set; }

        public long Quota { get; set; } = 5;

        public QuotaType QuotaType { get; set; } = QuotaType.Gb;
    }

    public enum QuotaType
    {
        Kb,
        Mb,
        Gb,
    }
}