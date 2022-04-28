using System.Net;
using System.Text;

namespace NeighborSharp.Types
{
    public class XboxResponse
    {
        public int statusCode;
        public string message;
        public XboxResponse(string line)
        {
            if (line.Length < 4)
                throw new Exception("Line is too short to parse");
            if (!int.TryParse(line.AsSpan(0, 3), out statusCode))
                throw new Exception("Status code could not be parsed from line.");
            message = line[5..];
        }
        public override string ToString()
        {
            return $"{statusCode}- {message}";
        }
    }

    public class XboxArguments
    {
        public Dictionary<string, string> stringValues = new();
        public Dictionary<string, uint> intValues = new();

        public XboxArguments() { }

        public XboxArguments(string line) { AddArguments(line); }

        public void AddArguments(string line)
        {
            string pendingKey = "";
            string pendingValue = "";
            bool parsingValue = false;
            bool inStringLiteral = false;
            bool parsedStringLiteral = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if ((c == ' ' && !inStringLiteral) || i == line.Length - 1)
                {
                    if (parsedStringLiteral)
                    {
                        if (c != ' ' && c != '"') pendingValue += c;
                        stringValues.Add(pendingKey, pendingValue);
                    }
                    else if (pendingKey != "")
                    {
                        if (!uint.TryParse(pendingValue, out uint parsedInt))
                        {
                            try
                            {
                                parsedInt = Convert.ToUInt32(pendingValue, 16);
                            }
                            catch (FormatException) // edge case very shit
                            {
                                pendingValue += c;
                                stringValues.Add(pendingValue.Trim(), "");
                            }
                        }
                        intValues.Add(pendingKey, parsedInt);
                    }
                    parsingValue = false;
                    pendingKey = "";
                    pendingValue = "";
                    parsedStringLiteral = false;
                } else if (c == '"')
                {
                    if (parsingValue) inStringLiteral = !inStringLiteral;
                    parsedStringLiteral = true;
                }
                else if (c == '=' && !parsingValue)
                {
                    pendingKey = pendingValue.Trim();
                    pendingValue = "";
                    parsingValue = true;
                    if (pendingKey == "string")
                    {
                        inStringLiteral = true;
                        parsedStringLiteral = true;
                    }
                }
                else
                {
                    pendingValue += c;
                }
            }
        }

        public override string ToString()
        {
            string ret = "";
            foreach (string key in stringValues.Keys)
            {
                ret += $"{key}=\"{stringValues[key]}\" ";
            }
            foreach (string key in intValues.Keys)
            {
                ret += $"{key}=0x{intValues[key]:X8} ";
            }
            return ret;
        }
    }

    public class XboxDrive
    {
        public long TotalBytes { get; }
        public long UsedBytes { get; }
        public long FreeBytes { get; }
        public string DriveLetter { get; }
        public XboxDrive(string drive)
        {
            DriveLetter = drive;
        }
        public XboxDrive(string drive, XboxArguments args)
        {
            DriveLetter = drive;
            TotalBytes = ((long)args.intValues["totalbyteshi"] << 32) | args.intValues["totalbyteslo"];
            FreeBytes = ((long)args.intValues["totalfreebyteshi"] << 32) | args.intValues["totalfreebyteslo"];
            UsedBytes = TotalBytes - FreeBytes;
        }
        public override string ToString()
        {
            return $"{DriveLetter}:";
        }
    }

    public class XboxFileEntry
    {
        public string FullPath { get; }
        public string FileName { get; }
        public bool IsDirectory { get; }
        public long FileSize { get; }
        public DateTime CreatedTime { get; }
        public DateTime ModifiedTime { get; }
        public XboxFileEntry(XboxArguments args, string directory)
        {
            FileName = args.stringValues["name"];
            FullPath = $"{directory}\\{FileName}";
            FileSize = ((long)args.intValues["sizehi"] << 32) | args.intValues["sizelo"];
            CreatedTime = DateTime.FromFileTimeUtc(((long)args.intValues["createhi"] << 32) | args.intValues["createlo"]);
            ModifiedTime = DateTime.FromFileTimeUtc(((long)args.intValues["changehi"] << 32) | args.intValues["changelo"]);
            IsDirectory = args.stringValues.ContainsKey("directory") || args.intValues.ContainsKey("directory");
        }

        public override string ToString()
        {
            return FullPath;
        }
    }

    public class DiscoveredConsole
    {
        public IPEndPoint EndPoint { get; }
        public string Name { get; }
        public DiscoveredConsole(IPEndPoint ep, byte[] datagram)
        {
            if (datagram[0] != 0x02)
                throw new Exception("Recieved incorrect type NAP packet.");
            int nameLength = datagram[1];
            EndPoint = ep;
            Name = Encoding.ASCII.GetString(datagram, 2, nameLength);
        }
        public override string ToString()
        {
            return $"{EndPoint}: {Name}";
        }
    }
}
