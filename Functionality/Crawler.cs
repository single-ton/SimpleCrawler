using Functionality.Events;
using Functionality.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Functionality
{
    public class Crawler : ICrawler
    {
        private List<UrlWithTiming> listLinks;
        private List<string> listRawLinks;
        private List<Thread> listThreads;
        private string url;
        private string hostContains;
        private string proxy;
        private int CountThreads;
        private CancellationTokenSource tokenSource;

        public event EventHandler<OnCompletedEventArgs> OnCompleted;

        public event EventHandler<OnErrorEventArgs> OnError;

        public CookieContainer CookiesContainer { get; set; }

        public Crawler(string url, string hostContains = null, string proxy = null) 
        {
            this.url = url;
            try
            {
                Uri uri = new Uri(url);
                this.url = uri.AbsoluteUri;
                if (hostContains is null)
                {
                    this.hostContains = uri.Host.Replace("www.", "");
                }
                else
                    this.hostContains = hostContains;
            }
            catch(Exception ex)
            {
                throw;
            }
            listLinks = new List<UrlWithTiming>();
            listRawLinks = new List<string>();
            listThreads = new List<Thread>();
            CountThreads = 10;
            this.OnCompleted += Crawler_OnCompleted;
            OnError += Crawler_OnError;
            this.proxy = proxy;
        }

        private void Crawler_OnError(object sender, OnErrorEventArgs e)
        {
            //Console.WriteLine(e.Uri+" "+ e.Exception.Message);
        }

        private void Crawler_OnCompleted(object sender, OnCompletedEventArgs e)
        {
            //Console.WriteLine(e.Uri.AbsoluteUri+ " " + e.Milliseconds);
            try
            {
                listThreads.Remove(listThreads.Where(x => x.ManagedThreadId == e.ThreadId).First());
            }
            catch (Exception ex) 
            { }
            listLinks.Where(x => x.Url == e.Uri.OriginalString).First().Timing = e.Milliseconds;
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(e.PageSource);
            var nodes = doc.DocumentNode.SelectNodes("//a[@href!='']");
            if (nodes is null)
                return;
            foreach(var node in nodes)
            {
                string link = node.Attributes["href"].Value;
                Uri uri = null;
                try
                {
                    uri = new Uri(link);
                }
                catch (Exception ex)
                {
                }
                if (uri != null)
                {
                    if (uri.Host.Contains(hostContains))
                    {
                        if (!listLinks.Contains(new UrlWithTiming(link, e.Milliseconds)) && !listRawLinks.Contains(link))
                        {
                            if (link == null)
                                ;
                            listRawLinks.Add(link);
                        }
                    }
                }
                
            }
            if (listThreads.Count == 0 && listRawLinks.Count == 0)
            {//end process
                tokenSource.Cancel();
            }
        }

        public Thread GetPageSource(Uri uri)
        {
            Thread t = new Thread(() =>
            {
                var pageSource = string.Empty;
                try
                {
                    var watch = new Stopwatch();
                    var request = (HttpWebRequest)WebRequest.Create(uri);
                    
                    request.Accept = "*/*";
                    request.ServicePoint.Expect100Continue = false;
                    request.ServicePoint.UseNagleAlgorithm = false;
                    request.AllowWriteStreamBuffering = false;
                    request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.AllowAutoRedirect = true;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36";
                    //request.Timeout = 5000;
                    request.KeepAlive = true;
                    request.Method = "GET";
                    if (proxy != null) request.Proxy = new WebProxy(proxy);
                    request.CookieContainer = this.CookiesContainer;
                    request.ServicePoint.ConnectionLimit = int.MaxValue;
                    watch.Start();
                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        foreach (Cookie cookie in response.Cookies) this.CookiesContainer.Add(cookie);
                        
                            
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (response.ContentEncoding.ToLower().Contains("gzip"))
                            {
                                using (GZipStream stream = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress))
                                {
                                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        pageSource = reader.ReadToEnd();
                                    }
                                }
                            }
                            else if (response.ContentEncoding.ToLower().Contains("deflate"))
                            {
                                using (DeflateStream stream = new DeflateStream(response.GetResponseStream(), CompressionMode.Decompress))
                                {
                                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        pageSource = reader.ReadToEnd();
                                    }
                                }
                            }
                            else
                            {
                                using (Stream stream = response.GetResponseStream())
                                {
                                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                                    {
                                        pageSource = reader.ReadToEnd();
                                    }
                                }
                            }
                        }
                        else
                        {
                            listThreads.Remove(Thread.CurrentThread);
                            return;
                        }
                    }
                    request.Abort();
                    watch.Stop();
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    var milliseconds = watch.ElapsedMilliseconds;

                    if (this.OnCompleted != null) this.OnCompleted(this, new OnCompletedEventArgs(uri, threadId, milliseconds, pageSource));
                }
                catch (Exception ex)
                {
                    listThreads.Remove(Thread.CurrentThread);
                    if (this.OnError != null) this.OnError(this, new OnErrorEventArgs(uri, ex));
                }
            });
            t.Start();
            return t;
        
        }
        private Thread Crawling(CancellationToken cancellationToken)
        {
            Thread t = new Thread(()=> 
            {
                while (true)
                {
                    for(int i = 0; i < listRawLinks.Count; i++)
                    {
                        if (listThreads.Count < CountThreads)
                        {
                            while (listRawLinks[i]==null)
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(1));
                            }
                            listLinks.Add(new UrlWithTiming(listRawLinks[i], 0));
                            listThreads.Add(GetPageSource(new Uri(listRawLinks[i])));
                            listRawLinks.Remove(listRawLinks[i]);
                            i--;
                        }
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        foreach(var x in listLinks.OrderBy(x => x.Timing).ToList())
                        {
                            Console.WriteLine(x);
                        }
                        Console.WriteLine("Ended");
                        return;
                    }
                }
            });
            t.Start();
            return t;
        }
        public void Start()
        {
            tokenSource = new CancellationTokenSource();
            Crawling(tokenSource.Token);
            listRawLinks.Add(url);
        }
        
    }
}
