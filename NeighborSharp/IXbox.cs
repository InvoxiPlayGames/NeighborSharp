using NeighborSharp.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NeighborSharp
{
    public interface IXbox
    {
        IPEndPoint EndPoint { get; }
        string? DebugName { get; }
        string GetRunningProcess();
        XboxDrive[] GetDriveList();
        XboxFileEntry[] GetDirectoryList(string path);
        void RunTitle(string title, string directory);
        void RunTitle(string title);
        void ColdReboot();
        void TitleReboot();
        void Shutdown();
        byte[] DownloadFile(string filename);
        void UploadFile(string filename, byte[] bytes);
    }
}
