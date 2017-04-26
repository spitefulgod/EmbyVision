using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmbyVision.Base
{
    public static class Logger
    {
        public static void Log(string Information)
        {
            Console.WriteLine(string.Format("{0:dd MMM yyyy HH:mm:ss} - {1}", DateTime.Now, Information));
        }
        public static void Log(string Source, string Information)
        {
            Log(string.Format("{0} - {1}", Source, Information));
        }
    }
}
