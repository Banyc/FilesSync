using System.IO;
using System.Threading.Tasks;
using FilesSync.Core.Helpers;
using FilesSync.Core.Models;

namespace FilesSync.Core
{
    public class SshFilesSync
    {
        private readonly FilesChangeMonitor monitor;
        private readonly SshFilesSender sender;
        private bool isScanning = false;
        private bool hasNewScanningRequest = false;

        private Task scanningTask;

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
            ScheduleNewScanning();
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            try
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
            catch (Renci.SshNet.Common.SshException ex)
            {
                ScheduleNewScanning();
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            ScheduleNewScanning();
        }

        private void ScheduleNewScanning()
        {
            lock (this.monitor)
            {
                if (this.isScanning)
                {
                    this.hasNewScanningRequest = true;
                }
                else
                {
                    this.isScanning = true;
                    scanningTask = Task.Run(() => ScanAndUpdate());
                }
            }
        }

        private void ScanAndUpdate()
        {
            this.isScanning = true;
            do
            {
                System.Diagnostics.Debug.WriteLine("[scanner] Begin scanning");
                try
                {
                    this.hasNewScanningRequest = false;

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
                catch (Renci.SshNet.Common.SshException ex)
                {
                    this.hasNewScanningRequest = true;
                }
                System.Diagnostics.Debug.WriteLine("[scanner] End scanning");
            }
            // if new file event occurs during scanning, then the scanning result is not valid.
            while (this.monitor.HasNewFileEventDuringScan ||
                   this.hasNewScanningRequest);

            this.isScanning = false;
        }
    }
}
