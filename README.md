# NeighborSharp

A very work-in-progress Xbox 360 and OG Xbox XBDM client library written in C# for .NET 8.0.

## What Works

* Retrieving information on mounted drives
* Browsing directories
* Launching XBE/XEX files
* Uploading files
* Downloading files

## What Doesn't Works

* Not too thread-safe but it's fine

Debugging features (for Xbox 360) can be found in my [EmDbg](https://github.com/InvoxiPlayGames/EmDbg) project.

# NeighborTool

A barebones CLI tool for using NeighborSharp.

## Usage:

```
usage: NeighborTool [og:]<console IP | discover> <command> [args...]
  for OG Xbox, use the "og:" prefix before IP address
  if running a developer kernel, use the debug IP!

available commands:
  info - Lists the name and currently running title of the console.
  listdisks - Lists all mounted drives available to the console.
  listdir <directory> - Lists all files and subfolders in a directory on the console.
  launch <remote file> [remote directory] - Launches an XBE or XEX on the console, optionally with a launch directory.
  download <remote file> <local file> - Downloads a file from the console.
  upload <local file> <remote file> - Uploads a file to the console.

console discovery:
  discover all - Lists the IP addresses and names of all discovered consoles.
  discover <name> - Lists the IP address of the console matching that name, if one is discovered.
```