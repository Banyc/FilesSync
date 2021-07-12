using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FilesSync.Core.Models
{
    public class DirectoryModel : FileSystemUnitModel
    {
        public Dictionary<string, FileModel> Files { get; set; } = new();
        public Dictionary<string, DirectoryModel> Directories { get; set; } = new();

        public FileSystemUnitModel GetTarget(string relativePath)
        {
            return this.GetTargetImpl(relativePath.Split("/"), 0);
        }

        public bool InsertUnit(FileSystemUnitModel unit, string relativePath)
        {
            return this.InsertUnitImpl(unit, relativePath.Split("/"), 0);
        }

        public void UpdateParentsOfOffspring()
        {
            foreach (var (_, file) in this.Files)
            {
                file.Parent = this;
            }
            foreach (var (_, directory) in this.Directories)
            {
                directory.Parent = this;
                directory.UpdateParentsOfOffspring();
            }
        }

        private FileSystemUnitModel GetTargetImpl(string[] splitPath, int splitPathIndex)
        {
            string targetName = splitPath[splitPathIndex];
            if (this.Directories.ContainsKey(targetName))
            {
                return this.Directories[targetName].GetTargetImpl(splitPath, splitPathIndex + 1);
            }
            else if (this.Files.ContainsKey(targetName) && splitPath.Length - 1 == splitPathIndex)
            {
                return this.Files[targetName];
            }
            return null;
        }

        private bool InsertUnitImpl(FileSystemUnitModel unit, string[] splitPath, int splitPathIndex)
        {
            if (splitPath.Length == splitPathIndex)
            {
                if (unit is FileModel fileModel)
                {
                    this.Files[fileModel.Name] = fileModel;
                }
                else if (unit is DirectoryModel directoryModel)
                {
                    this.Directories[directoryModel.Name] = directoryModel;
                }
                else
                {
                    return false;
                }
                unit.Parent = this;
                return true;
            }
            string targetName = splitPath[splitPathIndex];
            if (this.Directories.ContainsKey(targetName))
            {
                return this.Directories[targetName].InsertUnitImpl(unit, splitPath, splitPathIndex + 1);
            }
            else
            {
                return false;
            }
        }
    }
}
