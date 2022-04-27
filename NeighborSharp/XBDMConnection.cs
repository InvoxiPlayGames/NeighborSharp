using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NeighborSharp
{
    internal class XBDMConnection : IDisposable
    {
        private TcpClient Connection { get; }
        private NetworkStream Stream { get; }

        public XBDMConnection(Xbox360 xbox)
        {
            Connection = new();
            try
            {
                Connection.Connect(xbox.EndPoint);
                Connection.ReceiveTimeout = 2000;
                Stream = Connection.GetStream();
                while (Stream.ReadByte() != '\n');
            } catch (Exception)
            {
                throw new Exception($"Failed to intialise connection to {xbox.DebugName} ({xbox.EndPoint})");
            }
        }

        public void Dispose()
        {
            try
            {
                Command("BYE");
                Connection.Close();
            } catch (Exception)
            {

            }
        }

        private string ReadLine()
        {
            int readbyte;
            string read = "";
            while (true)
            {
                readbyte = Stream.ReadByte();
                if (readbyte == '\r') break;
                read += (char)readbyte;
            }
            Stream.ReadByte(); // flush \n
            return read;
        }

        private void RunCommand(string command)
        {
            command += "\r\n";
            Stream.Write(Encoding.ASCII.GetBytes(command));
        }

        public XboxResponse Command(string command)
        {
            RunCommand(command);
            return new XboxResponse(ReadLine());
        }

        public string[] CommandMultilineStrings(string command)
        {
            RunCommand(command);
            XboxResponse response = new(ReadLine());
            if (response.statusCode != 202)
                throw new Exception($"Multiline command returned {response.statusCode}: {response.message}");
            List<string> readLines = new();
            while (true)
            {
                string readLine = ReadLine();
                if (readLine == ".") break;
                readLines.Add(readLine);
            }
            return readLines.ToArray();
        }

        public XboxArguments CommandMultilineArg(string command) {
            string[] lines = CommandMultilineStrings(command);
            XboxArguments args = new();
            foreach(string line in lines)
                args.AddArguments(line);
            return args;
        }
        public XboxArguments[] CommandMultiline(string command)
        {
            string[] lines = CommandMultilineStrings(command);
            List<XboxArguments> args = new();
            foreach (string line in lines)
                args.Add(new XboxArguments(line));
            return args.ToArray();
        }

        /*public byte[] CommandBinary(string command)
        {

        }*/
    }
}
