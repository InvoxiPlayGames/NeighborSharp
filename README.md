# NeighborSharp

A very work-in-progress Xbox 360 XBDM client library written in C# for .NET 6.0.

## What Works

* Retrieving information on mounted drives
* Browsing directories
* Launching XEX files

## What Doesn't (TODO)

* Uploading files
* Downloading files
* Listing loaded modules
* Debugging features (console, debugger, memory viewer, etc)

# NeighborTool

A barebones CLI tool for using NeighborSharp.

## Usage:

```
usage: NeighborTool <console IP> <command> [args...]

available commands:
  info - Lists the name and currently running title of the console.
  listdisks - Lists all mounted drives available to the console.
  listdir <directory> - Lists all files and subfolders in a directory on the console.
  launch <remote file> [remote directory] - Launches an XEX on the console, optionally with a launch directory.
```