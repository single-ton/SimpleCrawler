using System;
using Functionality;

namespace MainProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter url: ");
            string url = Console.ReadLine();
            Crawler simpleCrawler = new Crawler(url, null, "127.0.0.1:8888");
            Console.WriteLine("Started");
            simpleCrawler.Start();
            Console.ReadKey();
        }
    }
}
