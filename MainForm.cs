using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace ClashNet
{
    public partial class MainForm : Form
    {
        //这里在窗体上没有拖拽一个NotifyIcon控件，而是在这里定义了一个变量  
        private NotifyIcon notifyIcon = null;
        private Process proc = null;

        public ChromiumWebBrowser browser;
        public void InitBrowser()
        {
            Cef.Initialize(new CefSettings());

            browser = new ChromiumWebBrowser(System.IO.Directory.GetCurrentDirectory() + @"\dashboard\index.html");
            this.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
        }

        public MainForm()
        {
            InitializeComponent();
            InitBrowser();
            //调用初始化托盘显示函数  
            InitialTray();
            this.InitClash();
            this.SetProxy();
        }

        private void SetProxy()
        {
            string args = @"global 127.0.0.1:7890 localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>";
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @".\sysproxy.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
        }

        private void UnSetProxy()
        {
            string args = @"set 1 - - -";
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @".\sysproxy.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
        }


        private void MainFormLoad(object sender, EventArgs e)
        {
            //这里写其他代码  
            this.BeginInvoke(new Action(() => {
                this.Hide();
                this.Opacity = 1;
            }));
        }

        /// <summary>  
        /// 窗体关闭的单击事件  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            //通过这里可以看出，这里的关闭其实不是真正意义上的“关闭”，而是将窗体隐藏，实现一个“伪关闭”  
            this.Hide();
        }

        private void InitClash()
        {
            //Process[] ps = Process.GetProcessesByName("clash.exe");
            //if (ps.Length > 0)
            //{
            //    proc = ps[0];
            //    return;
            //}
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\.config\clash";
            proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @".\clash.exe",
                    Arguments = @"-d "+ dir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            MyThread myThread = new MyThread(proc, notifyIcon);

            Thread thread = new Thread(myThread.ThreadMain);
            thread.Start();

        }

        private void InitialTray()
        {
            //隐藏主窗体  
            this.Hide();

            //实例化一个NotifyIcon对象  
            notifyIcon = new NotifyIcon();
            //托盘图标气泡显示的内容  
            notifyIcon.BalloonTipText = "ClashNet 正在后台运行";
            notifyIcon.BalloonTipTitle = "ClashNet";
            //托盘图标显示的内容  
            notifyIcon.Text = "ClashNet";

            System.Reflection.Assembly assembly = GetType().Assembly;
            using (System.IO.Stream streamSmall = assembly.GetManifestResourceStream("ClashNet.logo.ico"))
            {
                //注意：下面的路径可以是绝对路径、相对路径。但是需要注意的是：文件必须是一个.ico格式  
                notifyIcon.Icon = new System.Drawing.Icon(streamSmall);
            }
            //true表示在托盘区可见，false表示在托盘区不可见  
            notifyIcon.Visible = true;
            //气泡显示的时间（单位是毫秒）  
            notifyIcon.ShowBalloonTip(2000);
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);

            ////设置二级菜单  
            //MenuItem setting1 = new MenuItem("二级菜单1");  
            //MenuItem setting2 = new MenuItem("二级菜单2");  
            //MenuItem setting = new MenuItem("一级菜单", new MenuItem[]{setting1,setting2});  

            //关于选项  
            MenuItem about = new MenuItem(@"关于和帮助");
            about.Click += new EventHandler(about_Click);
            //退出菜单项  
            MenuItem exit = new MenuItem("退出");
            exit.Click += new EventHandler(exit_Click);

            ////关联托盘控件  
            //注释的这一行与下一行的区别就是参数不同，setting这个参数是为了实现二级菜单  
            //MenuItem[] childen = new MenuItem[] { setting, help, about, exit };  
            MenuItem[] childen = new MenuItem[] { about, exit };
            notifyIcon.ContextMenu = new ContextMenu(childen);

            //窗体关闭时触发  
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }

        /// <summary>  
                /// 鼠标单击  
                /// </summary>  
                /// <param name="sender"></param>  
                /// <param name="e"></param>  
        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            //鼠标左键单击  
            if (e.Button == MouseButtons.Left)
            {
                //如果窗体是可见的，那么鼠标左击托盘区图标后，窗体为不可见  
                if (this.Visible == true)
                {
                    this.Visible = false;
                }
                else
                {
                    this.Visible = true;
                    this.Activate();
                }
            }
        }

        /// <summary>  
        /// 退出选项  
        /// </summary>  
        /// <param name="sender"></param>  
        /// <param name="e"></param>  
        private void exit_Click(object sender, EventArgs e)
        {
            this.UnSetProxy();
            //退出程序  
            notifyIcon.Dispose();
           
            if (proc != null && !proc.HasExited)
            {
                proc.Kill();
                proc.WaitForExit();//关键，等待外部程序退出后才能往下执行
            }
            System.Environment.Exit(0);
        }

        private void about_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/coderbean/ClashNet");
        }
    }
} 