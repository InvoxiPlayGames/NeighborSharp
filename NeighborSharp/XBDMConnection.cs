using System.Net.Sockets;
using System.Text;
using NeighborSharp.Types;

namespace NeighborSharp
{
    public class XBDMConnection : IDisposable
    {
        private TcpClient Connection { get; }
        public NetworkStream Stream { get; }

        public XBDMConnection(Xbox360 xbox)
        {
            Connection = new();
            Connection.ReceiveTimeout = 1000;
            Connection.SendTimeout = 1000;
            if (!Connection.ConnectAsync(xbox.EndPoint).Wait(1000))
            {
                throw new Exception($"Failed to connect to {xbox.EndPoint}");
            }
            try
            {
                Stream = Connection.GetStream();
                while (Stream.ReadByte() != '\n'); // read out the welcome message
            } catch (Exception)
            {
                throw new Exception($"Failed to intialise connection to {xbox.EndPoint}");
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

        public bool BytesAvailable()
        {
            return Connection.Available > 0;
        }

        public string ReadLine()
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

        public void CommandNoResponse(string command)
        {
            RunCommand(command);
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

        public byte[] CommandBinary(string command)
        {
            RunCommand(command);
            XboxResponse response = new(ReadLine());
            if (response.statusCode != 203)
                throw new Exception($"Binary command returned {response.statusCode}: {response.message}");
            byte[] lengthb = new byte[4];
            Stream.Read(lengthb, 0, 4);
            int length = BitConverter.ToInt32(lengthb);
            byte[] readbytes = new byte[length];
            Stream.Read(readbytes, 0, length);
            return readbytes;
        }

        public XboxResponse Command(XboxArguments command)
        {
            return Command(command.ToString());
        }

        public XboxArguments[] CommandMultiline(XboxArguments command)
        {
            return CommandMultiline(command.ToString());
        }

        public XboxArguments CommandMultilineArg(XboxArguments command)
        {
            return CommandMultilineArg(command.ToString());
        }

        public string[] CommandMultilineStrings(XboxArguments command)
        {
            return CommandMultilineStrings(command.ToString());
        }

        public byte[] CommandBinary(XboxArguments command)
        {
            return CommandBinary(command.ToString());
        }
    }
}
