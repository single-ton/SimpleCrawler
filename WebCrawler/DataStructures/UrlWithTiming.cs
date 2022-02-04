using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler.DataStructures
{
    public class UrlWithTiming
    {
        public Uri Url { get; private set; }
        public long Timing { get; set; }
        public UrlWithTiming(Uri url, long timing)
        {
            this.Timing = timing;
            this.Url = url;
        }
        public override string ToString()
        {
            return Url + " " + Timing;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is UrlWithTiming)
            {
                return (obj as UrlWithTiming).Url.Equals(this.Url);
            }
            else
                return false;
        }
    }
}
