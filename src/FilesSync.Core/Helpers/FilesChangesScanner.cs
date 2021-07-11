using System.Linq;
using System.IO;
using System.Collections.Generic;
using FilesSync.Core.Models;

namespace FilesSync.Core.Helpers
{
    public class FilesChangesScanner
    {
        public string FolderPath { get; set; }

        public List<string> GetChangedFilesPaths(DirectoryModel oldFolderState)
        {
            return GetChangedFilesPaths(this.FolderPath, oldFolderState);
        }

        public static List<string> GetChangedFilesPaths(string folderPath, DirectoryModel oldFolderState)
        {
            DirectoryInfo currentDirectory = new(folderPath);

            List<string> modifiedFilesPaths = new();

            void TravelDirectories(DirectoryInfo currentDirectory, DirectoryModel oldDirectory)
            {
                var files = currentDirectory.GetFiles();
                foreach (var file in files)
                {
                    if (oldDirectory.Files.ContainsKey(file.Name))
                    {
                        if (oldDirectory.Files[file.Name].LastWriteTime == file.LastWriteTime)
                        {
                            // file has been recorded
                        }
                        else
                        {
                            // found modified file
                            modifiedFilesPaths.Add(file.FullName);
                        }
                    }
                    else
                    {
                        // found new file
                        modifiedFilesPaths.Add(file.FullName);
                    }
                }
                var directories = currentDirectory.GetDirectories();
                foreach (var directory in directories)
                {
                    if (!oldDirectory.Directories.ContainsKey(directory.Name))
                    {
                        oldDirectory.Directories[directory.Name] = new()
                        {
                            Name = directory.Name,
                            Path = directory.FullName,
                        };
                    }
                    TravelDirectories(directory, oldDirectory.Directories[directory.Name]);
                }
            }

            TravelDirectories(currentDirectory, oldFolderState);

            return modifiedFilesPaths;
        }
    }
}