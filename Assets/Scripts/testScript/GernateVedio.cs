//1.测试python是否可正常运行
//2.指导pythonScripting插件的使用
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//pythonScripts命名空间调用
using UnityEditor.Scripting.Python;

//方法二
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;


public class GernateVedio : MonoBehaviour
{

    //private ProcessStartInfo startInfo;
    //private Process process;
    //private UdpClient udpClient;
    //private IPEndPoint remoteEP;
    // Start is called before the first frame update
    void Start()
    {
        //获取要执行的python文件的路径
        string pythonPath = Application.dataPath + "/Scripts/Aipanting/";
        //运行python文件
        PythonRunner.RunFile(pythonPath + "aipainting.py", "__main__");
        //上述源自pythonScript的方法弃用
        //原因：无法找到其它py文件，提示no module

        //方法二，弃用，看不懂的bug满天飞
        //获取python文件相对路径
        //string pythonPath = "Scripts/Aipanting/aipainting.py";
        // 获取Unity项目的数据路径，也就是Unity工程下的Assert文件夹路径
        //string dataPath = Application.dataPath;
        // 拼接Python文件的完整路径
        //string fullPath = dataPath + "/" + pythonPath;
        // 设置命令行参数，这里使用activate Python来激活
        //string command = "/c activate Python & python \"" + fullPath + "\"";

        //Kill_All_Python_Process();


        //    //创建ProcessStartInfo对象
        //    startInfo = new ProcessStartInfo();
        //    // 设定执行cmd
        //    startInfo.FileName = "cmd.exe";
        //    // 输入参数是上一步的command字符串
        //    startInfo.Arguments = command;
        //    // 因为嵌入Unity中后台使用，所以设置不显示窗口
        //    startInfo.CreateNoWindow = true;
        //    // 这里需要设定为false
        //    startInfo.UseShellExecute = false;
        //    // 设置重定向这个进程的标准输出流，用于直接被Unity C#捕获，从而实现 Python -> Unity 的通信
        //    startInfo.RedirectStandardOutput = true;
        //    // 设置重定向这个进程的标准报错流，用于在Unity的C#中进行Debug Python里的bug
        //    startInfo.RedirectStandardError = true;

        //    // 创建Process
        //    process = new Process();
        //    // 设定Process的StartInfo至刚才设定好的内容
        //    process.StartInfo = startInfo;
        //    // 设置异步输出的回调函数，用于实时输出Python中的Print和报错内容到Unity的Console
        //    process.OutputDataReceived += new DataReceivedEventHandler(OnOutputDataReceived);
        //    process.ErrorDataReceived += new DataReceivedEventHandler(OnErrorDataReceived);

        //    // 启动脚本Process，并且激活逐行读取输出与报错
        //    process.Start();
        //    // 设置异步输出流读取
        //    process.BeginErrorReadLine();
        //    process.BeginOutputReadLine();

        //    // 创建UDP通信的Client
        //    udpClient = new UdpClient();
        //    // 设置IP地址与端口号
        //    remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 19864);
        //}

        //// Update is called once per frame
        //void Update()
        //{
        //        if (Input.GetKeyDown(KeyCode.Space))
        //        {
        //            // 准备指令
        //            byte[] message = Encoding.ASCII.GetBytes("Recognizing");
        //            // 发送指令            
        //            udpClient.Send(message, message.Length, remoteEP);
        //            // 表示发送
        //            UnityEngine.Debug.Log("Sent message: " + Encoding.ASCII.GetString(message));
        //        }
        //}

        //private void OnApplicationQuit()
        //{
        //    // 在应用程序退出前执行一些代码
        //    UnityEngine.Debug.Log("应用程序即将退出，清理所有Python进程");
        //    // 结束所有Python进程
        //    Kill_All_Python_Process();
        //}
        //// 捕获标准输出
        //private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        //{
        //    //UnityEngine.Debug.Log("OutPutReceived");
        //    if (!string.IsNullOrEmpty(e.Data))
        //    {
        //        UnityEngine.Debug.Log(e.Data);

        //    }
        //}
        //// 捕获报错
        //private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        //{
        //    //UnityEngine.Debug.Log("ErrorReceived");
        //    if (!string.IsNullOrEmpty(e.Data))
        //    {
        //        // 调试语句
        //        UnityEngine.Debug.LogError("Received error output: " + e.Data);
        //    }
        //}

        ////python进程查杀
        //void Kill_All_Python_Process()
        //{
        //    Process[] allProcesses = Process.GetProcesses();
        //    foreach (Process process_1 in allProcesses)
        //    {
        //        try
        //        {
        //            // 获取进程的名称
        //            string processName = process_1.ProcessName;
        //            // 如果进程名称中包含"python"，则终止该进程，并且排除本进程本身
        //            if (processName.ToLower().Contains("python") && process_1.Id != Process.GetCurrentProcess().Id)
        //            {
        //                process_1.Kill();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // 处理异常
        //            print(ex);
        //        }
        //    }
        //}
    }

}
