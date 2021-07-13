# Files Sync

Deploy files on servers via the local file system in real-time.

## Features

- Synchronize filesystem from a local folder to a remote folder for transparent files deployment.

- The local folder is an active publisher, while the remote folder is a passive subscriber.

- No remote-side client is needed. File transmission is via SSH protocol. 

- Each remote folder only corresponds to one local folder, but each local folder can have multiply remote folders.

## Limitations

- Any change on the remote file system will not result in the change on the local side.

## Todo

- [ ] Support full synchronization.
