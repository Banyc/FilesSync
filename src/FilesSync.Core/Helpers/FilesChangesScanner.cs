using System.Linq;
using System.IO;
using System.Collections.Generic;
using FilesSync.Core.Models;

namespace FilesSync.Core.Helpers
{
    public class FilesChangesScanner
    {
        public string FolderPath { get; set; }

        public List<string> GetChangedUnitsPaths(DirectoryModel oldFolderState)
        {
            return GetChangedUnitsPaths(this.FolderPath, oldFolderState);
        }

        // ignore new recursive empty folders
        private static List<string> GetChangedUnitsPaths(string folderPath, DirectoryModel oldFolderState)
        {
            DirectoryInfo currentDirectory = new(folderPath);

            List<string> modifiedUnitsPaths = new();

            void TravelDirectories(DirectoryInfo currentDirectory, DirectoryModel oldDirectory)
            {
                var files = currentDirectory.GetFiles();
                HashSet<string> currentFilesNames = new();
                // find modified files and new files
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
                            modifiedUnitsPaths.Add(file.FullName);
                        }
                    }
                    else
                    {
                        // found new file
                        modifiedUnitsPaths.Add(file.FullName);
                    }
                    currentFilesNames.Add(file.Name);
                }
                // find deleted files
                HashSet<string> stateFilesNames = new(oldDirectory.Files.Keys);
                var deletedFilesNames = stateFilesNames.Except(currentFilesNames);
                foreach (var deletedFileName in deletedFilesNames)
                {
                    modifiedUnitsPaths.Add(oldDirectory.Files[deletedFileName].Path);
                }
                // find deleted folders
                HashSet<string> stateDirectoriesNames = new(oldDirectory.Directories.Keys);
                var deletedDirectoriesNames = stateDirectoriesNames.Except(currentDirectory.GetDirectories().Select(xxxx => xxxx.Name));
                foreach (var deletedDirectoryName in deletedDirectoriesNames)
                {
                    modifiedUnitsPaths.Add(oldDirectory.Directories[deletedDirectoryName].Path);
                }

                // travel the subfolders
                var directories = currentDirectory.GetDirectories();
                foreach (var directory in directories)
                {
                    if (!oldDirectory.Directories.ContainsKey(directory.Name))
                    {
                        oldDirectory.Directories[directory.Name] = new()
                        {
                            Name = directory.Name,
                            Parent = oldDirectory,
                        };
                    }
                    TravelDirectories(directory, oldDirectory.Directories[directory.Name]);
                }
            }

            TravelDirectories(currentDirectory, oldFolderState);

            return modifiedUnitsPaths;
        }
    }
}
