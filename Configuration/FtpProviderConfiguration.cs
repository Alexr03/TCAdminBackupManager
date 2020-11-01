namespace TCAdminBackupManager.Configuration
{
    public class FtpProviderConfiguration : BackupProviderConfiguration
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Port { get; set; } = 21;
    }
}