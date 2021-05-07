using MyLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemonProgram
{
    public partial class UserManager : Form
    {
        public UserManager()
        {
            InitializeComponent();
        }
        byte[] ServerIP = { 192, 168, 0, 52 };
        string ServerPort = "9000";

        List<Socket> socks = new List<Socket>();
        Socket sock = null;
        Socket sockServer = null;

        Thread threadServer = null;
        Thread threadSession = null;

        delegate void cbAddList(string str, bool isIn);

        void AddList(string str, bool isIn)
        {
            if (inList.InvokeRequired || outList.InvokeRequired)
            {
                cbAddList cb = new cbAddList(AddList);
                Invoke(cb, new object[] { str, isIn });
            }
            else
            {
                if (isIn) inList.Items.Add(str);
                else
                {
                    outList.Items.Add(str);
                    inList.Items.Remove(str);
                }
            }
        }

        private void UserManager_Load(object sender, EventArgs e)
        {
            sockServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            threadServer = new Thread(ServerProcess);
            threadServer.IsBackground = true;    //주 스레드 종료시 함께 종료
            threadSession = new Thread(SessionProcess);
            threadSession.IsBackground = true;

            threadServer.Start();
            threadSession.Start();
        }

        void ServerProcess()
        {
            IPAddress ip = new IPAddress(ServerIP);
            IPEndPoint ep = new IPEndPoint(ip, int.Parse(ServerPort));
            sockServer.Bind(ep);
            sockServer.Listen(50000);
            try
            {
                while (true)
                {
                    sock = sockServer.Accept();

                    if (sock != null)
                    {
                        string[] sArr = sock.RemoteEndPoint.ToString().Split(':');
                        AddList(sock.RemoteEndPoint.ToString(), true);
                        socks.Add(sock);
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        void SessionProcess()
        {
            while (true)
            {
                List<Socket> removelist = new List<Socket>();

                foreach (Socket ss in socks)
                {
                    if (!mylib.isAlive(ss))
                    {
                        removelist.Add(ss);
                    }
                }
                foreach (Socket ss in removelist)
                {
                    AddList(ss.RemoteEndPoint.ToString(), false);
                    socks.Remove(ss);
                }
                Thread.Sleep(100);
            }
        }

        private void UserManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sockServer != null) sockServer.Close();
            if (sock != null) sock.Close();
            foreach (Socket ss in socks) ss.Close();

            if (threadServer != null) threadServer.Abort();
            if (threadSession != null) threadSession.Abort();
        }
    }
}
