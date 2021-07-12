using System.Text.Json.Serialization;

namespace FilesSync.Core.Models
{
    public class FileSystemUnitModel
    {
        public string Name { get; set; }
        [JsonIgnore]
        public DirectoryModel Parent { get; set; }

        public string Path
        {
            get
            {
                if (this.Parent == null)
                {
                    return this.Name;
                }
                else
                {
                    return $"{this.Parent.Path}/{this.Name}";
                }
            }
        }
    }
}
