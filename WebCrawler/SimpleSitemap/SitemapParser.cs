using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WebCrawler.SimpleSitemap
{
    public class SitemapParser
    {
        #region names
        private const string SitemapSchema = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private readonly XName UrlSetName = XName.Get("urlset", SitemapSchema);
        private readonly XName SitemapIndexName = XName.Get("sitemapindex", SitemapSchema);
        private readonly XName SitemapName = XName.Get("sitemap", SitemapSchema);
        private readonly XName LocationName = XName.Get("loc", SitemapSchema);
        private readonly XName UrlName = XName.Get("url", SitemapSchema);
        #endregion
        private Fetcher fetcher;
        public SitemapParser()
        {
            fetcher = new Fetcher();
        }
        public async Task<IEnumerable<Uri>> GetUrls(Uri uri/*url sitemap*/)
        {
            List<Uri> result = new List<Uri>();
            string sitemapContent = await fetcher.Fetch(uri);
            XElement sitemapXElement = XElement.Parse(sitemapContent);
            //Check if this is Index Sitemap
            if (sitemapXElement.Name.Equals(SitemapIndexName))
            {
                return await ParseIndexSitemap(sitemapXElement);
            }
            //Check if this is Normal Sitemap with items
            if (sitemapXElement.Name.Equals(UrlSetName))
            {
                return ParseSitemapUrls(sitemapXElement);
            }
            return result;
        }
        private async Task<List<Uri>> ParseIndexSitemap(XElement sitemapXElement)
        {
            List<Uri> listUrls = new List<Uri>();
            foreach (var urlElement in sitemapXElement.Elements(SitemapName))
            {
                var locElement = urlElement.Elements(LocationName).FirstOrDefault();
                if (locElement == null || string.IsNullOrWhiteSpace(locElement.Value))
                    continue;
                Uri uriSitemap = new Uri(locElement.Value);
                listUrls.AddRange(await ParseSitemapUrls(uriSitemap));
            }
            return listUrls;
        }
        private async Task<List<Uri>> ParseSitemapUrls(Uri uri)
        {
            string sitemapContent = await fetcher.Fetch(uri);
            XElement sitemapXElement = XElement.Parse(sitemapContent);
            return ParseSitemapUrls(sitemapXElement);
        }
        private List<Uri> ParseSitemapUrls(XElement sitemapXElement)
        {
            List<Uri> listUrls = new List<Uri>();
            var urlElements = sitemapXElement.Elements(UrlName);
            foreach (var urlElement in urlElements)
            {
                listUrls.Add(new Uri(urlElement.Element(LocationName).Value));
            }
            return listUrls;
        }

    }
}
