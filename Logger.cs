using System;
using System.Diagnostics;

namespace EyeScroller
{
    class Logger
    {
        public static void Log(params Object[] args)
        {
            var output = "";
            foreach (var obj in args)
            {
                output += " " + obj.ToString();
            }
            Debug.WriteLine(output);
        }
    }
}
