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
        
        byte[] ServerIP = { 192, 168, 0, 85 };
        string ServerPort = "9000";

        //        List<Player>[] players=null;
        Dictionary<string, string>[] game = null;
        /*
        public class Player
        {
            //string session;
            string id;
            bool alive;
            int state;
            string msg;
            public Player(string id, bool alive, int state, string msg)
            {
                //this.session = s;
                this.id = id;
                this.alive = alive;
                this.state = state;
                this.msg = msg;
            }
            public void logOut()
            {
                this.alive = false;
            }
            public void chState(int s)
            {
                this.state = s;
            }
        }
        */


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
                else outList.Items.Add(str);
            }
        }

        private void UserManager_Load(object sender, EventArgs e)
        {
            game = new Dictionary<string, string>[3];
            for (int i = 0; i < 3; i++) game[i] = new Dictionary<string, string>();

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
                        // 처음 받은 소켓에 메세지 바로 receive가능하면 players[0]에 넣기
                        game[0].Add(sArr[1], "");
                    }
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public static bool isAlive(Socket ss)
        {
            if (ss == null) return false;
            if (!ss.Connected) return false;

            //1000마이크로초, 1ms초 동안 응답을 기다림
            //SelectRead모드는 해당 소켓이 readable인지 
            bool r1 = ss.Poll(1000, SelectMode.SelectRead);
            //읽을 수 있는 데이터가 없으면
            bool r2 = ss.Available == 0;
            if (r1 && r2) return false;
            else
            {
                //예외처리로 들어가면 연결 문제이므로 false반환 
                try
                {
                    byte[] b = new byte[1]; b[0] = 0;
                    //인자를 조건에 맞게 넣어, 발생가능한 오류들은 소켓 연결과 관련된 것들이 되도록
                    int sentByteCount = ss.Send(new byte[1], 0, SocketFlags.OutOfBand);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        void SessionProcess()
        {
            while (true)
            {
                foreach (Socket ss in socks)
                {
                    //if (!isAlive(ss)) AddList(ss.RemoteEndPoint.ToString(), false);
                    if (ss.Available > 0)
                    {
                        byte[] ba = new byte[ss.Available];
                        ss.Receive(ba);
                        ReadProcess(ss, ba);
                    }
                
                }
                Thread.Sleep(100);
            }
        }
        void ReadProcess(Socket ss, byte[] ba)
        {
            string str = Encoding.Default.GetString(ba).Trim();
            string[] sa = str.Split(',');
            if (sa[2] == "1")
            {
                string state = sa[3];
                if (game[0].ContainsKey(sa[0]) && state!="0")
                {
                    game[int.Parse(state)].Add(sa[0], str);
                    AddList(str, true);
                    game[0].Remove(sa[0]);
                }
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
