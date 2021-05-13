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
        Dictionary<Socket, string>[] game = null;

        List<GameRoom>[] gamerooms = null;
        

        public class GameRoom
        {
            Socket player1;
            Socket player2;
            int cnt;
            int[] numcnt = new int[10];
            int answer = -1;

            public GameRoom(Socket p)
            {
                player1 = p;
                player2 = null;
                cnt = 1;
                
                // for nb Rules
                Random r = new Random();
                answer = r.Next(0, 10000);
                int val = answer;
                int div = 1000;
                while (div >= 1)
                {
                    int mok = val / div;
                    numcnt[mok]++;
                    val %= div;
                    div /= 10;
                }
            }

            public Socket getPlayer1()
            {
                return player1;
            }
            public Socket getPlayer2()
            {
                return player2;
            }
            public void addPlayer(Socket p)
            {
                if (player1 == null) player1 = p;
                else player2 = p;
                cnt++;
            }
            public bool isEmpty()
            {
                if (cnt < 2) return true;

                else return false;
            }

            public string nbResult(string msg)
            {
                int query = int.Parse(msg);
                int ball = 0, strike = 0;
                int div = 1000;
                int ans = answer;
                int[] nums = new int[10];
                Array.Copy(numcnt, nums, 10);

                while (div >= 1)
                {
                    int cur = query / div;
                    if (ans / div == cur) strike++;
                    if (nums[cur] > 0) ball++;
                    nums[cur]--;
                    query %= div;
                    ans %= div;
                    div /= 10;
                }
                ball -= strike;

                return $"{strike} strike, {ball} ball";
            }
            
        }

        
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
            game = new Dictionary<Socket, string>[3];
            for (int i = 0; i < 3; i++) game[i] = new Dictionary<Socket, string>();

            
            gamerooms = new List<GameRoom>[3];
            for (int i = 0; i < 3; i++) gamerooms[i] = new List<GameRoom>();

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
                        lock (socks)
                        {
                            socks.Add(sock);
                        }
                        game[0].Add(sock,"");
                        
                       
                    }
                    Thread.Sleep(10);
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
                for(int i = 0; i<socks.Count; i++)
                {
                    if (socks[i].Available > 0)
                    {
                        byte[] ba = new byte[socks[i].Available];
                        socks[i].Receive(ba);
                        // Socket send
                        string[] packet = Encoding.Default.GetString(ba).Split(',');

                        ReadProcess(socks[i], ba); //방만드는 작업.
                        
                        
                        // 


                    }
                }
 
                Thread.Sleep(100);
            }
        }
        void ReadProcess(Socket ss, byte[] ba)
        {
            string str = Encoding.Default.GetString(ba).Trim();
            string[] sa = str.Split(',');

            int state = int.Parse(sa[3].Trim());       // 해당 소켓의 페이지 상태

            // 해당 연결이 살아있는지
            if (sa[2] == "1")
            {
                // 로그인 상태 --> 게임 선택한 상태
                if (game[0].ContainsKey(ss) && state != 0)
                {
                    int idx = -1;
                    // gameroom 할당하기
                    bool isInRoom = false;
                    // 현재 있는 방 중 빈방 존재 시, 들어감
                    for (int i = 0; i < gamerooms[state].Count; i++)
                    {
                        if (gamerooms[state][i].isEmpty())
                        {
                            idx = i;
                            gamerooms[state][i].addPlayer(ss);
                            isInRoom = true;

                            // if(2)
                        }
                    }
                    // 빈 방이 없으면 새로운 방 들어가서 대기
                    if (!isInRoom)
                    {
                        idx = gamerooms[state].Count;     // 새로 추가될 방의 번호
                        gamerooms[state].Add(new GameRoom(ss));

                    }
                    // 방 번호 부여
                    str += idx.ToString() + "/";
                    game[state].Add(ss, str);
                    AddList("게임 방 선택: " + str, true);
                    game[0].Remove(ss);

                    //ss.Send(Encoding.Default.GetBytes(str));

                    if (!gamerooms[state][idx].isEmpty())
                    {
                        Socket target = gamerooms[state][idx].getPlayer1();
                        Socket target2 = gamerooms[state][idx].getPlayer2();
                        string s1 = game[state][target] + "gamestart";
                        string s2 = game[state][target2] + "gamestart";

                        if (state == 2)
                        {

                            Random r = new Random();
                            string a = r.Next(0, 10000).ToString();
                            s1 += "/" + a;
                            s2 += "/" + a;
                        }
                        target.Send(Encoding.Default.GetBytes(s1));
                        target2.Send(Encoding.Default.GetBytes(s2));
                    }

                }
                else if(sa[4].Split('/')[0]!="")
                {
                    int gameN = int.Parse(sa[3]);
                    int rn = int.Parse(sa[4].Split('/')[0]);
                    string msg = sa[4].Split('/')[1];
                    
                    Socket target = gamerooms[gameN][rn].getPlayer1();
                    if(target == ss)
                    {
                        target = gamerooms[gameN][rn].getPlayer2();
                    }

                    string real = game[gameN][target]+msg;
                    target.Send(Encoding.Default.GetBytes(real));

                }

                // 게임 내 정보 변화
                //else
                //{
                //    int roomidx = int.Parse(sa[4].Split('/')[0]);
                //    if (state == 2)
                //    {
                //        string ret = gamerooms[state][roomidx].nbResult(sa[4].Split('/')[1]);
                //        ss.Send(Encoding.Default.GetBytes(ret));
                //    }
                //}
            }
            // 현재 연결 끊김
            else
            {
                // 이전에 게임을 선택한 상태라면
                if (state != 0)
                {
                    int room = int.Parse(sa[4].Split('/')[0]);
                    // 게임 방 상태 변경하기
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
