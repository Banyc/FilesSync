using System.Collections.Generic;
namespace FilesSync.Core.Models
{
    public class DirectoryModel
    {
        public DirectoryModel Parent { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public Dictionary<string, FileModel> Files { get; set; } = new();
        public Dictionary<string, DirectoryModel> Directories { get; set; } = new();
    }
}
