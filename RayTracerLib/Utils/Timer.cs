using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTracerLib
{
    /// <summary>
    /// Timer used to display processing times
    /// </summary>
    /// <param name="leftPadding"> the amount of left padding </param>
    public class Timer(int leftPadding)
    {
        private readonly string _padding = new(' ', leftPadding);
        private readonly Stopwatch sw = Stopwatch.StartNew();

        /// <summary>
        /// Restarts the timer and displays a message
        /// </summary>
        /// <param name="msg"> the message to display </param>
        public void Restart(string msg)
        {
            
            if(msg.Length > 0) 
            {
                Console.WriteLine(_padding + msg);
            }
            sw.Restart();
        }

        /// <summary>
        /// Prints a message followed by the time elapsed since the last restart
        /// </summary>
        /// <param name="msg"> the message to display </param>
        public void Print(string msg)
        {
            Console.WriteLine(_padding + msg + sw.Elapsed.TotalSeconds + " s");
        }

    }
}
