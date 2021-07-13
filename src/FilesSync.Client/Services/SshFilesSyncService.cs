using System.Threading;
using System.Threading.Tasks;
using FilesSync.Core;
using FilesSync.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FilesSync.Client.Services
{
    public class SshFilesSyncService : IHostedService
    {
        private readonly SshFilesSyncSettings settings;
        public SshFilesSyncService(IOptions<SshFilesSyncSettings> options)
        {
            this.settings = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            FilesSyncBuilder builder = new();
            SshFilesSync filesSync = builder.GetSshFilesSync(settings);
            filesSync.UpdateFilesChangesDuringDownTime();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
