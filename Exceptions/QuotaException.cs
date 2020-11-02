using System;
using TCAdminBackupManager.Models;
using TCAdminBackupManager.Models.Objects;

namespace TCAdminBackupManager.Exceptions
{
    public class QuotaException : Exception
    {
        public readonly Backup Backup;
        public readonly BackupRequest Request;

        public QuotaException(Backup backup, BackupRequest request)
        {
            Backup = backup;
            Request = request;
        }

        public override string Message { get; } = $"Executing backup request will exceed quota.";
    }
}