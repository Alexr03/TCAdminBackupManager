using System;
using System.Collections.Generic;
using System.IO;

namespace TCAdminBackupManager.Models
{
    public class BackupRequest
    {
        public string Name { get; set; }
        
        public int ProviderId { get; set; }

        private string _path = "";

        public string Path
        {
            get => _path;
            set
            {
                Console.WriteLine("Setter was called. - Input: " + value);
                if (value != "\\" || value != "/")
                {
                    _path = value;
                }   
            }
        }

        public List<BackupRequestFile> Files { get; set; } = new List<BackupRequestFile>();
        public List<BackupRequestDirectory> Directories { get; set; } = new List<BackupRequestDirectory>();
    }

    public class BackupRequestFile
    {
        public string Name { get; set; }
        public string Extension { get; set; }

        public BackupRequestFile()
        {
        }

        public BackupRequestFile(string name, string extension)
        {
            Name = name;
            Extension = extension;
        }

        public BackupRequestFile(string name)
        {
            Name = Path.GetFileNameWithoutExtension(name);
            Extension = Path.GetExtension(name);
        }
    }

    public class BackupRequestDirectory
    {
        public string Name { get; set; }

        public BackupRequestDirectory()
        {
        }

        public BackupRequestDirectory(string name)
        {
            Name = name;
        }
    }
}