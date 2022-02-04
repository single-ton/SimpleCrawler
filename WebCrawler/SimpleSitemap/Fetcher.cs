using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler.SimpleSitemap
{
    public class Fetcher
    {
        private readonly string userAgent;

        public Fetcher()
        {
            userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36";
        }

        public Fetcher(string userAgent)
        {
            this.userAgent = userAgent;
        }

        public async Task<string> Fetch(Uri location)
        {
            //Automatically handle gzip compressed content
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml, */*");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

                return await client.GetStringAsync(location);
            }
        }
       
    }
}
