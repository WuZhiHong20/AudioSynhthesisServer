using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class VITS
    {
        private readonly Process vitsProcess;

        public delegate void onOutputHandler(VITS sender, string e);
        public event onOutputHandler OutputHandler;
        public VITS()
        {
            try
            {
                vitsProcess = new Process()
                {
                    StartInfo = new ProcessStartInfo("cmd.exe")
                    {
                        //"/k"，执行完后不会立即退出，而是保持在运行状态
                        Arguments = "/k",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                vitsProcess.OutputDataReceived += Command_OutputDataReceived;
                vitsProcess.ErrorDataReceived += Command_ErrorDataReceived;
                vitsProcess.Start();
                vitsProcess.BeginOutputReadLine();
                vitsProcess.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }
        void Command_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnOutput(e.Data);
        }

        void Command_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            OnOutput(e.Data);
        }

        private void OnOutput(string data)
        {
            OutputHandler?.Invoke(this, data);
        }

        /// <summary>
        /// 往VITS终端写命令
        /// </summary>
        /// <param name="data"></param>
        public void Write(string data)
        {
            try
            {
                vitsProcess.StandardInput.WriteLine(data);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }
    }
}
