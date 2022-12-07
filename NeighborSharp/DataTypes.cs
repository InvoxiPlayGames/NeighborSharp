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
        public Dictionary<string, ulong> longValues = new();
        public List<string> commands = new();

        public XboxArguments() { }

        public XboxArguments(string line) { AddArguments(line); }

        public void AddArguments(string line)
        {
            string pendingKey = "";
            string pendingValue = "";
            bool parsingKey = true;
            bool parsingValue = false;
            bool inStringLiteral = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (parsingKey)
                {
                    if ((c == ' ' && pendingKey != "") || i == line.Length - 1)
                    {
                        // if we're at the end of a key definition and there's 
                        // a space, this is a command
                        // if we're at the end, just add on the current character anyway
                        if (i == line.Length - 1) pendingKey += c;
                        commands.Add(pendingKey);
                        pendingKey = "";
                    } else if (c == '=' && pendingKey != "")
                    {
                        // if we're at the end of a key definition and there's
                        // an assignment value, start parsing a value
                        parsingKey = false;
                        if (pendingKey == "string") // the "string" value in debug logs is weird
                        { // so we go ahead and treat the rest as a string literal value
                            pendingValue = line.Substring(i + 1);
                            stringValues.Add(pendingKey, pendingValue);
                            i = line.Length;
                        } else
                        {
                            // start parsing the value after the =
                            parsingValue = true;
                        }
                    } else
                    {
                        // add the character to the pending key value
                        pendingKey += c;
                    }
                } else if (parsingValue)
                {
                    if ((c == ' ' && !inStringLiteral) || i == line.Length - 1)
                    {
                        // if we see a space and we *aren't* in the middle of a
                        // string literal, we're finished here, try to determine the type
                        uint tempuint = 0;
                        ulong tempulong = 0;
                        // if we're at the end, just add on the current character anyway
                        if (i == line.Length - 1 && c != '"') pendingValue += c;
                        if (pendingValue.StartsWith("0x") &&
                            !pendingValue.Contains(",")) // hexadecimal uint and not a vector type
                        {
                            tempuint = Convert.ToUInt32(pendingValue.Substring(2), 16);
                            intValues.Add(pendingKey, tempuint);
                        } else if (pendingValue.StartsWith("0q")) // hexadecimal ulong
                        {
                            tempulong = Convert.ToUInt64(pendingValue.Substring(2), 16);
                            longValues.Add(pendingKey, tempulong);
                        } else // neither of these thigns, try to parse as an integer. if not, we string!
                        {
                            if (ulong.TryParse(pendingValue, out tempulong))
                            {
                                if (tempulong <= uint.MaxValue) // if it can be an int, make it so.
                                {
                                    tempuint = (uint)tempulong;
                                    intValues.Add(pendingKey, tempuint);
                                } else
                                {
                                    longValues.Add(pendingKey, tempulong);
                                }
                            } else
                            {
                                stringValues.Add(pendingKey, pendingValue);
                            }
                        }
                        pendingKey = "";
                        pendingValue = "";
                        parsingValue = false;
                        parsingKey = true;
                    } else if (c == '"')
                    {
                        // if we're in a string literal and we see a quote, check if
                        // it's escaped. if it isn't, then end the string literal.
                        // if we aren't in a string literal, we are now!
                        if (!inStringLiteral || (inStringLiteral && line[i-1] != '\\'))
                        {
                            inStringLiteral = !inStringLiteral;
                        } 
                    } else
                    {
                        pendingValue += c;
                    }
                }
            }
        }

        public override string ToString()
        {
            string ret = "";
            foreach (string command in commands)
            {
                ret += $"{command} ";
            }
            foreach (string key in stringValues.Keys)
            {
                ret += $"{key}=\"{stringValues[key]}\" ";
            }
            foreach (string key in intValues.Keys)
            {
                ret += $"{key}=0x{intValues[key]:X8} ";
            }
            foreach (string key in longValues.Keys)
            {
                ret += $"{key}=0q{longValues[key]:X8} ";
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
            return $"{EndPoint.Address}{(Name.Length > 1 ? " - " + Name : "")}";
        }
    }
}
