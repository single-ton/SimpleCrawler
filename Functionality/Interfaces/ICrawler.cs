using Functionality.Events;
using System;
using System.Threading.Tasks;

namespace Functionality.Interfaces
{
    public interface ICrawler
    {
        event EventHandler<OnCompletedEventArgs> OnCompleted;

        event EventHandler<OnErrorEventArgs> OnError;
        void Start(); 
    }
}
