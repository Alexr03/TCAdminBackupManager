namespace TCAdminBackupManager.Configuration
{
    public class S3ProviderConfiguration : BackupProviderConfiguration
    {
        public string Host { get; set; }
        public string AccessId { get; set; }
        public string AccessSecret { get; set; }
        public string Region { get; set; } = "us-east-1";
    }
}