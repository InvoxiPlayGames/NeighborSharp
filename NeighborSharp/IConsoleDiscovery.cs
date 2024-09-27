using NeighborSharp.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NeighborSharp
{
    public class IConsoleDiscovery
    {
        private static readonly UdpClient udp = new();
        internal int port = 730;

        public DiscoveredConsole[] DiscoverAllConsoles()
        {
            byte[] type3 = { 0x03, 0x00 };
            udp.Client.EnableBroadcast = true;
            udp.Client.ReceiveTimeout = 1000;
            udp.Send(type3, new IPEndPoint(IPAddress.Broadcast, port));
            List<DiscoveredConsole> consoles = new();
            try
            {
                while (true)
                {
                    IPEndPoint? console = null;
                    byte[] datagram = udp.Receive(ref console);
                    consoles.Add(new(console, datagram));
                }
            }
            catch (Exception)
            {
                // ignore :)
            }
            return consoles.ToArray();
        }

        public DiscoveredConsole? DiscoverConsoleByName(string consolename)
        {
            List<byte> type1 = [0x01, (byte)consolename.Length, .. Encoding.ASCII.GetBytes(consolename)];
            udp.Client.ReceiveTimeout = 1000;
            udp.Send(type1.ToArray(), new IPEndPoint(IPAddress.Broadcast, port));
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

        public bool DiscoverConsole(IPEndPoint endPoint)
        {
            byte[] type3 = { 0x03, 0x00 };
            udp.Client.ReceiveTimeout = 250;
            udp.Send(type3, endPoint);
            try
            {
                while (true)
                {
                    IPEndPoint? console = null;
                    byte[] datagram = udp.Receive(ref console);
                    if (console == endPoint)
                        return true;
                }
            }
            catch (Exception)
            {
                // ignore :)
            }
            return false;
        }
    }
}
