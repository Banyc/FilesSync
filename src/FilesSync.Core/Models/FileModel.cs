using System;
namespace FilesSync.Core.Models
{
    public class FileModel : FileSystemUnitModel
    {
        public byte[] MD5 { get; set; }
        public DateTime LastWriteTime { get; set; }
        public int Size { get; set; }
    }
}
