namespace TCAdminBackupManager.Configuration
{
    public class LocalProviderConfiguration : BackupProviderConfiguration
    {
        public string LocalDirectory { get; set; } = @"$[Service.RootDirectory]BackupManager\";
    }
}