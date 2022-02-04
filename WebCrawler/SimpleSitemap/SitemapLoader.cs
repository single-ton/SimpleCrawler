using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebCrawler.Interfaces;

namespace WebCrawler.SimpleSitemap
{
    public class SitemapLoader: ISitemapLoader
    {
        private Fetcher sitemapFetcher;
        private Uri baseUri;
        private Uri robotsTxtLocation;
        private IEnumerable<Uri> sitemapLocations;
        private Fetcher fetcher;
        public SitemapLoader(string url/*url of any web page from site*/)
        {
            this.sitemapFetcher = new Fetcher();
            baseUri = new Uri(url);
            if (!baseUri.AbsolutePath.Equals("/"))
                baseUri = new Uri(baseUri.AbsoluteUri.Replace(baseUri.AbsolutePath, ""));
            robotsTxtLocation = new Uri(baseUri, "/robots.txt");
            fetcher = new Fetcher();
           
        }
        public bool SitemapExists()
        {
            RobotsTxtParser robots = new RobotsTxtParser();
            IEnumerable<Uri> sitemaps=null;
            string robotsContent = null;
            try
            {
                robotsContent = fetcher.Fetch(robotsTxtLocation).GetAwaiter().GetResult();
                sitemaps = robots.GetSitemapLinks(robotsContent, baseUri);
            }
            catch { }
            if (sitemaps.Count() == 0)
                return false;
            return true;
        }
        public async Task<List<Uri>> GetUrlsFromSitemaps()
        {
            sitemapLocations = null;
            RobotsTxtParser robots = new RobotsTxtParser();
            string robotsContent=null;
            try
            {
                robotsContent = await fetcher.Fetch(robotsTxtLocation);
                sitemapLocations = robots.GetSitemapLinks(robotsContent,baseUri);
            }
            catch(Exception ex)
            {//robots.txt not exists

            }
            List<Uri> result = new List<Uri>();
            if(sitemapLocations!=null)
                foreach(var sitemapLocation in  sitemapLocations)
                {
                    SitemapParser sitemapParser = new SitemapParser();
                    result.AddRange(await sitemapParser.GetUrls(sitemapLocation));
                }
            return result;
        }
        
    }
}
