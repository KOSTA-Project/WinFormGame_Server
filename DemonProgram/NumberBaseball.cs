using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DemonProgram
{
    class NumberBaseball
    {
        Socket player1 = null;
        Socket player2 = null;

        int nbLen = 4;
        int turn = 0;
        int answer = -1;
        int[] numcnt = new int[10];
        Socket winner = null;
        
        public NumberBaseball(Socket player1, Socket player2)
        {
            this.player1 = player1;
            this.player1 = player1;
            
            Random r = new Random();
            answer = r.Next(0, (int)Math.Pow(10, nbLen));
            int val = answer;
            int div = (int)Math.Pow(10, nbLen - 1);
            while(div>=1)
            {
                int mok = val / div;
                numcnt[mok]++;
                val %= div;
                div /= 10;
            }
        }

        public void oneRound(Socket p1, Socket p2, string q1, string q2)
        {
            
            string s1 = for1Query(p1, int.Parse(q1));
            string s2 = for1Query(p2, int.Parse(q2));

            if (winner == null) turn++;

        }


        public string for1Query(Socket sock, int query)
        {
            int ball = 0, strike = 0;
            int div = (int)Math.Pow(10, nbLen - 1);
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

            if (strike == nbLen) winner = sock;

            return $"{strike} strike, {ball} ball";
        }

        // 해당 내용은 웹에서 확인하는 걸로
        public bool isValidQuery(string query)
        {
            if (query.Length != nbLen) return false;
            foreach (char c in query)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

    }
}
