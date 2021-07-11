namespace FilesSync.Core.Models
{
    public class SshFileSenderSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string RemoteFolder { get; set; }
        public string LocalFolder { get; set; }
    }
}
