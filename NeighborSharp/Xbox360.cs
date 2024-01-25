using System.Net;
using System.Runtime.InteropServices;
using NeighborSharp.Types;

namespace NeighborSharp
{
    public class Xbox360
    {
        public IPEndPoint EndPoint { get; }
        public string? DebugName { get; private set; }

        private void FetchConsoleInfo()
        {
            XBDMConnection conn = new(this);
            DebugName = conn.Command("dbgname").message;
        }

        public Xbox360(string ip)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(ip), 730);
            FetchConsoleInfo();
        }

        public Xbox360(IPAddress ip)
        {
            EndPoint = new IPEndPoint(ip, 730);
            FetchConsoleInfo();
        }

        public override string ToString()
        {
            return $"{DebugName} on {EndPoint.Address}";
        }

        public string GetRunningProcess()
        {
            XBDMConnection conn = new(this);
            XboxArguments args = conn.CommandMultilineArg("xbeinfo running");
            return args.stringValues["name"];
        }

        public byte[] ReadBytes(uint addr, int len)
        {
            XBDMConnection conn = new(this);
            XboxArguments args = conn.CommandMultilineArg($"getmem addr={addr} length={len}");
            return MemoryHelper.HexStringToByteArray(string.Join(string.Empty, args.commands));
        }

        public T Read<T>(uint addr) where T : unmanaged
        {
            var data = ReadBytes(addr, Marshal.SizeOf(typeof(T)));

            if (data.Length <= 0)
                return default;

            return MemoryHelper.ByteArrayToStructure<T>(data.Reverse().ToArray());
        }

        public XboxResponse WriteBytes(uint addr, byte[] data)
        {
            XBDMConnection conn = new(this);

            var cmd = $"setmem addr={addr} data=";

            foreach (var b in data)
                cmd += b.ToString("X2");

            return conn.Command(cmd);
        }

        public XboxResponse Write<T>(uint addr, T data) where T : unmanaged
        {
            return WriteBytes(addr, MemoryHelper.StructureToByteArray(data).Reverse().ToArray());
        }

        public XboxDrive[] GetDriveList()
        {
            XBDMConnection conn = new(this);
            List<XboxDrive> drives = new();
            XboxArguments[] args = conn.CommandMultiline("drivelist");
            foreach(XboxArguments arg in args)
            {
                string letter = arg.stringValues["drivename"];
                XboxArguments commargs = new();
                commargs.stringValues["name"] = $"{letter}:\\";
                try
                {
                    XboxArguments driveargs = conn.CommandMultilineArg("drivefreespace " + commargs.ToString());
                    drives.Add(new(letter, driveargs));
                }
                catch (Exception)
                {
                    drives.Add(new(letter));
                }
            }
            return drives.ToArray();
        }

        public XboxFileEntry[] GetDirectoryList(string path)
        {
            XBDMConnection conn = new(this);
            List<XboxFileEntry> files = new();
            XboxArguments commargs = new();
            commargs.stringValues["name"] = path;
            XboxArguments[] args = conn.CommandMultiline("dirlist " + commargs.ToString());
            foreach (XboxArguments arg in args)
            {
                files.Add(new(arg, path));
            }
            return files.ToArray();
        }

        public void RunTitle(string title, string directory)
        {
            XBDMConnection conn = new(this);
            XboxArguments args = new();
            args.stringValues["title"] = title;
            args.stringValues["directory"] = directory;
            string command = "magicboot " + args.ToString();
            conn.Command(command);
        }

        public void RunTitle(string title)
        {
            string[] cracked = title.Split('\\');
            RunTitle(title, string.Join('\\', cracked.SkipLast(1)));
        }

        public void ColdReboot()
        {
            XBDMConnection conn = new(this);
            conn.Command("magicboot COLD");
        }

        public void TitleReboot()
        {
            XBDMConnection conn = new(this);
            conn.Command("magicboot");
        }

        public void Shutdown()
        {
            XBDMConnection conn = new(this);
            conn.Command("shutdown");
        }

        public byte[] DownloadFile(string filename)
        {
            XBDMConnection conn = new(this);
            XboxArguments args = new();
            args.stringValues["name"] = filename;
            string command = "getfile " + args.ToString();
            return conn.CommandBinary(command);
        }

        public void UploadFile(string filename, byte[] bytes)
        {
            XBDMConnection conn = new(this);
            XboxArguments args = new();
            args.stringValues["name"] = filename;
            args.intValues["length"] = (uint)bytes.Length;
            string command = "sendfile " + args.ToString();
            XboxResponse response = conn.Command(command);
            if (response.statusCode != 204)
                throw new Exception("Did not get message to upload binary data.");
            conn.Stream.Write(bytes);
        }
    }
}