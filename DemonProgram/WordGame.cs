using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DemonProgram
{

    // 2개의 세션이 들어와 있다고 가정.


    class WordGame
    {

        Socket[] players = new Socket[2];
        string url;
        string apikey;
        string type;
        XmlDocument xml = new XmlDocument();
        Dictionary<Socket, string> playerinfo = null;
        string message;

        List<string> history = new List<string>(); //검색 기록 저장.

        public WordGame(Socket p1, Socket p2, Dictionary<Socket, string> game_player)
        {

            this.playerinfo = game_player;
            int i = 0;
            foreach(KeyValuePair<Socket, string> pl in game_player)
            {
                players[i] = pl.Key;
            }

            url = "https://krdict.korean.go.kr/api/search?key=";
            apikey = "EBB6D3290D88C645CF1452F7DA3229D0";
            type = "&part=word&pos=1&q=";
        }


        public void sendPacket(Socket s1, string message)
        {
            string pkg = playerinfo[s1];
            string[] pkg_info = pkg.Split(',');
            string[] room_and_message = pkg_info[4].Split('/');
            room_and_message[1] = message;
            string insert = string.Join(",", room_and_message);
            pkg_info[4] = insert;

            string new_pkg = string.Join(",", pkg_info);

            s1.Send(Encoding.Default.GetBytes(new_pkg));

        }


        public void RunGame()
        {
            int turn = 0;
            while (true)
            {
                // 1, 2번 차례대로 socket을 받자
                
                byte[] receive = new byte[players[0].Available];
                players[0].Receive(receive);
                string[] packet = Encoding.Default.GetString(receive).Split(',');
                if (players[0].RemoteEndPoint.ToString().Split(':')[1] != packet[0] || packet[2]!="1")
                    break;
                string word = packet[4].Split('/')[1];

                if (checkLastWord(word) == false)
                {
                    message = "끝 단어와 일치하지 않습니다.";
                    sendPacket(players[turn], message);
                    continue;
                }
                else
                {
                    if (checkDuplicate(word) == true)
                    {
                        message = "이미 사용된 단어입니다.";
                        sendPacket(players[turn], message);
                        continue;
                    }
                    else
                    {
                        message = searchWord(word);
                        if(message== "존재 하지 않습니다.")
                        {
                            sendPacket(players[turn],message);
                            continue;
                        }
                        else
                        {
                            sendPacket(players[turn], message);
                            turn = (turn + 1) % 2;
                        }
                    }
                }

            }

        }



        public string searchWord(string search)
        {
            // Query문 만들기
            string query = url + apikey + type + search;
            // Request문 보내기.
            WebRequest request = WebRequest.Create(query);
            request.Method = "GET";

            // Response 받기.
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            string result = reader.ReadToEnd();

            // XML로 만들어 주기.
            xml.LoadXml(result);
            stream.Close();


            XmlNodeList xnlist = xml.GetElementsByTagName("item");
            XmlNodeList word_count = xml.GetElementsByTagName("total");
            int count = int.Parse(word_count[0].InnerText);

            if (count == 0)
            {
                //wordlist.Text += "존재하지 않습니다\r\n";
                string res = "존재 하지 않습니다.";
                return res;
            }
            else
            {
                string mean = xnlist[0]["sense"]["definition"].InnerText;
                //wordlist.Text += mean + "\r\n";
                return mean;
            }
        }
        public bool checkLastWord(string word)
        {
            if (history.Count == 0)
                return true;
            else
            {
                string lastword = history.Last();
                if (lastword[lastword.Length - 1] == word[0])
                    return true;
                else
                    return false;
            }
        }

        public bool checkDuplicate(string word)
        {
            if (history.Contains(word))
                return true;
            else
                return false;
        }
    }
}
