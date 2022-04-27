using System;
using System.Net;
using NeighborSharp;

namespace NeighborTool
{
    internal class Program
    {
        public static string BytesToString(double bytes, int round)
        {
            if (bytes > Math.Pow(1000, 4)) return $"{Math.Round(bytes / Math.Pow(1000, 4), round)} TB";
            if (bytes > Math.Pow(1000, 3)) return $"{Math.Round(bytes / Math.Pow(1000, 3), round)} GB";
            if (bytes > Math.Pow(1000, 2)) return $"{Math.Round(bytes / Math.Pow(1000, 2), round)} MB";
            if (bytes > Math.Pow(1000, 1)) return $"{Math.Round(bytes / Math.Pow(1000, 1), round)} KB";
            return $"{bytes} B";
        }

        static void PrintUsage()
        {
            Console.WriteLine("usage: NeighborTool <console IP> <command> [args...]");
            Console.WriteLine();
            Console.WriteLine("available commands:");
            Console.WriteLine("  info - Lists the name and currently running title of the console.");
            Console.WriteLine("  listdisks - Lists all mounted drives available to the console.");
            Console.WriteLine("  listdir <directory> - Lists all files and subfolders in a directory on the console.");
            Console.WriteLine("  launch <remote file> [remote directory] - Launches an XEX on the console, optionally with a launch directory.");
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage(); return;
            }
            if (args[0].ToLower() == "discover")
            {
                DiscoveredConsole[] consoles = Xbox360Discovery.DiscoverAllConsoles();
                foreach(DiscoveredConsole console in consoles)
                {
                    Console.WriteLine($"{console.endpoint}: {console.name}");
                }
                return;
            }
            Xbox360 xbox = new(args[0]);
            switch (args[1])
            {
                case "info":
                    Console.WriteLine($"Name: {xbox.DebugName}");
                    Console.WriteLine($"Running: {xbox.GetRunningProcess()}");
                    break;
                case "listdisks":
                    XboxDrive[] drives = xbox.GetDriveList();
                    Console.WriteLine($"Drives mounted: {drives.Length}");
                    foreach (XboxDrive drive in drives)
                    {
                        Console.WriteLine($"{drive.driveLetter}:");
                        Console.WriteLine($"  Free: {BytesToString(drive.freeBytes, 2)}");
                        Console.WriteLine($"  Total: {BytesToString(drive.totalBytes, 2)}");
                    }
                    break;
                case "listdir":
                    if (args.Length < 3)
                    {
                        PrintUsage(); return;
                    }
                    XboxFileEntry[] files = xbox.GetDirectoryList(args[2]);
                    foreach(XboxFileEntry file in files)
                    {
                        Console.WriteLine($"{file.createdTime} - {file.fileName}");
                        if (file.isDirectory)
                            Console.WriteLine($"  Directory");
                        else
                            Console.WriteLine($"  Size: {BytesToString(file.fileSize, 2)}");
                    }
                    break;
                case "launch":
                    if (args.Length < 3)
                    {
                        PrintUsage(); return;
                    }
                    if (args.Length == 4)
                        xbox.RunTitle(args[2], args[3]);
                    else
                        xbox.RunTitle(args[2]);
                    break;
            }
        }
    }
}