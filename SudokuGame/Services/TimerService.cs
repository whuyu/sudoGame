using System;
using System.Diagnostics;

namespace SudokuGame.Services
{
    public class TimerService
    {
        private readonly Stopwatch _stopwatch;
        private TimeSpan _initialTime;

        public TimerService()
        {
            _stopwatch = new Stopwatch();
            _initialTime = TimeSpan.Zero;
        }

        public bool IsRunning => _stopwatch.IsRunning;

        public TimeSpan ElapsedTime => _stopwatch.Elapsed + _initialTime;

        public void Start(TimeSpan? initialTime = null)
        {
            if (initialTime.HasValue)
            {
                _initialTime = initialTime.Value;
            }
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Pause()
        {
            _stopwatch.Stop();
        }

        public void Resume()
        {
            _stopwatch.Start();
        }

        public void Reset()
        {
            _stopwatch.Reset();
            _initialTime = TimeSpan.Zero;
        }

        public string GetFormattedTime()
        {
            var time = ElapsedTime;
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
    }
} 