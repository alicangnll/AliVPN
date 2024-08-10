using Microsoft.Win32;
using Renci.SshNet;
using MaterialSkin.Controls;

namespace DSSHVPN
{
    public partial class Form1 : MaterialForm
    {
        public Form1()
        {
            InitializeComponent();
        }
        // SSH Baðlantýsý için gerekli iþlemler yükleniyor
        static uint forwardport = 9000;
        SshClient sshClient = new SshClient("0.0.0.0", 22, "0000", "0000");
        ForwardedPortDynamic portForwarded = new ForwardedPortDynamic(forwardport);
        // SSH Baðlantýsý için gerekli protokoller çaðýrýlýyor
        private class WinINetInterop
        {
            public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
            public const int INTERNET_OPTION_REFRESH = 37;

            [System.Runtime.InteropServices.DllImport("wininet.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int lpdwBufferLength);
        }
        // Yazýlým yerel aðda Proxy açacaðý için Windows'u Proxy ayarlarýna göre konfigüre ediyoruz.
        private void set_windows_proxy()
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            registry.SetValue("ProxyEnable", 1);
            registry.SetValue("ProxyServer", "socks5://127.0.0.1:" + forwardport);
            WinINetInterop.InternetSetOption(IntPtr.Zero, WinINetInterop.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            WinINetInterop.InternetSetOption(IntPtr.Zero, WinINetInterop.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
        // SSH baðlantýsý kuruluyor
        private void Connect_SSHVPN(string ip, int port, string username, string password)
        {
            SshClient sshClient = new SshClient(ip, port, username, password);
            sshClient.Connect();
            sshClient.AddForwardedPort(portForwarded);
            portForwarded.Start();
            set_windows_proxy();
        }
        // Proxy baðlantýsý siliniyor
        private void unset_windows_proxy()
        {
            RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            registry.SetValue("ProxyEnable", 0);
            registry.SetValue("ProxyServer", "");
        }
        // VPN baðlantýsý kesiliyor
        private void Disconnect_VPN()
        {
            portForwarded.Stop();
            sshClient.Disconnect();
            sshClient.RemoveForwardedPort(portForwarded);
            unset_windows_proxy();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string ip = textBox1.Text;
            int port = int.Parse(textBox2.Text);
            string username = textBox3.Text;
            string password = textBox4.Text;
            if(ip == "" || port == null || username == "" || password == "")
            {
                MessageBox.Show("Empty textbox detected!");
            } else
            {
                if (button1.Text == "Connect to VPN")
                {
                    if (port >= 65535 || port <= 0)
                    {
                        MessageBox.Show("Port number must between of 0 and 65535");
                    }
                    else
                    {
                        try
                        {
                            Connect_SSHVPN(ip, port, username, password);
                            button1.Text = "Disconnect to VPN";
                            label6.Text = "socks5://127.0.0.1:" + forwardport;
                            button1.BackColor = Color.Red;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occured.\nERROR : " + ex.Message);
                        }
                    }
                }
                else
                {
                    try
                    {
                        Disconnect_VPN();
                        button1.Text = "Connect to VPN";
                        button1.BackColor = Color.GreenYellow;
                        label6.Text = "NO";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("An error occured.\nERROR : " + ex.Message);
                    }
                }
            }  
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                try
                {
                    Disconnect_VPN();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured.\nERROR : " + ex.Message);
                }
            }
            if (e.CloseReason == CloseReason.WindowsShutDown)
            {
                try
                {
                    Disconnect_VPN();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured.\nERROR : " + ex.Message);
                }
            }
        }
    }
}
