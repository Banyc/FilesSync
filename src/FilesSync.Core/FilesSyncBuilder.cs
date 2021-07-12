using FilesSync.Core.Helpers;
using FilesSync.Core.Models;

namespace FilesSync.Core
{
    public class FilesSyncBuilder
    {
        public SshFilesSync GetSshFilesSync(SshFilesSyncSettings settings)
        {
            FilesChangeMonitor monitor = new(settings.FilesChangeMonitor);
            SshFilesSender sender = new(settings.SshFileSender);
            SshFilesSync filesSync = new(monitor, sender);
            return filesSync;
        }
    }
}
