using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SortedDictionary<DateTime, int> dic = new SortedDictionary<DateTime, int>();

            dic.Add(DateTime.Now, 0);
            dic.Add(DateTime.MaxValue, 1);
            dic.Add(DateTime.MinValue, 2);
            dic.Add(DateTime.Today, 3);
            dic.Add(DateTime.Now.AddHours(3.14), 4);

            foreach(var item in dic)
            {
                Console.WriteLine("{0} : {1}", item.Key, item.Value);
            }
        }
    }
}
