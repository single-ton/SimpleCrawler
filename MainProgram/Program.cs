
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebCrawler;
using WebCrawler.SimpleCrawler;
using WebCrawler.SimpleSitemap;

namespace MainProgram
{
    class Program
    {
        static async Task Main(string[] args)
        {


            Console.WriteLine("Enter url: ");
            string url = Console.ReadLine();
            //string url = "https://www.vekreklami.com.ua/";
            //string url = "https://john.blogspot.com/";
            //string url = "https://pc103help.blogspot.com/";
            //string url = "https://seva22.blogspot.com/p/11.html";
            //string url = "https://seosly.com/newsletter/";
            Crawler simpleCrawler = null;
            try { simpleCrawler = new Crawler(url, null, "127.0.0.1:8888"); } catch { }

            if (simpleCrawler != null)
                simpleCrawler.Start(100);
            SitemapLoader sitemapLoader = new SitemapLoader(url);
            var sitemapLinks = await sitemapLoader.GetUrlsFromSitemaps();
            var crawlerLinks = simpleCrawler.ListResultLinks;
            while (!simpleCrawler.Ended)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            if (sitemapLinks.Count != 0)
            {
                Console.WriteLine("Urls FOUNDED IN SITEMAP.XML but not founded after crawling a web site");
                foreach (var link in sitemapLinks)
                {
                    if (crawlerLinks.Where(x => x.Url.Equals(link)).Count() == 0)
                        Console.WriteLine(link);
                }
                Console.WriteLine();
                Console.WriteLine("Urls FOUNDED BY CRAWLING THE WEBSITE but not in sitemap.xml");
                foreach (var link in crawlerLinks)
                {
                    if (sitemapLinks.Where(x => x.Equals(link.Url)).Count() == 0)
                        Console.WriteLine(link.Url);
                }
                Console.WriteLine();
            }
            if (crawlerLinks.Count != 0)
            {
                Console.WriteLine("Timing");
                foreach (var x in crawlerLinks.OrderBy(x => x.Timing).ToList())
                {
                    Console.WriteLine(x);
                }
                Console.WriteLine();
            }
            Console.WriteLine("Urls(html documents) found after crawling a website: " + crawlerLinks.Count().ToString());
            Console.WriteLine("Urls found in sitemap: " + sitemapLinks.Count().ToString());


            Console.ReadKey();
        }
    }
}
