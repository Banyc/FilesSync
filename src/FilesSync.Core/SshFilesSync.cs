using System.IO;
using FilesSync.Core.Helpers;
using FilesSync.Core.Models;

namespace FilesSync.Core
{
    public class SshFilesSync
    {
        private readonly FilesChangeMonitor monitor;
        private readonly SshFilesSender sender;

        public SshFilesSync(FilesChangeMonitor monitor, SshFilesSender sender)
        {
            this.monitor = monitor;
            monitor.FileSystemWatcher.Changed += OnFileEvent;
            monitor.FileSystemWatcher.Created += OnFileEvent;
            monitor.FileSystemWatcher.Deleted += OnFileEvent;
            monitor.FileSystemWatcher.Renamed += OnFileEvent;
            monitor.FileSystemWatcher.Error += OnWatcherError;

            this.sender = sender;
        }

        public void UpdateFilesChangesDuringDownTime()
        {
            ScanAndUpdate();
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    // exclude directories
                    if (File.Exists(e.FullPath))
                    {
                        this.sender.Create(e.FullPath);
                        this.monitor.CommitCreate(e.FullPath);
                    }
                    break;
                case WatcherChangeTypes.Deleted:
                    this.sender.Delete(e.FullPath);
                    this.monitor.CommitDelete(e.FullPath);
                    break;
                case WatcherChangeTypes.Changed:
                    // exclude directories
                    if (File.Exists(e.FullPath))
                    {
                        this.sender.Create(e.FullPath);
                        this.monitor.CommitCreate(e.FullPath);
                    }
                    break;
                case WatcherChangeTypes.Renamed:
                    RenamedEventArgs eventArgs = (RenamedEventArgs)e;
                    this.sender.Move(eventArgs.OldFullPath, eventArgs.FullPath);
                    if (File.Exists(e.FullPath))
                    {
                        this.monitor.CommitMoveFile(eventArgs.OldFullPath, eventArgs.FullPath);
                    }
                    else
                    {
                        this.monitor.CommitMoveDirectory(eventArgs.OldFullPath, eventArgs.FullPath);
                    }
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    break;
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            ScanAndUpdate();
        }

        private void ScanAndUpdate()
        {
            do
            {
                var paths = this.monitor.GetChangedUnitsPaths();
                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        this.sender.Create(path);
                        this.monitor.CommitCreate(path);
                    }
                    else
                    {
                        this.sender.Delete(path);
                        this.monitor.CommitDelete(path);
                    }
                }
            }
            // if new file event occurs during scanning, then the scanning result is not valid.
            while (this.monitor.HasNewFileEventDuringScan);
        }
    }
}
