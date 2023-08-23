using System;
using System.Text;
using System.Runtime.InteropServices;


namespace SharpADS
{
    class Program
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateFileW([MarshalAs(UnmanagedType.LPWStr)] string filename, uint access, int share, IntPtr securityAttributes, int creationDisposition, int flagsAndAttributes, IntPtr templateFile);
        [DllImport("kernel32.dll")] static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, int nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, [In] IntPtr lpOverlapped); [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public struct WIN32_FIND_STREAM_DATA { public long StreamSize; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH + 36)] public string StreamName; }
        [DllImport("kernel32.dll", SetLastError = true)] static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);
        [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] static extern bool DeleteFile(string lpFileName);
        [DllImport("kernel32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern IntPtr FindFirstStreamW([MarshalAs(UnmanagedType.LPTStr)] string filename, IntPtr infoLevel, out WIN32_FIND_STREAM_DATA data, int reserved = 0);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] public static extern bool FindNextStreamW(IntPtr hFind, out WIN32_FIND_STREAM_DATA data);
        [DllImport("kernel32.dll")] static extern uint GetLastError();
     
        // Source: https://learn.microsoft.com/en-us/windows/win32/fileio/file-access-rights-constants
        // Source: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-samr/262970b7-cd4a-41f4-8c4d-5a27f0092aaa
        // Source: https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        const int MAX_PATH = 260;
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const int FILE_WRITE_ATTRIBUTES = 0x100;
        const int FILE_SHARE_READ = 0x00000001;
        const int OPEN_ALWAYS = 4;
        const int FILE_FLAG_SEQUENTIAL_SCAN = 0x80;
        const int FILE_ATTRIBUTE_NORMAL = 0x08000000;
        // Maximum size to display for every ADS value
        const int MAX_ADS_SIZE_TO_DISPLAY = 256;


        static void listStreams(String filename)
        {
            Console.WriteLine("[+] Listing streams...\n");
            WIN32_FIND_STREAM_DATA stream_data;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out stream_data, 0);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                uint lasterror = GetLastError();
                if (lasterror == 38)
                {
                    Console.WriteLine("[+] No ADS values in this directory");
                    return;
                }
                else if (lasterror == 2) {
                    Console.WriteLine("[-] File or directory not found: " + filename);
                    System.Environment.Exit(-1);
                }
                else
                {
                    Console.WriteLine("[-] Invalid handle. Error code: " + GetLastError());
                    System.Environment.Exit(-1);
                }
            }

            int count = 0;
            while (true)
            {
                if (stream_data.StreamName != "::$DATA")
                {
                    count += 1;
                    String stream_name_aux = filename + stream_data.StreamName;
                    byte[] stream_content = new byte[MAX_ADS_SIZE_TO_DISPLAY];
                    //IntPtr streamHandle_aux = CreateFileW(stream_name_aux, 0x80000000, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);
                    IntPtr streamHandle_aux = CreateFileW(stream_name_aux, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_ALWAYS, FILE_FLAG_SEQUENTIAL_SCAN | FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                    if (streamHandle_aux == INVALID_HANDLE_VALUE)
                    {
                        Console.WriteLine("[-] Invalid handle. Error code: " + GetLastError());
                        System.Environment.Exit(-1);
                    }

                    ReadFile(streamHandle_aux, stream_content, (uint)stream_content.Length, out uint _, IntPtr.Zero);
                    Console.WriteLine("[+] Stream name: " + stream_name_aux);
                    Console.WriteLine("[+] ADS content: " + Encoding.UTF8.GetString(stream_content, 0, stream_content.Length));
                }
                if (!FindNextStreamW(hFind, out stream_data))
                {
                    break;
                }
            }

            if (count != 0)
            {
                Console.WriteLine("[+] Number of ADS values: " + count);
            }
            else {
                Console.WriteLine("[+] No ADS values in this file");
            }
        }


        static void clearStreams(String filename)
        {
            Console.WriteLine("[+] Deleting all streams...\n");
            WIN32_FIND_STREAM_DATA stream_data;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out stream_data);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle. Error code: " + GetLastError());
                System.Environment.Exit(-1);
            }

            while (true)
            {
                if (stream_data.StreamName != "::$DATA") {
                    String stream_name_aux = filename + stream_data.StreamName;
                    DeleteFile(stream_name_aux);
                }
                if (!FindNextStreamW(hFind, out stream_data))
                {
                    break;
                }
            }
        }


        static void writeStream(String filename, String stream_name, byte[] payload_bytes)
        {
            Console.WriteLine("[+] Creating or updating stream "+ stream_name + " ...");
            String target_stream = $"{filename}:{stream_name}";
            DeleteFile(target_stream);
            IntPtr streamHandle = CreateFileW(target_stream, GENERIC_READ | GENERIC_WRITE | FILE_WRITE_ATTRIBUTES, FILE_SHARE_READ, IntPtr.Zero, OPEN_ALWAYS, FILE_FLAG_SEQUENTIAL_SCAN | FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

            if (streamHandle == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle. Error code: " + GetLastError());
                System.Environment.Exit(-1);
            };

            WriteFile(streamHandle, payload_bytes, payload_bytes.Length, out uint aux, IntPtr.Zero);
            CloseHandle(streamHandle);
        }


        static void deleteStream(String filename, String stream_name)
        {
            Console.WriteLine("[+] Deleting stream "+stream_name+" ...");
            String target_stream = $"{filename}:{stream_name}";
            DeleteFile(target_stream);
        }


        static void readStream(String filename, String stream_name)
        {
            Console.WriteLine("[+] Reading stream "+ stream_name + " ...");
            WIN32_FIND_STREAM_DATA stream_data;
            IntPtr hFind = FindFirstStreamW(filename, IntPtr.Zero, out stream_data);

            if (hFind == INVALID_HANDLE_VALUE)
            {
                Console.WriteLine("[-] Invalid handle. Error code: " + GetLastError());
                System.Environment.Exit(-1);
            }

            while (true)
            {
                if (stream_data.StreamName == (":"+stream_name+":$DATA"))
                {
                    String stream_name_aux = filename + stream_data.StreamName;
                    byte[] stream_content = new byte[MAX_ADS_SIZE_TO_DISPLAY];
                    // IntPtr streamHandle_aux = CreateFileW(stream_name_aux, 0x80000000, 0x00000001, IntPtr.Zero, 4, 0x80 | 0x08000000, IntPtr.Zero);
                    IntPtr streamHandle_aux = CreateFileW(stream_name_aux, GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_ALWAYS, FILE_FLAG_SEQUENTIAL_SCAN | FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                    if (streamHandle_aux == INVALID_HANDLE_VALUE)
                    {
                        Console.WriteLine("[-] Invalid handle. Error code: " + GetLastError());
                        System.Environment.Exit(-1);
                    }

                    ReadFile(streamHandle_aux, stream_content, (uint)stream_content.Length, out uint _, IntPtr.Zero);
                    Console.WriteLine("[+] Stream name: " + stream_name_aux);
                    Console.WriteLine("[+] ADS content: " + Encoding.UTF8.GetString(stream_content, 0, stream_content.Length));
                }
                if (!FindNextStreamW(hFind, out stream_data))
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

            // Hexadecimal payload
            if (payload_str.Length >= 2)
            {
                if (payload_str.Substring(0, 2) == "0x")
                {
                    try
                    {
                        payload_str = payload_str.Replace("0x", "");
                        buf = ToByteArray(payload_str);
                        return buf;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        System.Environment.Exit(-1);
                    }
                }
            }

            // Payload from url, http or https
            if (payload_str.Length >= 4)
            {
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

            // Regular payload
            buf = Encoding.ASCII.GetBytes(payload_str);
            return buf;
        }


        static void getHelp()
        {
            Console.WriteLine("[+] SharpADS.exe [option] (args) ");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe write FILE_PATH STREAM_NAME PAYLOAD");
            Console.WriteLine("[+] Example (string):      SharpADS.exe write c:\\Temp\\test.txt ADS_name1 RandomString");
            Console.WriteLine("[+] Example (hexadecimal): SharpADS.exe write c:\\Temp\\test.txt ADS_name2 0x4142434445");
            Console.WriteLine("[+] Example (from url):    SharpADS.exe write c:\\Temp\\test.txt ADS_name3 http:///127.0.0.1/payload.bin");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe read FILE_PATH STREAM_NAME");
            Console.WriteLine("[+] Example: SharpADS.exe read c:\\Temp\\test.txt ADS_name1");
            Console.WriteLine("");

            Console.WriteLine("[+] SharpADS.exe delete FILE_PATH STREAM_NAME");
            Console.WriteLine("[+] Example: SharpADS.exe delete c:\\Temp\\test.txt ADS_name1");
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
            if (args.Length < 2)
            {
                getHelp();
            }

            String option = args[0];
            String filename = args[1];
            filename = filename.Replace("'", "");
            filename = filename.Replace("\"", "");
            if (filename.EndsWith("\\"))
            {
            
                filename = filename.Remove(filename.Length - 1, 1);
            }
            
            if (option == "write")
            {
                if (args.Length < 4)
                {
                    getHelp();
                }
                String stream_name = args[2];
                String payload = args[3];
                byte[] payload_content = getPayload(payload);
                writeStream(filename, stream_name, payload_content);
                listStreams(filename);
            }

            else if (option == "delete")
            {
                if (args.Length < 3)
                {
                    getHelp();
                }
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
                String stream_name = args[2];
                readStream(filename, stream_name);
            }

            else if (option == "list")
            {
                listStreams(filename);
            }
            
            else if (option == "clear")
            {
                clearStreams(filename);
                listStreams(filename);
            }

        }

    }

}