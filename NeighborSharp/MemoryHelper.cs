using System.Runtime.InteropServices;

namespace NeighborSharp
{
    public class MemoryHelper
    {
        public static void PrintBytes(byte[] data, uint baseAddr = 0)
        {
            var oldColour = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Address  ");

            // Print top row.
            for (int i = 0; i < 16; i++)
                Console.Write($"{(i + baseAddr % 16):X2} ");

            Console.WriteLine("");
            Console.ForegroundColor = oldColour;

            for (int i = 0; i < data.Length; i += 16)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{(baseAddr + i):X8} ");
                Console.ForegroundColor = oldColour;

                for (int j = 0; j < 16; j++)
                {
                    int index = i + j;

                    if (index < data.Length)
                    {
                        Console.Write($"{data[index]:X2} ");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("?? ");
                        Console.ForegroundColor = oldColour;
                    }
                }

                Console.WriteLine();
            }
        }

        public static T ByteArrayToStructure<T>(byte[] data) where T : struct
        {
            if (data == null || data.Length <= 0)
                return default;

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructureToByteArray<T>(T structure) where T : struct
        {
            byte[] data = new byte[Marshal.SizeOf(typeof(T))];

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }

            return data;
        }

        public static byte[] HexStringToByteArray(string hexStr)
        {
            hexStr = hexStr.Replace("0x", "")
                           .Replace(" ", "")
                           .Replace("?", "");

            return Enumerable.Range(0, hexStr.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hexStr.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
