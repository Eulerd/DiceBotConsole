using System;
using System.Threading;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            AutoResetEvent AREve = new AutoResetEvent(false);

            TimerCallback TimerDele = new TimerCallback(CheckDemo);

            Timer DemoTimer = new Timer(TimerDele, AREve, 0, 1000);

            AREve.WaitOne(-1);
        }

        static void CheckDemo(object info)
        {
            AutoResetEvent AutoEve = (AutoResetEvent)info;

            Console.WriteLine(DateTime.Now);
        }
    }
}
