namespace TCAdminBackupManager.Models
{
    public class BackupResponse
    { 
        public bool Success { get; set; }

        public BackupResponse(bool success)
        {
            Success = success;
        }
    }
}