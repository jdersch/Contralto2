using System;
using System.Diagnostics;
using System.Threading;


namespace ContraltoUI
{
    /// <summary>
    /// High-Resolution timer for frame-rate throttling; based on a gist at https://gist.github.com/DraTeots/436019368d32007284f8a12f1ba0f545
    /// </remarks>
    public class HighResolutionTimer
    {
        /// <summary>
        /// Tick time length in [ms]
        /// </summary>
        public static readonly double TickLength = 1000f / Stopwatch.Frequency;

        /// <summary>
        /// Tick frequency
        /// </summary>
        public static readonly double Frequency = Stopwatch.Frequency;

        /// <summary>
        /// True if the system/operating system supports HighResolution timer
        /// </summary>
        public static bool IsHighResolution = Stopwatch.IsHighResolution;

        /// <summary>
        /// Indicates whether the timer is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;


        /// <summary>
        /// Creates timer with interval in [ms]
        /// </summary>
        /// <param name="interval">Interval time in [ms]</param>
        public HighResolutionTimer(float interval)
        {
            Interval = interval;
            _event = new AutoResetEvent(false);
        }

        /// <summary>
        /// The interval of a timer in [ms]
        /// </summary>
        public float Interval
        {
            get { return _interval; }
            set
            {
                if (value < 0f || Single.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _interval = value;
            }
        }

        /// <summary>
        /// If true, sets the execution thread to ThreadPriority.Highest
        /// (works after the next Start())
        /// </summary>
        /// <remarks>
        /// It might help in some cases and get things worse in others. 
        /// It suggested that you do some studies if you apply
        /// </remarks>
        public bool UseHighPriorityThread { get; set; } = false;

        /// <summary>
        /// Starts the timer
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            _thread = new Thread(ExecuteTimer)
            {
                IsBackground = true,
            };

            if (UseHighPriorityThread)
            {
                _thread.Priority = ThreadPriority.Highest;
            }
            _thread.Start();
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <remarks>
        /// This function is waiting an executing thread (which do  to stop and join.
        /// </remarks>
        public void Stop(bool joinThread = true)
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            // Even if _thread.Join may take time it is guaranteed that 
            // Elapsed event is never called overlapped with different threads
            if (joinThread && Thread.CurrentThread != _thread)
            {
                _thread.Join();
            }
        }

        /// <summary>
        /// Waits for the timer to tick.
        /// </summary>
        public void Wait()
        {
            _event.WaitOne();
        }

        private void ExecuteTimer()
        {
            float nextTrigger = 0f;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (_isRunning)
            {
                nextTrigger += _interval;
                double elapsed;

                while (true)
                {
                    elapsed = ElapsedHiRes(stopwatch);
                    double diff = nextTrigger - elapsed;
                    if (diff <= 0.0f)
                    {
                        break;
                    }

                    if (diff < 1.0f)
                    {
                        Thread.SpinWait(10);
                    }
                    else if (diff < 5.0f)
                    {
                        Thread.SpinWait(100);
                    }
                    else if (diff < 15.0f)
                    {
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }

                    if (!_isRunning)
                    {
                        return;
                    }
                }

                double delay = elapsed - nextTrigger;
                _event.Set();

                if (!_isRunning)
                {
                    return;
                }

                // restarting the timer in every hour to prevent precision problems
                if (stopwatch.Elapsed.TotalHours >= 1d)
                {
                    stopwatch.Restart();
                    nextTrigger = 0f;
                }
            }

            stopwatch.Stop();
        }

        private static double ElapsedHiRes(Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * TickLength;
        }

        
        private volatile float _interval;
        private volatile bool _isRunning;
        private Thread _thread;
        private AutoResetEvent _event;
    }
}
