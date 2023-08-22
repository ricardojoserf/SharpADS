using System;
using System.IO;
using System.Net;
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
        const int ADS_MAX_SIZE = 0x40000 - 1;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateFileW([MarshalAs(UnmanagedType.LPWStr)] string filename, uint access, int share, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr templateFile);
        [DllImport("kernel32.dll")] static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] IntPtr lpOverlapped); [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct WIN32_FIND_STREAM_DATA { public long StreamSize; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH + 36)] public string StreamName; }
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] static extern bool DeleteFile(string lpFileName);
        [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern IntPtr FindFirstStreamW([MarshalAs(UnmanagedType.LPTStr)] string filename, IntPtr infoLevel, out WIN32_FIND_STREAM_DATA data, int reserved = 0);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern bool FindNextStreamW(IntPtr hFind, out WIN32_FIND_STREAM_DATA data);
        
        
        static void listStreams(String filename) {
            Console.WriteLine("[+] Listing streams \n");
            WIN32_FIND_STREAM_DATA data2;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out data2);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle (hFind)");
                System.Environment.Exit(-1);
            }

            while (true)
            {
                if (data2.StreamName != "::$DATA")
                {
                    String stream_name_aux = filename + data2.StreamName;
                    byte[] data3 = new byte[256];
                    IntPtr streamHandle_aux = CreateFileW(stream_name_aux, 0x80000000, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);

                    if (streamHandle_aux == INVALID_HANDLE_VALUE)
                    {
                        Console.WriteLine("[-] Invalid handle (streamHandle_aux)");
                        System.Environment.Exit(-1);
                    }

                    ReadFile(streamHandle_aux, data3, (uint)data3.Length, out uint data4, IntPtr.Zero);
                    Console.WriteLine("[+] Stream name: " + stream_name_aux);
                    Console.WriteLine("[+] ADS content: " + Encoding.UTF8.GetString(data3, 0, data3.Length));
                }
                if (!FindNextStreamW(hFind, out data2))
                {
                    break;
                }
            }
        }


        static void clearStreams(String filename)
        {
            Console.WriteLine("[+] Deleting all streams");
            WIN32_FIND_STREAM_DATA data2;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out data2);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle (hFind)");
                System.Environment.Exit(-1);
            }

            while (true)
            {
                if (data2.StreamName != "::$DATA") {
                    String stream_name_aux = filename + data2.StreamName;
                    DeleteFile(stream_name_aux);
                }
                if (!FindNextStreamW(hFind, out data2))
                {
                    break;
                }
            }
        }


        static void writeStream(String filename, String stream_name, byte[] data) {
            Console.WriteLine("[+] Creating or updating stream "+ stream_name + " \n");
            String target_stream = $"{filename}:{stream_name}";
            DeleteFile(target_stream);
            IntPtr streamHandle = CreateFileW(target_stream, 0x80000000 | 0x40000000 | 0x100, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);

            if (streamHandle == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle (streamHandle)");
                System.Environment.Exit(-1);
            };

            WriteFile(streamHandle, data, data.Length, out uint aux, IntPtr.Zero);
            CloseHandle(streamHandle);
        }


        static void deleteStream(String filename, String stream_name)
        {
            Console.WriteLine("[+] Deleting stream "+stream_name+" \n");
            String target_stream = $"{filename}:{stream_name}";
            DeleteFile(target_stream);
        }


        static void readStream(String filename, String stream_name)
        {
            Console.WriteLine("[+] Reading stream "+ stream_name + " \n");
            WIN32_FIND_STREAM_DATA data2;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out data2);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle (hFind)");
                System.Environment.Exit(-1);
            }

            while (true)
            {
                if (data2.StreamName == (":"+stream_name+":$DATA"))
                {
                    String stream_name_aux = filename + data2.StreamName;
                    byte[] data3 = new byte[256];
                    IntPtr streamHandle_aux = CreateFileW(stream_name_aux, 0x80000000, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);

                    if (streamHandle_aux == INVALID_HANDLE_VALUE)
                    {
                        Console.WriteLine("[-] Invalid handle (streamHandle_aux)");
                        System.Environment.Exit(-1);
                    }

                    ReadFile(streamHandle_aux, data3, (uint)data3.Length, out uint data4, IntPtr.Zero);
                    Console.WriteLine("[+] Stream name: " + stream_name_aux);
                    Console.WriteLine("[+] ADS content: " + Encoding.UTF8.GetString(data3, 0, data3.Length));
                }
                if (!FindNextStreamW(hFind, out data2))
                {
                    break;
                }
            }
        }


        public static byte[] ToByteArray(String hexString)
        {
            byte[] retval = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
                retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            return retval;
        }


        static byte[] getPayload(String payload_str)
        {
            byte[] buf = { };

            // Payload from url, http or https
            if (payload_str.Length >= 2)
            {
                if (payload_str.Substring(0, 2) == "0x")
                {
                    try {
                        payload_str = payload_str.Replace("0x", "");
                        buf = ToByteArray(payload_str);
                        return buf;
                    }
                    catch (Exception ex){
                        Console.WriteLine(ex.ToString());
                        System.Environment.Exit(-1);
                    }
                }
            }

            // Payload from url, http or https
            if (payload_str.Length >= 4) {
                if (payload_str.Substring(0, 4) == "http")
                {
                    Console.WriteLine("[+] Getting payload from url: " + payload_str);
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                    using (System.Net.WebClient myWebClient = new System.Net.WebClient())
                    {
                        try
                        {
                            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                            buf = myWebClient.DownloadData(payload_str);
                            return buf;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            System.Environment.Exit(-1);
                        }
                    }

                }    
            }

            buf = Encoding.ASCII.GetBytes(payload_str);
            return buf;
        }


        static void getHelp()
        {
            Console.WriteLine("[+] SharpADS.exe [option] (args) ");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe write FILE_PATH STREAM_NAME PAYLOAD");
            Console.WriteLine("[+] Example: SharpADS.exe write c:\\Temp\\test.txt ads_name 'This is a text'");
            Console.WriteLine("[+] Example: SharpADS.exe write c:\\Temp\\test.txt ads_name http:///127.0.0.1/payload.bin");
            Console.WriteLine("[+] Example: SharpADS.exe write c:\\Temp\\test.txt ads_name 4142434445");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe read FILE_PATH STREAM_NAME");
            Console.WriteLine("[+] Example: SharpADS.exe read c:\\Temp\\test.txt ads_name");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe delete FILE_PATH STREAM_NAME");
            Console.WriteLine("[+] Example: SharpADS.exe delete c:\\Temp\\test.txt ads_name");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe list FILE_PATH");
            Console.WriteLine("[+] Example: SharpADS.exe list c:\\Temp\\test.txt");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe clear FILE_PATH");
            Console.WriteLine("[+] Example: SharpADS.exe clear c:\\Temp\\test.txt");

            System.Environment.Exit(0);
        }


        static void Main(string[] args)
        {
            if (args.Length < 2) {
                getHelp();
            }
            String option = args[0];

            if (option == "write")
            {
                if (args.Length < 4)
                {
                    getHelp();
                }
                String filename = args[1];
                String stream_name = args[2];
                String payload = args[3];
                byte[] data = getPayload(payload);
                writeStream(filename, stream_name, data);
                listStreams(filename);
            }

            else if (option == "delete")
            {
                if (args.Length < 3)
                {
                    getHelp();
                }
                String filename = args[1];
                String stream_name = args[2];
                deleteStream(filename, stream_name);
                listStreams(filename);
            }
            
            else if (option == "read")
            {
                if (args.Length < 3)
                {
                    getHelp();
                }
                String filename = args[1];
                String stream_name = args[2];
                readStream(filename, stream_name);
            }

            else if (option == "list") {
                String filename = args[1];
                listStreams(filename);
            }
            
            else if (option == "clear")
            {
                String filename = args[1];
                clearStreams(filename);
            }

        }

    }

}