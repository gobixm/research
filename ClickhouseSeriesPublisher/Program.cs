using System;

namespace ClickhouseSeriesPublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            new Publisher().Run(1);
            Console.WriteLine("press to quit");
            Console.ReadKey();
        }
    }
}