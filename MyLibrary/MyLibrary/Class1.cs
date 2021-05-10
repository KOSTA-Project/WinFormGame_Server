using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MyLibrary
{
    public class mylib
    {
        public static string GetEncrypt(string str)
        {
            MD5 md = new MD5CryptoServiceProvider();
            byte[] ba = md.ComputeHash(Encoding.Default.GetBytes(str));

            string ret = "";
            for (int i = 0; i < ba.Length; i++)
            {
                ret += ba[i].ToString("x2");
            }
            return ret;
        }
        public static string GetToken(int idx, string str, char deli)
        {
            return str.Split(deli)[0];
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

        public class SQLDB
        {
            SqlConnection sqlconn = new SqlConnection();
            SqlCommand sqlcmd = new SqlCommand();
            string ConnStr;

            public SQLDB(string str)
            {
                ConnStr = str;
                sqlconn.ConnectionString = ConnStr;
                sqlconn.Open();
                sqlcmd.Connection = sqlconn;
            }
            // 
            public object Run(string sql)
            {
                try
                {
                    sqlcmd.CommandText = sql;
                    if (mylib.GetToken(0, sql.Trim(), ' ').ToUpper() == "SELECT")
                    {
                        SqlDataReader sdr = sqlcmd.ExecuteReader();
                        DataTable dt = new DataTable();
                        dt.Load(sdr);
                        sdr.Close();
                        return dt;
                    }
                    else
                    {
                        return sqlcmd.ExecuteNonQuery();
                    }
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1);
                    return null;
                }
            }

            public object Get(string sql)
            {
                try
                {
                    sqlcmd.CommandText = sql;
                    if (mylib.GetToken(0, sql.Trim(), ' ').ToUpper() == "SELECT")
                    {
                        object obj = sqlcmd.ExecuteScalar();
                        return obj;
                    }
                }
                catch (Exception e1)
                {
                    Console.WriteLine(e1);
                }
                return null;
            }
        }

    }
}
