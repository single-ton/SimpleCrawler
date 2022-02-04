using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler.SimpleSitemap
{
    public class RobotsTxtParser
    {
        private const string SitemapTag = "Sitemap:";

        public IEnumerable<Uri> GetSitemapLinks(string robotsTxt, Uri baseUri)
        {
            if (string.IsNullOrEmpty(robotsTxt))
                return Enumerable.Empty<Uri>();
            //all sitemaps string in file
            var lines = robotsTxt.Split('\n')
                .Select(x => x.Trim())
                .Where(x => x.StartsWith(SitemapTag, StringComparison.OrdinalIgnoreCase));
            //delete everything except the link
            var sitemaps = lines
                .Select(x => x.Substring(SitemapTag.Length))
                .Select(x => x.Trim());

            var validSitemaps = new List<Uri>();
            foreach (var sitemap in sitemaps)
            {
                Uri sitemapUri;
                if (Uri.TryCreate(baseUri, sitemap, out sitemapUri))
                    validSitemaps.Add(sitemapUri);
            }
            return validSitemaps;
        }
    }
}
