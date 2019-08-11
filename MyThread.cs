using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClashNet
{
    class MyThread
    {
        private Process process;
        private NotifyIcon notifyIcon;

        public MyThread(Process process, NotifyIcon notifyIcon)
        {
            this.process = process;
            this.notifyIcon = notifyIcon;
        }

        public void ThreadMain()
        {
            // 这里需要起一个新的线程来做这件事，不然程序就死在这里了
            while (!process.HasExited && !process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                // do something with line
                Console.WriteLine(line);
            }
            if(process.ExitCode!=0)
            {
                notifyIcon.BalloonTipText = "Clash 启动失败，请查看错误日志";
                notifyIcon.ShowBalloonTip(2000);
            }
        }
    }
}
