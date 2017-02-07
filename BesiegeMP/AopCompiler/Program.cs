using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AopCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            var p = Process.Start(@"C:\Program Files (x86)\MSBuild\14.0\Bin\csc.exe",
            string.Join(" ", args));
            p.WaitForExit();
            */
            Console.Write(string.Join(" ", args));
        }
    }
}
