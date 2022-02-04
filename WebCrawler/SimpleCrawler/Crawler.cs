using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebCrawler.DataStructures;
using WebCrawler.Events;

namespace WebCrawler.SimpleCrawler
{
    public partial class Crawler
    {
        private List<UrlWithTiming> listResultLinks;
        public List<UrlWithTiming> ListResultLinks { get { return listResultLinks; } }
        private List<Uri> listRawLinks;
        private List<Uri> listProcessedLinks;
        private List<Thread> listThreads;
        private string url;
        private string hostContains;
        private string proxy;
        private int CountThreads;
        private CancellationTokenSource tokenSource;
        public bool Ended { get; private set; }

        public event EventHandler<OnCompletedEventArgs> OnCompleted;

        public event EventHandler<OnErrorEventArgs> OnError;

        private Crawler()
        {
            listResultLinks = new List<UrlWithTiming>();
            listRawLinks = new List<Uri>();
            listThreads = new List<Thread>();
            listProcessedLinks = new List<Uri>();
            CountThreads = 10;
            OnCompleted += Crawler_OnCompleted;
            OnError += Crawler_OnError;
        }
        public Crawler(string url, string hostContains = null, string proxy = null):this()
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            this.proxy = proxy;
        }
        private void Crawler_OnError(object sender, OnErrorEventArgs e)
        {
            //Console.WriteLine(e.Uri+" "+ e.Exception.Message);
        }

        private void Crawler_OnCompleted(object sender, OnCompletedEventArgs e)
        {
            listResultLinks.Add(new UrlWithTiming(e.Uri, e.Milliseconds));
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(e.PageSource);
            var nodes = doc.DocumentNode.SelectNodes("//a[@href!='']");
            if (nodes is null)
                return;
            foreach (var node in nodes)
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
                        if (listProcessedLinks.Where(x => x.ToString().Remove(0, x.Scheme.Length + 3).Equals(uri.ToString().Remove(0, uri.Scheme.Length + 3))).Count() == 0 &&
                            listRawLinks.Where(x => x.ToString().Remove(0, x.Scheme.Length + 3).Equals(uri.ToString().Remove(0, uri.Scheme.Length + 3))).Count() == 0 && isNotImage(uri.ToString()))
                        {
                            
                            listRawLinks.Add(uri);
                        }
                    }
                }

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
                    request.AllowAutoRedirect = true;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36";
                    //request.Timeout = 5000;
                    request.KeepAlive = true;
                    request.Method = "GET";
                    if (proxy != null) request.Proxy = new WebProxy(proxy);
                    request.ServicePoint.ConnectionLimit = int.MaxValue;
                    
                    watch.Start();
                    var response = (HttpWebResponse)request.GetResponse();
                    watch.Stop();
                    using (response)
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            if (!response.ResponseUri.Equals(uri))
                                uri = response.ResponseUri; //If a redirect occurred
                            if (response.ContentType.Contains("text/html") && response.ContentEncoding != null)
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
                    var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    var milliseconds = watch.ElapsedMilliseconds;
                    if (this.OnCompleted != null) this.OnCompleted(this, new OnCompletedEventArgs(uri, threadId, milliseconds, pageSource));
                }
                catch(WebException ex)
                {

                    if (ex.Response != null)
                    {
                        if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.Found)
                            ;
                        if (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.BadGateway)
                            ;//I do not know how best to handle such errors in this case
                    }
                    else
                    {
                        //'No connection could be made because the target machine actively refused it. [::ffff:127.0.0.1]:8888 (127.0.0.1:8888)'
                        //"No such host is known. (seva22.blogspot.com:443)" - no internet
                        lock (tokenSource)
                        {
                            if (!tokenSource.Token.IsCancellationRequested)
                            {
                                //listResultLinks.Clear();
                                //Console.WriteLine(ex.Message);
                                tokenSource.Cancel();
                            }
                        }
                    }
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
        private bool isNotImage(string url)
        {
            string expression = @"^.*\.(jpg|gif|png)$";
            return !Regex.IsMatch(url, expression, RegexOptions.IgnoreCase);
        }
        private Thread Monitoring(CancellationToken cancellationToken)
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    for (int i = 0; i < listThreads.Count; i++)
                        if (listThreads[i] != null && listThreads[i].ThreadState == System.Threading.ThreadState.Stopped)
                        {
                            listThreads.RemoveAt(i);
                            i--;
                        }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (listThreads.Count == 0 && listRawLinks.Count == 0)
                    {//end process
                        tokenSource.Cancel();
                    }
                }
            });
            t.Start();
            return t;
        }
        private Thread Crawling(CancellationToken cancellationToken, int countThreads)
        {
            this.CountThreads = countThreads;
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    for (int i = 0; i < listRawLinks.Count; i++)
                    {
                        if (listThreads.Count < CountThreads)
                        {
                            while (listRawLinks[i] == null)
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(1));
                            }
                            listProcessedLinks.Add(listRawLinks[i]);
                            listThreads.Add(GetPageSource(listRawLinks[i]));
                            
                            listRawLinks.Remove(listRawLinks[i]);
                            i--;
                        }
                        else break;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    if (cancellationToken.IsCancellationRequested)
                    {
                        //foreach (var x in listResultLinks.OrderBy(x => x.Timing).ToList())
                        //{
                        //    Console.WriteLine(x);
                        //}
                        //Console.WriteLine("Count: "+listResultLinks.Count().ToString());
                        //Console.WriteLine("Ended");
                        Ended = true;
                        return;
                    }
                }
            });
            t.Start();
            return t;
        }
        public void Start(int countThreds = 100)
        {
            Ended = false;
            //Console.WriteLine("Started");
            tokenSource = new CancellationTokenSource();
            Crawling(tokenSource.Token, countThreds);
            listRawLinks.Add(new Uri(url));
            Monitoring(tokenSource.Token);
        }
    }
}
