using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Functionality
{
    public class UrlWithTiming
    {
        public string Url { get; private set; }
        public long Timing { get; set; }
        public UrlWithTiming(string url, long timing)
        {
            this.Timing = timing;
            this.Url = url;
        }
        public override string ToString()
        {
            if (Timing == 0)
                ;
            return Url+" "+Timing;
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
