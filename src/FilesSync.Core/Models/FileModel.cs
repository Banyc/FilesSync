using System;
namespace FilesSync.Core.Models
{
    public class FileModel
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public byte[] MD5 { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
