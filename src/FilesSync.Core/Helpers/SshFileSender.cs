using System.Collections.Generic;
using System.IO;
using FilesSync.Core.Models;
using Renci.SshNet;
using Renci.SshNet.Sftp;

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
                System.Diagnostics.Debug.WriteLine("[SSH] Begin Creating");
                string remoteFilePath = GetRemoteFilePath(localFilePath);
                using SftpClient client = GetSftpClient();

                client.Connect();
                if (File.Exists(localFilePath))
                {
                    // file has not been deleted or renamed
                    using FileStream localFile = File.OpenRead(localFilePath);
                    string remoteDirectoryPath = Path.GetDirectoryName(remoteFilePath);
                    remoteDirectoryPath = remoteDirectoryPath.Replace('\\', '/');
                    if (!client.Exists(remoteDirectoryPath))
                    {
                        // client.CreateDirectory(remoteDirectoryPath);
                        // recursive folder creation
                        SshClient sshClient = GetSshClient();
                        sshClient.Connect();
                        sshClient.RunCommand($"mkdir -p \"{remoteDirectoryPath}\"");
                        sshClient.Disconnect();
                    }
                    if (client.Exists(remoteFilePath))
                    {
                        client.DeleteFile(remoteFilePath);
                    }
                    client.UploadFile(localFile, remoteFilePath);
                }
                client.Disconnect();
                System.Diagnostics.Debug.WriteLine("[SSH] End Creating");
            }
        }

        public void Delete(string localFilePath)
        {
            lock (this)
            {
                System.Diagnostics.Debug.WriteLine("[SSH] Begin Deleting");
                string remoteFilePath = GetRemoteFilePath(localFilePath);
                // using SshClient client = GetSshClient();
                using SftpClient client = GetSftpClient();

                client.Connect();
                if (client.Exists(remoteFilePath))
                {
                    var attributes = client.GetAttributes(remoteFilePath);
                    if (attributes.IsRegularFile)
                    {
                        client.DeleteFile(remoteFilePath);
                    }
                    else
                    {
                        // delete folder via recursive deletion
                        using SshClient sshClient = GetSshClient();
                        sshClient.Connect();
                        sshClient.RunCommand($"rm -r \"{remoteFilePath}\"");
                        sshClient.Disconnect();
                    }
                    // remove empty folders
                    RemoveEmptyParentFolders(Path.GetDirectoryName(remoteFilePath).Replace("\\", "/"));
                }
                client.Disconnect();
                System.Diagnostics.Debug.WriteLine("[SSH] End Deleting");
            }
        }

        public void Move(string localOldFilePath, string localNewFilePath)
        {
            lock (this)
            {
                System.Diagnostics.Debug.WriteLine("[SSH] Begin Moving");
                string remoteOldFilePath = GetRemoteFilePath(localOldFilePath);
                string remoteNewFilePath = GetRemoteFilePath(localNewFilePath);
                // using SshClient client = GetSshClient();
                using SftpClient client = GetSftpClient();

                client.Connect();
                // empty folders have not been created in remote yet.
                if (client.Exists(remoteOldFilePath))
                {
                    client.RenameFile(remoteOldFilePath, remoteNewFilePath);
                    // remove empty folders
                    RemoveEmptyParentFolders(Path.GetDirectoryName(remoteOldFilePath).Replace("\\", "/"));
                }
                client.Disconnect();
                System.Diagnostics.Debug.WriteLine("[SSH] End Moving");
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

        private void RemoveEmptyParentFolders(string childPath)
        {
            using SftpClient client = GetSftpClient();
            client.Connect();

            while (true)
            {
                var list = client.ListDirectory(childPath);
                List<SftpFile> fileList = new(list);
                if (fileList.Count > 2)
                {
                    break;
                }
                client.DeleteDirectory(childPath);

                string parent = Path.GetDirectoryName(childPath).Replace("\\", "/");

                childPath = parent;
            }

            client.Disconnect();
        }
    }
}
