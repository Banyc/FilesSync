using System.IO;
using FilesSync.Core.Models;
using Renci.SshNet;

namespace FilesSync.Core.Helpers
{
    public enum SshFilesSenderOperation
    {
        CreateOrModify,
        Delete,
        RenameOrMove,
    }

    public class SshFilesSender
    {
        private readonly SshFileSenderSettings settings;

        public SshFilesSender(SshFileSenderSettings settings)
        {
            this.settings = settings;
        }

        public void Create(string localFilePath)
        {
            lock (this)
            {
                string remoteFilePath = GetRemoteFilePath(localFilePath);
                using SftpClient client = GetSftpClient();

                client.Connect();
                using FileStream localFile = File.OpenRead(localFilePath);
                string remoteDirectoryPath = Path.GetDirectoryName(remoteFilePath);
                remoteDirectoryPath = remoteDirectoryPath.Replace('\\', '/');
                if (!client.Exists(remoteDirectoryPath))
                {
                    client.CreateDirectory(remoteDirectoryPath);
                }
                if (client.Exists(remoteFilePath))
                {
                    client.DeleteFile(remoteFilePath);
                }
                client.UploadFile(localFile, remoteFilePath);
                client.Disconnect();
            }
        }

        public void Delete(string localFilePath)
        {
            lock (this)
            {
                string remoteFilePath = GetRemoteFilePath(localFilePath);
                // using SshClient client = GetSshClient();
                using SftpClient client = GetSftpClient();

                client.Connect();
                // client.RunCommand($"rm -f \"{remoteFilePath}\"");
                client.DeleteFile(remoteFilePath);
                // TODO: remove empty folders
                client.Disconnect();
            }
        }

        public void Move(string localOldFilePath, string localNewFilePath)
        {
            lock (this)
            {
                string remoteOldFilePath = GetRemoteFilePath(localOldFilePath);
                string remoteNewFilePath = GetRemoteFilePath(localNewFilePath);
                // using SshClient client = GetSshClient();
                using SftpClient client = GetSftpClient();

                client.Connect();
                // client.RunCommand($"mv \"{remoteOldFilePath}\" \"{remoteNewFilePath}\"");
                client.RenameFile(remoteOldFilePath, remoteNewFilePath);
                // TODO: remove empty folders
                client.Disconnect();
            }
        }

        private string GetRemoteFilePath(string localFilePath)
        {
            string relativePath = Path.GetRelativePath(this.settings.LocalFolder, localFilePath);
            string remoteFilePath = Path.Combine(this.settings.RemoteFolder, relativePath);
            return remoteFilePath.Replace('\\', '/');
        }

        private SshClient GetSshClient()
        {
            SshClient client =
                new(host: this.settings.Host,
                    port: this.settings.Port,
                    username: this.settings.Username,
                    password: this.settings.Password);
            return client;
        }

        private ScpClient GetScpClient()
        {
            ScpClient client =
                new(host: this.settings.Host,
                    port: this.settings.Port,
                    username: this.settings.Username,
                    password: this.settings.Password);
            return client;
        }

        private SftpClient GetSftpClient()
        {
            SftpClient client =
                new(host: this.settings.Host,
                    port: this.settings.Port,
                    username: this.settings.Username,
                    password: this.settings.Password);
            return client;
        }
    }
}
