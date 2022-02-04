using WebCrawler.Events;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebCrawler.DataStructures;

namespace WebCrawler.Interfaces
{
    public interface ICrawler
    {
        event EventHandler<OnCompletedEventArgs> OnCompleted;

        event EventHandler<OnErrorEventArgs> OnError;
        List<UrlWithTiming> ListResultLinks { get; }
        void Start(int countThreds); 
    }
}
