﻿using System.Net;
using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class ClientState
    {
        public Socket? socket;
        public byte[] readBufff = new byte[1024];
    }

    internal class Program
    {
        static Socket listenfd;
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>();

        static string dicPath;
        static string modelPath = "\\AudioConfig\\keqing.pth";
        static string configPath = "\\AudioConfig\\config.json";
        static string VITSPath = "\\MoeGoe\\MoeGoe.exe";
        static string prefix = "";
        static string testPath = "\\AudioConfig\\config.txt";
        static string savePath = "\\Audios\\";
        static bool talken = false;

        static void LoadConfig()
        {
            DirectoryInfo path = new DirectoryInfo(Directory.GetCurrentDirectory());
            //读取txt配置文件
#if DEBUG
            prefix = path.Parent.Parent.Parent.Parent.Parent.FullName;
#else
            prefix = path.Parent.FullName;
#endif
            Console.WriteLine(prefix + modelPath);
            var p = prefix + testPath;
            string[] files = File.ReadAllLines(p, Encoding.UTF8);
            foreach (string file in files)
            {
                Console.WriteLine(file);
            }
        }

        static VITS vitsCmd;

        private static void InitVITS()
        {
            vitsCmd = new VITS();
            vitsCmd.OutputHandler += VITSOUT;
            vitsCmd.Write($"\"{prefix + VITSPath}\" --escape");
            vitsCmd.Write(prefix + modelPath);
            vitsCmd.Write(prefix + configPath);
            var dateTime = System.DateTime.Now.ToString().Substring(0, 10);
            dateTime = dateTime.Replace("/", "_");
            dateTime = dateTime.Replace(" ", "");
            dicPath = prefix + savePath + dateTime;
            if (!Directory.Exists(dicPath))
            {
                Directory.CreateDirectory(dicPath);
            }
        }

        static void SynhthesisAudio(string inputMode, string message, string SpeakerID, string Path)
        {
            if (message.Length <= 1) return;
            if (talken == false) { talken = true; }
            else { vitsCmd.Write("y"); }
            vitsCmd.Write(inputMode);
            vitsCmd.Write("[ZH]" + message + "[ZH]");
            vitsCmd.Write(SpeakerID);
            //Console.WriteLine(inputMode);
            //Console.WriteLine("[ZH]" + message + "[ZH]");
            //Console.WriteLine(SpeakerID);
            string fileName;
            if(message.Length >= 10) { fileName = message.Substring(0, 10); }
            else  { fileName = message; }

            Path = Path + "\\" + fileName + ".wav";
            vitsCmd.Write(Path);
            //Console.WriteLine(Path);
        }

        static void StartExit()
        {
            if (talken == false)
            {
                vitsCmd.Write("t");
                vitsCmd.Write("[ZH]再见喽[ZH]");
                vitsCmd.Write("87");
                vitsCmd.Write(prefix + savePath + "再见喽.wav");
                vitsCmd.Write("n");
            }
            else
            {
                vitsCmd.Write("n");
                Console.WriteLine("从这儿走...");
            }
        }

        static void HandleInput(string input)
        {
            string[] commands = input.Split();
            if (commands[0].Contains("Exit"))
            {
                StartExit();
                Thread.Sleep(5000);
                return;
            }
            string[] strs = commands[2].Split("。");
            int i = 0;

            Console.WriteLine(dicPath);

            var files = Directory.GetFiles(dicPath);
            var Path = dicPath + "\\" + files.Length.ToString();
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
            foreach (string str in strs)
            {
                SynhthesisAudio(commands[1], str, commands[3], Path);
            }
        }

        static void Main(string[] args)
        {
            //string message = "Repeat t 你好，我是牧濑红莉牺，是你的助手。很高兴见到你，从今往后，请多多关照。 87";
            string exitMessage = "Exit";
            LoadConfig();
            InitVITS();
            //HandleInput(message);
            HandleInput(exitMessage);
            //NetWork();
            Console.ReadLine();
        }

        private static void VITSOUT(VITS sender, string e)
        {
            Console.WriteLine(e);
        }

        private static void NetWork()
        {
            Console.WriteLine("Hello, World!");
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress iPAdr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEp = new IPEndPoint(iPAdr, 9888);
            listenfd.Bind(ipEp);
            listenfd.Listen(1);
            Console.WriteLine("Server Setup Succ!");

            //checkRead
            List<Socket> checkRead = new List<Socket>();

            while (true)
            {
                checkRead.Clear();
                checkRead.Add(listenfd);
                foreach (ClientState clientState in clients.Values)
                {
                    if (clientState.socket != null)
                    {
                        checkRead.Add(clientState.socket);
                    }
                }

                Socket.Select(checkRead, null, null, 1000);

                foreach (Socket s in checkRead)
                {
                    if (s == listenfd)
                    {
                        ReadListenfd(s);
                    }
                    else
                    {
                        ReadClientfd(s);
                    }
                    Console.WriteLine("handle over");
                }
            }
        }

        public static void ReadListenfd(Socket listenfd)
        {
            Console.WriteLine("Accept");
            Socket clientfd = listenfd.Accept();
            ClientState state = new ClientState();
            state.socket = clientfd;
            clients.Add(clientfd, state);
        }

        public static bool ReadClientfd(Socket clientfd)
        {
            ClientState state = clients[clientfd];
            int count = 0;
            try
            {
                count = clientfd.Receive(state.readBufff);
            }
            catch (Exception ex)
            {
                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Receive SocketException " + ex.ToString());
                return false;
            }
            if(count == 0)
            {
                clientfd.Close();
                clients.Remove(clientfd);
                Console.WriteLine("Socket Closed ... ");
                return false;
            }
            string recvStr = System.Text.Encoding.Default.GetString(state.readBufff, 0, count);
            Console.WriteLine("Server Receive " + recvStr);
            
            //回复
            string sendStr = clientfd.RemoteEndPoint.ToString() + " : " + recvStr;
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);
            clientfd.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallBack, clientfd);
            Console.WriteLine("Server wana send " + sendStr);
            return true;
        }

        private static void SendCallBack(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int count = socket.EndSend(ar);
                Console.WriteLine("Socket Send Succ                " + count);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket Send fail " + ex.ToString());
            }
        }
    }
}