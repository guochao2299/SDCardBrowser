using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace SDCardBrowser
{
    public partial class frmMain : Form
    {
        private SerialPort m_port = null;
        private UserSettings m_settings = null;

        public frmMain()
        {
            InitializeComponent();
        }

        private void toolSettings_Click(object sender, EventArgs e)
        {
            frmSettings fs = new frmSettings(m_settings);
            if (fs.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                m_settings.IsAutoSaveWhenAppClosed = fs.IsSendCmdWhenReceiveKeyCode;
                m_settings.SerialPort = fs.PortName;
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            try
            {
                m_settings = UserSettings.LoadFromFile();
                m_settings.UserSettingsChanged += delegate(object obj, EventArgs ea)
                {
                    this.lblStatus.Text = m_settings.ToString();
                };
                this.lblStatus.Text = m_settings.ToString();                
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载配置文件失败，错误消息为:" + ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
        private const string LIST_CMD = @"^LIST:\S+:[01]{1}$";
        private const char SPLIT_CHAR = ':';

        private delegate void UpdateSerialPortDataHandler(string cnt);
        private void UpdateSerialPortData(string cnt)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new UpdateSerialPortDataHandler(UpdateSerialPortData), cnt);
            }
            else
            {
                Regex r = new Regex(LIST_CMD);
                if (r.IsMatch(cnt))
                {
                    string[] subStrs = cnt.Split(SPLIT_CHAR);
                    ucFile b = new ucFile();
                    b.Text = subStrs[1];
                    if (subStrs[2] == "0")
                    {
                        b.IsDir = false;
                        b.Image = (Image)SDCardBrowser.Properties.Resources.ResourceManager.GetObject("file");
                    }
                    else
                    {
                        b.IsDir = true;
                        b.DirOpening += delegate(object objSender, EventArgs ea)
                        {                            
                            this.lblPath.Text += ((this.lblPath.Text.Last()=='/')?string.Empty:"/") + subStrs[1];
                            ListDirContents(this.lblPath.Text);
                        };
                        b.Image = (Image)SDCardBrowser.Properties.Resources.ResourceManager.GetObject("dir");
                    }

                    this.flpPanel.Controls.Add(b);
                }
            }            
        }

        private void ListDirContents(string dirPath)
        {
            this.flpPanel.Controls.Clear();

            if (!string.Equals(dirPath, "/"))
            {
                ucFile b = new ucFile();
                b.Text = "返回上一级";
                b.IsDir = true;
                b.DirOpening += delegate(object objSender, EventArgs ea)
                {
                    int lastIndex = dirPath.LastIndexOf('/');
                    this.lblPath.Text = dirPath.Substring(0, lastIndex);
                    if (string.IsNullOrEmpty(this.lblPath.Text))
                    {
                        this.lblPath.Text = "/";
                    }
                    ListDirContents(this.lblPath.Text);
                };
                b.Image = (Image)SDCardBrowser.Properties.Resources.ResourceManager.GetObject("dir");

                this.flpPanel.Controls.Add(b);
            }

            m_port.Write(string.Format("OPEN:{0};",dirPath));
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;            
            string indata = sp.ReadLine().TrimEnd('\r');
            Console.WriteLine(indata);
            UpdateSerialPortData(indata);
        }

        private void toolSerialControl_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            try
            {
                if (m_port != null && m_port.IsOpen)
                {
                    m_port.Close();
                    m_port.Dispose();
                    m_port = null;

                    this.toolSerialControl.Text = "开启串口监控";
                    this.toolSerialControl.Checked = false;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(m_settings.SerialPort))
                    {
                        MessageBox.Show("当前串口为空，请在设置窗口中选中可用端口");
                        return;
                    }

                    string[] ports = SerialPort.GetPortNames();

                    if (ports == null || ports.Length <= 0)
                    {
                        MessageBox.Show("本机没有可用串口!");
                        return;
                    }

                    if (!ports.Any(r => string.Compare(r, m_settings.SerialPort, true) == 0))
                    {
                        MessageBox.Show(string.Format("端口{0}不存在，请在设置窗口中选中可用端口"));
                        return;
                    }

                    flpPanel.Controls.Clear();

                    m_port = new SerialPort(m_settings.SerialPort, 9600);
                    m_port.Parity = Parity.None;
                    m_port.StopBits = StopBits.One;
                    m_port.DataBits = 8;
                    m_port.Handshake = Handshake.None;
                    m_port.RtsEnable = true;

                    m_port.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);

                    m_port.Open();

                    this.lblPath.Text = "/";

                    this.toolSerialControl.Text = "关闭串口监控";
                    this.toolSerialControl.Checked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("串口操作失败，错误消息为：" + ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;

            try
            {
                if (m_port != null && m_port.IsOpen)
                {
                    m_port.Close();
                    m_port.Dispose();
                    m_port = null;
                }

                if (m_settings.IsAutoSaveWhenAppClosed)
                {
                    m_settings.Serialize2File();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}
