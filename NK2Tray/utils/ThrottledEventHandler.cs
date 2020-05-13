using System;

namespace NK2Tray.utils
{
    class ThrottledEventHandler<TArgs> where TArgs : EventArgs
    {
        private readonly EventHandler<TArgs> _innerHandler;
        private readonly EventHandler<TArgs> _outerHandler;
        private readonly System.Timers.Timer _throttleTimer;

        private readonly object _throttleLock = new object();
        private Action _delayedHandler = null;

        public ThrottledEventHandler(EventHandler<TArgs> handler, TimeSpan delay)
        {
            _innerHandler = handler;
            _outerHandler = HandleIncomingEvent;
            _throttleTimer = new System.Timers.Timer(delay.TotalMilliseconds);
            _throttleTimer.Elapsed += Timer_Tick;
        }

        private void HandleIncomingEvent(object sender, TArgs args)
        {
            lock (_throttleLock)
            {
                if (_throttleTimer.Enabled)
                {
                    _delayedHandler = () => SendEventToHandler(sender, args);
                }
                else
                {
                    SendEventToHandler(sender, args);
                }
            }
        }

        private void SendEventToHandler(object sender, TArgs args)
        {
            if (_innerHandler != null)
            {
                _innerHandler(sender, args);
                _throttleTimer.Start();
            }
        }

        private void Timer_Tick(object sender, EventArgs args)
        {
            lock (_throttleLock)
            {
                _throttleTimer.Stop();

                if (_delayedHandler != null)
                {
                    _delayedHandler();
                    _delayedHandler = null;
                }
            }
        }

        public static implicit operator EventHandler<TArgs>(ThrottledEventHandler<TArgs> throttledHandler)
        {
            return throttledHandler._outerHandler;
        }
    }
}
