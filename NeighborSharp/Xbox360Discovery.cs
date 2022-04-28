using System.Net;
using System.Net.Sockets;
using System.Text;
using NeighborSharp.Types;

namespace NeighborSharp
{
    public class Xbox360Discovery
    {
        private static readonly UdpClient udp = new();

        public static DiscoveredConsole[] DiscoverAllConsoles()
        {
            byte[] type3 = { 0x03, 0x00 };
            udp.Client.EnableBroadcast = true;
            udp.Client.ReceiveTimeout = 1000;
            udp.Send(type3, new IPEndPoint(IPAddress.Broadcast, 730));
            List<DiscoveredConsole> consoles = new();
            try
            {
                while (true)
                {
                    IPEndPoint? console = null;
                    byte[] datagram = udp.Receive(ref console);
                    consoles.Add(new(console, datagram));
                }
            } catch (Exception)
            {
                // ignore :)
            }
            return consoles.ToArray();
        }
        public static DiscoveredConsole? DiscoverConsoleByName(string consolename)
        {
            List<byte> type1 = new(){ 0x01, (byte)consolename.Length };
            type1.AddRange(Encoding.ASCII.GetBytes(consolename));
            udp.Client.ReceiveTimeout = 1000;
            udp.Send(type1.ToArray(), new IPEndPoint(IPAddress.Broadcast, 730));
            try
            {
                IPEndPoint? console = null;
                byte[] datagram = udp.Receive(ref console);
                return new(console, datagram);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
