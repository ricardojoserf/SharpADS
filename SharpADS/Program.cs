using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SharpADS
{
    class Program
    {
        // access: https://learn.microsoft.com/en-us/windows/win32/fileio/file-access-rights-constants
        // access: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-samr/262970b7-cd4a-41f4-8c4d-5a27f0092aaa
        // share, creationDisposition, flagsAndAttributes: https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        const int MAX_PATH = 260;
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateFileW([MarshalAs(UnmanagedType.LPWStr)] string filename, uint access, int share, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr templateFile);
        [DllImport("kernel32.dll")] static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] IntPtr lpOverlapped); [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct WIN32_FIND_STREAM_DATA { public long StreamSize; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH + 36)] public string StreamName; }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern IntPtr FindFirstStreamW([MarshalAs(UnmanagedType.LPTStr)] string filename, IntPtr infoLevel, out WIN32_FIND_STREAM_DATA data, int reserved = 0);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern bool FindNextStreamW(IntPtr hFind, out WIN32_FIND_STREAM_DATA data);
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);


        static void readStreams(String filename) {
            WIN32_FIND_STREAM_DATA data2;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out data2);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle (hFind)");
                System.Environment.Exit(-1);
            }

            while (true)
            {
                String stream_name_aux = filename + data2.StreamName;
                Console.WriteLine("[+] Stream name: " + stream_name_aux);
                Console.WriteLine("[+] Stream size: " + data2.StreamSize);

                byte[] data3 = new byte[256];
                IntPtr streamHandle_aux = CreateFileW(stream_name_aux, 0x80000000, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);
                ReadFile(streamHandle_aux, data3, (uint)data3.Length, out uint data4, IntPtr.Zero);
                Console.WriteLine("[+] ADS content: " + Encoding.UTF8.GetString(data3, 0, data3.Length));

                if (!FindNextStreamW(hFind, out data2))
                {
                    break;
                }
            }
        }


        static void writeStream(String filename, String stream_name) {
            String target_stream = $"{filename}:{stream_name}";
            IntPtr streamHandle = CreateFileW(target_stream, 0x80000000 | 0x40000000 | 0x100, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);

            if (streamHandle == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle (streamHandle)");
                System.Environment.Exit(-1);
            }

            byte[] data = { 0x41, 0x42, 0x43, 0x44, 0x45 };
            WriteFile(streamHandle, data, data.Length, out uint aux, IntPtr.Zero);
        }


        static void Main(string[] args)
        {
            String filename = args[0];
            String stream_name = args[1];

            writeStream(filename, stream_name);
            readStreams(filename);
        }
    }
}