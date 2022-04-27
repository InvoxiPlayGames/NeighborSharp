using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NeighborSharp
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
                        stringValues.Add(pendingKey, pendingValue);
                    else
                    {
                        if (!uint.TryParse(pendingValue, out uint parsedInt))
                        {
                            try
                            {
                                parsedInt = Convert.ToUInt32(pendingValue, 16);
                            } catch (FormatException) // edge case very shit
                            {
                                pendingValue += c;
                                stringValues.Add(pendingValue, "");
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
                    pendingKey = pendingValue;
                    pendingValue = "";
                    parsingValue = true;
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
        public long totalBytes;
        public long usedBytes;
        public long freeBytes;
        public string driveLetter;
        public XboxDrive(string drive)
        {
            driveLetter = drive;
        }
        public XboxDrive(string drive, XboxArguments args)
        {
            driveLetter = drive;
            ParseArgs(args);
        }
        public void ParseArgs(XboxArguments args)
        {
            totalBytes = ((long)args.intValues["totalbyteshi"] << 32) | args.intValues["totalbyteslo"];
            freeBytes = ((long)args.intValues["totalfreebyteshi"] << 32) | args.intValues["totalfreebyteslo"];
            usedBytes = totalBytes - freeBytes;
        }
        public override string ToString()
        {
            return $"{driveLetter}:";
        }
    }

    public class XboxFileEntry
    {
        public string fullPath;
        public string fileName;
        public bool isDirectory;
        public long fileSize;
        public DateTime createdTime;
        public DateTime modifiedTime;
        public XboxFileEntry(XboxArguments args, string directory)
        {
            fileName = args.stringValues["name"];
            fullPath = $"{directory}\\{fileName}";
            fileSize = ((long)args.intValues["sizehi"] << 32) | args.intValues["sizelo"];
            createdTime = DateTime.FromFileTimeUtc(((long)args.intValues["createhi"] << 32) | args.intValues["createlo"]);
            modifiedTime = DateTime.FromFileTimeUtc(((long)args.intValues["changehi"] << 32) | args.intValues["changelo"]);
            isDirectory = args.stringValues.ContainsKey("directory") || args.intValues.ContainsKey("directory");
        }
    }

    public class DiscoveredConsole
    {
        public IPEndPoint endpoint;
        public string name;
        public DiscoveredConsole(IPEndPoint ep, byte[] datagram)
        {
            if (datagram[0] != 0x02)
                throw new Exception("Recieved incorrect type NAP packet.");
            int nameLength = datagram[1];
            endpoint = ep;
            name = Encoding.ASCII.GetString(datagram, 2, nameLength);
        }
    }
}
