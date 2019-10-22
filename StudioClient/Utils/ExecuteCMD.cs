using StudioClient.Model;
using System.Diagnostics;

namespace StudioClient.Utils
{
    class ExecuteCMD
    {
        /// <summary>
        /// 执行 CMD 命令，处理执行结果
        /// </summary>
        /// <param name="strCMD"></param>
        /// <returns></returns>
        public static CmdExecuteResultModel Handle(string strCMD)
        {
            Process cmdProcess = new Process();
            cmdProcess.StartInfo.FileName = "cmd.exe";
            cmdProcess.StartInfo.UseShellExecute = false;  // 是否使用操作系统shell启动
            cmdProcess.StartInfo.RedirectStandardInput = true;  // 接受来自调用程序的输入信息
            cmdProcess.StartInfo.RedirectStandardOutput = true;  // 由调用程序获取输出信息
            cmdProcess.StartInfo.RedirectStandardError = true;  // 重定向标准错误输出
            cmdProcess.StartInfo.CreateNoWindow = true;  // 不显示程序窗口
            cmdProcess.Start();  // 启动程序

            //向cmd窗口发送输入信息
            cmdProcess.StandardInput.WriteLine(strCMD + " &exit");

            cmdProcess.StandardInput.AutoFlush = true;

            //获取cmd窗口的输出信息
            string error = cmdProcess.StandardError.ReadToEnd();
            string output = cmdProcess.StandardOutput.ReadToEnd();
            //等待程序执行完退出进程
            cmdProcess.WaitForExit();
            cmdProcess.Close();

            if (error.Equals(""))
            {
                // 执行成功
                return new CmdExecuteResultModel()
                {
                    StateCode = 0,
                    ResultContent = output.Substring(output.IndexOf("&exit") + 7, output.Length - output.IndexOf("&exit") - 7)
                };
            }
            else
            {
                // 执行失败
                return new CmdExecuteResultModel()
                {
                    StateCode = 1,
                    ResultContent = error
                };
            }
        }
    }
}
