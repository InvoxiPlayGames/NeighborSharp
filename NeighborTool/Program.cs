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
            Console.WriteLine("usage: NeighborTool <console IP | discover> <command> [args...]");
            Console.WriteLine();
            Console.WriteLine("available commands:");
            Console.WriteLine("  info - Lists the name and currently running title of the console.");
            Console.WriteLine("  listdisks - Lists all mounted drives available to the console.");
            Console.WriteLine("  listdir <directory> - Lists all files and subfolders in a directory on the console.");
            Console.WriteLine("  launch <remote file> [remote directory] - Launches an XEX on the console, optionally with a launch directory.");
            Console.WriteLine("  mempeek <address> [length] - Peeks memory at the specified address.");
            Console.WriteLine("  mempoke <address> [i8/i16/i32/i64/u8/u16/u32/u64/f32/f64] <data> - Pokes memory at the specified address with the input data.");
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
            if (args.Length == 2 && args[0].ToLower() == "discover")
            {
                if (args[1].ToLower() == "all")
                {
                    DiscoveredConsole[] consoles = Xbox360Discovery.DiscoverAllConsoles();
                    foreach (DiscoveredConsole console in consoles)
                    {
                        Console.WriteLine(console);
                    }
                } else
                {
                    DiscoveredConsole? console = Xbox360Discovery.DiscoverConsoleByName(args[1]);
                    if (console != null)
                        Console.WriteLine(console);
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
                case "mempeek":
                    if (args.Length < 3)
                    {
                        PrintUsage(); return;
                    }
                    var peekAddr = Convert.ToUInt32(args[2], 16);
                    var peekLen = args.Length == 4 ? Convert.ToInt32(args[3]) : 64;
                    var peekResult = xbox.ReadBytes(peekAddr, peekLen);
                    if (peekResult.Length <= 0)
                        Console.WriteLine($"Failed to read data at 0x{peekAddr:X8}");
                    else
                        MemoryHelper.PrintBytes(peekResult, peekAddr);
                    break;

                case "mempoke":
                {
                    if (args.Length < 3)
                    {
                        PrintUsage();
                        return;
                    }

                    var pokeAddr = Convert.ToUInt32(args[2], 16);
                    var pokeLen = 0;
                    XboxResponse pokeResponse = null;

                    if (args.Length > 4)
                    {
                        switch (args[3])
                        {
                            case "i8":  pokeResponse = xbox.Write(pokeAddr, Convert.ToSByte(args[4]));  pokeLen = 1; break;
                            case "u8":  pokeResponse = xbox.Write(pokeAddr, Convert.ToByte(args[4]));   pokeLen = 1; break;
                            case "i16": pokeResponse = xbox.Write(pokeAddr, Convert.ToInt16(args[4]));  pokeLen = 2; break;
                            case "u16": pokeResponse = xbox.Write(pokeAddr, Convert.ToUInt16(args[4])); pokeLen = 2; break;
                            case "i32": pokeResponse = xbox.Write(pokeAddr, Convert.ToInt32(args[4]));  pokeLen = 4; break;
                            case "u32": pokeResponse = xbox.Write(pokeAddr, Convert.ToUInt32(args[4])); pokeLen = 4; break;
                            case "i64": pokeResponse = xbox.Write(pokeAddr, Convert.ToInt64(args[4]));  pokeLen = 8; break;
                            case "u64": pokeResponse = xbox.Write(pokeAddr, Convert.ToUInt64(args[4])); pokeLen = 8; break;
                            case "f32": pokeResponse = xbox.Write(pokeAddr, Convert.ToSingle(args[4])); pokeLen = 4; break;
                            case "f64": pokeResponse = xbox.Write(pokeAddr, Convert.ToDouble(args[4])); pokeLen = 8; break;
                        }
                    }
                    else
                    {
                        var pokeData = MemoryHelper.HexStringToByteArray(args[3]);

                        pokeResponse = xbox.WriteBytes(pokeAddr, pokeData);
                        pokeLen = pokeData.Length;
                    }

                    if (pokeResponse?.statusCode != 200)
                    {
                        Console.WriteLine($"Failed to write data to 0x{pokeAddr:X8}\nReason: {pokeResponse?.message}");
                    }
                    else
                    {
                        Console.WriteLine($"Successfully written data to 0x{pokeAddr:X8}\n");
                        MemoryHelper.PrintBytes(xbox.ReadBytes(pokeAddr, pokeLen), pokeAddr);
                    }

                    break;
                }

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