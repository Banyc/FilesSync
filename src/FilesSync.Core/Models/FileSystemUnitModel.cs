using System.Text.Json.Serialization;

namespace FilesSync.Core.Models
{
    public class FileSystemUnitModel
    {
        public string Name { get; set; }
        [JsonIgnore]
        public DirectoryModel Parent { get; set; }

        // public string RelativePath
        // {
        //     get
        //     {
        //         if (this.Parent == null)
        //         {
        //             return "";
        //         }
        //         else
        //         {
        //             if (this.Parent.RelativePath == "")
        //             {
        //                 return $"{this.Name}";
        //             }
        //             else
        //             {
        //                 return $"{this.Parent.RelativePath}/{this.Name}";
        //             }
        //         }
        //         // if (this.Parent == null)
        //         // {
        //         //     return this.Name;
        //         // }
        //         // else
        //         // {
        //         //     return $"{this.Parent.RelativePath}/{this.Name}";
        //         // }
        //     }
        // }

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
