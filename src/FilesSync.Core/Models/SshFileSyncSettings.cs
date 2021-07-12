namespace FilesSync.Core.Models
{
    public class SshFilesSyncSettings
    {
        public FilesChangeMonitorSettings FilesChangeMonitor { get; set; }
        public SshFileSenderSettings SshFileSender { get; set; }
    }
}
