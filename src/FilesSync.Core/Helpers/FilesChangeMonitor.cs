using System.IO;
using System.Collections.Generic;
using FilesSync.Core.Models;
using System.Text.Json;
namespace FilesSync.Core.Helpers
{
    public class FilesChangeMonitor
    {
        // file system state
        public DirectoryModel State { get; set; }
        // runtime watcher
        public FileSystemWatcher FileSystemWatcher { get; }
        // scanner
        public FilesChangesScanner FilesChangesScanner { get; }

        private readonly FilesChangeMonitorSettings settings;

        public bool HasNewFileEventDuringScan { get; private set; } = false;
        private bool isScanning = false;

        public FilesChangeMonitor(FilesChangeMonitorSettings settings)
        {
            this.settings = settings;

            // load state
            if (File.Exists(settings.StatePersistencePath))
            {
                this.State = JsonSerializer.Deserialize<DirectoryModel>(File.ReadAllText(settings.StatePersistencePath));
                this.State.Name = settings.DirectoryPathToMonitor;
                this.State.UpdateParentsOfOffspring();
            }
            else
            {
                this.State = new()
                {
                    Name = settings.DirectoryPathToMonitor,
                    Parent = null,
                };
                Directory.CreateDirectory(Path.GetDirectoryName(settings.StatePersistencePath));
            }

            // create monitoring folder
            Directory.CreateDirectory(settings.DirectoryPathToMonitor);

            this.FileSystemWatcher = new();
            this.FileSystemWatcher.IncludeSubdirectories = true;
            this.FileSystemWatcher.Path = settings.DirectoryPathToMonitor;

            this.FileSystemWatcher.Changed += OnWatcherEvent;
            this.FileSystemWatcher.Created += OnWatcherEvent;
            this.FileSystemWatcher.Deleted += OnWatcherEvent;
            this.FileSystemWatcher.Renamed += OnWatcherEvent;
            this.FileSystemWatcher.Error += OnWatcherError;

            this.FileSystemWatcher.EnableRaisingEvents = true;

            this.FilesChangesScanner = new()
            {
                FolderPath = settings.DirectoryPathToMonitor,
            };
        }

        public List<string> GetChangedUnitsPaths()
        {
            lock (this)
            {
                this.isScanning = true;
            }
            var result = this.FilesChangesScanner.GetChangedUnitsPaths(this.State);
            lock (this)
            {
                this.isScanning = false;
            }
            return result;
        }

        public void CommitCreate(string localFilePath)
        {
            lock (this)
            {
                CreateFileInState(localFilePath);
                SaveState();
            }
        }

        public void CommitDelete(string localFilePath)
        {
            lock (this)
            {
                DeleteFromState(localFilePath);
                SaveState();
            }
        }

        public void CommitMoveFile(string oldLocalFilePath, string newLocalFilePath)
        {
            lock (this)
            {
                DeleteFromState(oldLocalFilePath);
                CreateFileInState(newLocalFilePath);
                SaveState();
            }
        }

        public void CommitMoveDirectory(string oldDirectoryPath, string newDirectoryPath)
        {
            lock (this)
            {
                var directoryModel = (DirectoryModel)DeleteFromState(oldDirectoryPath);
                CreateFolderInState(directoryModel, newDirectoryPath);
                SaveState();
            }
        }

        private void CreateFileInState(string localFilePath)
        {
            FileInfo fileInfo = new(localFilePath);
            (string targetName, DirectoryModel directoryModel) = GetTargetNameAndParentDirectoryModel(localFilePath);
            directoryModel.Files[targetName] = new()
            {
                LastWriteTime = fileInfo.LastWriteTime,
                Name = fileInfo.Name,
                Parent = directoryModel,
            };
        }

        private void CreateFolderInState(DirectoryModel directoryModel, string localDirectoryPath)
        {
            (string targetName, DirectoryModel parentDirectoryModel) = GetTargetNameAndParentDirectoryModel(localDirectoryPath);
            directoryModel.Parent = parentDirectoryModel;
            directoryModel.Name = targetName;
            parentDirectoryModel.Directories[directoryModel.Name] = directoryModel;
        }

        private FileSystemUnitModel DeleteFromState(string localFilePath)
        {
            FileSystemUnitModel deletedTarget = null;
            (string targetName, DirectoryModel directoryModel) = GetTargetNameAndParentDirectoryModel(localFilePath);
            if (directoryModel.Files.ContainsKey(targetName))
            {
                deletedTarget = directoryModel.Files[targetName];
                directoryModel.Files.Remove(targetName);
            }
            else if (directoryModel.Directories.ContainsKey(targetName))
            {
                deletedTarget = directoryModel.Directories[targetName];
                directoryModel.Directories.Remove(targetName);
            }
            // remove empty folders
            while (directoryModel.Files.Count == 0 && directoryModel.Directories.Count == 0)
            {
                string directoryName = directoryModel.Name;
                directoryModel = directoryModel.Parent;
                directoryModel.Directories.Remove(directoryName);
            }
            return deletedTarget;
        }

        private void OnWatcherEvent(object sender, FileSystemEventArgs e)
        {
            lock (this)
            {
                if (isScanning)
                {
                    this.HasNewFileEventDuringScan = true;
                }
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            lock (this)
            {
                if (isScanning)
                {
                    this.HasNewFileEventDuringScan = true;
                }
            }
        }

        private void SaveState()
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
            };
            File.WriteAllText(this.settings.StatePersistencePath, JsonSerializer.Serialize(this.State, options));
        }

        private (string, DirectoryModel) GetTargetNameAndParentDirectoryModel(string localFilePath)
        {
            string relativePath = Path.GetRelativePath(this.settings.DirectoryPathToMonitor, localFilePath);
            string[] pathPartitions = relativePath.Split(Path.DirectorySeparatorChar);
            DirectoryModel currentDirectory = this.State;
            for (int i = 0; i < pathPartitions.Length - 1; i++)
            {
                if (!currentDirectory.Directories.ContainsKey(pathPartitions[i]))
                {
                    currentDirectory.Directories[pathPartitions[i]] = new()
                    {
                        Name = pathPartitions[i],
                        Parent = currentDirectory,
                    };
                }
                currentDirectory = currentDirectory.Directories[pathPartitions[i]];
            }
            return (pathPartitions[^1], currentDirectory);
        }
    }
}
