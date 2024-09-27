using NeighborSharp;
using NeighborSharp.Types;

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
            Console.WriteLine("usage: NeighborTool [og:]<console IP | discover> <command> [args...]");
            Console.WriteLine("  for OG Xbox, use the \"og:\" prefix before IP address");
            Console.WriteLine("  if running a developer kernel, use the debug IP!");
            Console.WriteLine();
            Console.WriteLine("available commands:");
            Console.WriteLine("  info - Lists the name and currently running title of the console.");
            Console.WriteLine("  listdisks - Lists all mounted drives available to the console.");
            Console.WriteLine("  listdir <directory> - Lists all files and subfolders in a directory on the console.");
            Console.WriteLine("  launch <remote file> [remote directory] - Launches an XBE or XEX on the console, optionally with a launch directory.");
            Console.WriteLine("  download <remote file> <local file> - Downloads a file from the console.");
            Console.WriteLine("  upload <local file> <remote file> - Uploads a file to the console.");
            Console.WriteLine();
            Console.WriteLine("console discovery:");
            Console.WriteLine("  discover all - Lists the IP addresses and names of all discovered consoles.");
            Console.WriteLine("  discover <name> - Lists the IP address of the console matching that name, if one is discovered.");
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage(); return;
            }
            bool isOg = false;
            if (args[0].ToLower().StartsWith("og:"))
            {
                isOg = true;
                args[0] = args[0].Substring(3);
            }
            if (args.Length == 2 && args[0].ToLower() == "discover")
            {
                IConsoleDiscovery discovery;
                if (!isOg)
                    discovery = new Xbox360Discovery();
                else
                    discovery = new XboxOGDiscovery();

                if (args[1].ToLower() == "all")
                {
                    DiscoveredConsole[] consoles = discovery.DiscoverAllConsoles();
                    foreach (DiscoveredConsole console in consoles)
                    {
                        Console.WriteLine(console);
                    }
                } else
                {
                    DiscoveredConsole? console = discovery.DiscoverConsoleByName(args[1]);
                    if (console != null)
                        Console.WriteLine(console);
                }
                return;
            }
            IXbox xbox;
            if (!isOg)
                xbox = new Xbox360(args[0]);
            else
                xbox = new XboxOG(args[0]);
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
                        Console.WriteLine($"{drive.DriveLetter}:");
                        Console.WriteLine($"  Free: {BytesToString(drive.FreeBytes, 2)}");
                        Console.WriteLine($"  Total: {BytesToString(drive.TotalBytes, 2)}");
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
                        Console.WriteLine($"{file.CreatedTime} - {file.FileName}");
                        if (file.IsDirectory)
                            Console.WriteLine($"  Directory");
                        else
                            Console.WriteLine($"  Size: {BytesToString(file.FileSize, 2)}");
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
                case "download":
                    if (args.Length < 4)
                    {
                        PrintUsage(); return;
                    }
                    File.WriteAllBytes(args[3], xbox.DownloadFile(args[2]));
                    break;
                case "upload":
                    if (args.Length < 4)
                    {
                        PrintUsage(); return;
                    }
                    byte[] filebytes = File.ReadAllBytes(args[2]);
                    xbox.UploadFile(args[3], filebytes);
                    break;
                case "shutdown":
                    xbox.Shutdown();
                    break;
            }
        }
    }
}