namespace TCAdminBackupManager.Configuration
{
    public class BackupProviderConfiguration
    {
        public bool Enabled { get; set; }
        
        public long QuotaBytes { get; set; }
    }
}