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
            }
            else
            {
                this.State = new()
                {
                    Name = Path.GetDirectoryName(settings.StatePersistencePath),
                    Path = settings.StatePersistencePath,
                    Parent = null,
                };
                File.Create(settings.StatePersistencePath);
            }

            this.FileSystemWatcher = new();
            this.FileSystemWatcher.IncludeSubdirectories = true;
            this.FileSystemWatcher.Path = settings.DirectoryPathToMonitor;

            this.FileSystemWatcher.Changed += OnWatcherEvent;
            this.FileSystemWatcher.Created += OnWatcherEvent;
            this.FileSystemWatcher.Deleted += OnWatcherEvent;
            this.FileSystemWatcher.Renamed += OnWatcherEvent;
            this.FileSystemWatcher.Error += OnWatcherError;

            this.FilesChangesScanner = new()
            {
                FolderPath = settings.DirectoryPathToMonitor,
            };
        }

        public List<string> GetChangedFilesPaths()
        {
            lock (this)
            {
                this.isScanning = true;
            }
            var result = this.FilesChangesScanner.GetChangedFilesPaths(this.State);
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
                CreateInState(localFilePath);
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

        public void CommitMove(string oldLocalFilePath, string newLocalFilePath)
        {
            lock (this)
            {
                DeleteFromState(oldLocalFilePath);
                CreateInState(newLocalFilePath);
                SaveState();
            }
        }

        private void CreateInState(string localFilePath)
        {
            FileInfo fileInfo = new(localFilePath);
            (string targetName, DirectoryModel directoryModel) = GetFileNameAndDirectoryModel(localFilePath);
            directoryModel.Files[targetName] = new()
            {
                LastWriteTime = fileInfo.LastWriteTime,
                Path = fileInfo.FullName,
                Name = fileInfo.Name,
            };
        }

        private void DeleteFromState(string localFilePath)
        {
            FileInfo fileInfo = new(localFilePath);
            (string targetName, DirectoryModel directoryModel) = GetFileNameAndDirectoryModel(localFilePath);
            if (directoryModel.Files.ContainsKey(targetName))
            {
                directoryModel.Files.Remove(targetName);
            }
            // remove empty folders
            while (directoryModel.Files.Count == 0)
            {
                string directoryName = directoryModel.Name;
                directoryModel = directoryModel.Parent;
                directoryModel.Directories.Remove(directoryName);
            }
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
            File.WriteAllText(this.settings.StatePersistencePath, JsonSerializer.Serialize(this.State));
        }

        private (string, DirectoryModel) GetFileNameAndDirectoryModel(string localFilePath)
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
                        Path = Path.Combine(currentDirectory.Path, pathPartitions[i]),
                        Parent = currentDirectory,
                    };
                }
                currentDirectory = currentDirectory.Directories[pathPartitions[i]];
            }
            return (pathPartitions[^1], currentDirectory);
        }
    }
}
