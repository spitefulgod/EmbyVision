using EmbyVision.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static EmbyVision.Rest.RestClient;

namespace EmbyVision.Base
{
    public static class Common
    {
        /// <summary>
        /// Gets the external IP address, used for determining if a server is local.
        /// </summary>
        /// <returns></returns>
        public static RestResult GetExternalIPAddr()
        {
            using (RestClient Client = new RestClient("http://www.whatismypublicip.com/"))
            {
                RestResult Result = Client.Execute("", RestClient.PostType.GET);
                if (Result.Success)
                {
                    int Start = Result.Response.ToLower().IndexOf("up_finished\">");
                    if (Start > -1)
                    {
                        int End = Result.Response.IndexOf("<", Start + "up_finished\">".Length);

                        if (Start >= 0 && End >= 0)
                        {
                            Result.Response = Result.Response.Substring(Start + "up_finished\">".Length, End - Start - "up_finished\">".Length).Trim();
                            return Result;
                        }
                    }
                }
                return new RestResult() { Success = false, Error = "IP extraction failure" };
            }
        }
        /// <summary>
        /// Gets a SHA1 hashcode for a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string HashCodeSha1(string value)
        {
            byte[] buffer = ToByteArray(value);
            SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            string hash = BitConverter.ToString(
                cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");

            return hash;
        }
        /// <summary>
        /// Gets a MD5 hash code from a string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string HashCodeMd5(string value)
        {
            byte[] buffer = ToByteArray(value);
            MD5CryptoServiceProvider cryptoTransformMd5 = new MD5CryptoServiceProvider();
            string hash = BitConverter.ToString(
                cryptoTransformMd5.ComputeHash(buffer)).Replace("-", "");

            return hash;
        }
        public static byte[] ToByteArray(string Value)
        {
            System.Text.ASCIIEncoding encoder = new System.Text.ASCIIEncoding();
            byte[] Buffer = encoder.GetBytes(Value);
            encoder = null;
            return Buffer;
        }
        /// <summary>
        /// Some common routines for getting basic lists.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAlphaNumList()
        {
            return new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        }
        public static List<string> GetAlphaList()
        {
            return new List<string>() { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        }
        public static List<string> NumberList(int Start, int End)
        {
            if (Start > End)
            {
                int temp = Start;
                Start = End;
                End = temp;
            }
            List<string> returnList = new List<string>();
            for (var i = Start; i <= End; i++)
                returnList.Add(i.ToString());
            return returnList;
        }
        public static string ToOrdinal(int number)
        {
            switch (number % 100)
            {
                case 11:
                case 12:
                case 13:
                    return number.ToString() + "th";
            }

            switch (number % 10)
            {
                case 1:
                    return number.ToString() + "st";
                case 2:
                    return number.ToString() + "nd";
                case 3:
                    return number.ToString() + "rd";
                default:
                    return number.ToString() + "th";
            }
        }
    }
}
