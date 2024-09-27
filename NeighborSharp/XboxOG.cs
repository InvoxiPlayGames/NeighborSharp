using System.Net;
using NeighborSharp.Types;

namespace NeighborSharp
{
    public class XboxOG : Xbox360
    {
        private void FetchConsoleInfo()
        {
            XBDMConnection conn = new(this);
            DebugName = conn.Command("dbgname").message;
        }

        public XboxOG(string ip) : base(new IPEndPoint(IPAddress.Parse(ip), 731))
        {
            FetchConsoleInfo();
        }

        public XboxOG(IPAddress ip) : base(new IPEndPoint(ip, 731))
        {
            FetchConsoleInfo();
        }

        public override string ToString()
        {
            return $"{DebugName} on {EndPoint.Address}";
        }

        public new XboxDrive[] GetDriveList()
        {
            XBDMConnection conn = new(this);
            List<XboxDrive> drives = new();
            XboxResponse resp = conn.Command("drivelist");
            foreach (char drive in resp.message)
            {
                XboxArguments commargs = new();
                commargs.stringValues["name"] = $"{drive}:\\";
                try
                {
                    XboxArguments driveargs = conn.CommandMultilineArg("drivefreespace " + commargs.ToString());
                    drives.Add(new(drive.ToString(), driveargs));
                }
                catch (Exception)
                {
                    drives.Add(new(drive.ToString()));
                }
            }
            return drives.ToArray();
        }
    }
}