using Xunit;
using SudokuGame.Services;
using System;
using System.Threading.Tasks;

namespace SudokuGame.Tests
{
    public class TimerServiceTests
    {
        private readonly TimerService _timerService;

        public TimerServiceTests()
        {
            _timerService = new TimerService();
        }

        [Fact]
        public void StartTimer_ShouldStartFromZero()
        {
            // Act
            _timerService.Start();
            
            // Assert
            Assert.True(_timerService.IsRunning);
            Assert.True(_timerService.ElapsedTime.TotalSeconds >= 0);
        }

        [Fact]
        public void StartTimer_WithInitialTime_ShouldStartFromInitialTime()
        {
            // Arrange
            var initialTime = TimeSpan.FromMinutes(5);

            // Act
            _timerService.Start(initialTime);
            
            // Assert
            Assert.True(_timerService.IsRunning);
            Assert.True(_timerService.ElapsedTime >= initialTime);
        }

        [Fact]
        public async Task StopTimer_ShouldStopTimeTracking()
        {
            // Arrange
            _timerService.Start();
            await Task.Delay(100); // 等待一小段时间

            // Act
            var timeBeforeStop = _timerService.ElapsedTime;
            _timerService.Stop();
            await Task.Delay(100); // 等待一小段时间

            // Assert
            Assert.False(_timerService.IsRunning);
            Assert.Equal(timeBeforeStop.TotalSeconds, _timerService.ElapsedTime.TotalSeconds, 1);
        }

        [Fact]
        public void Reset_ShouldResetToZero()
        {
            // Arrange
            _timerService.Start();
            Task.Delay(100).Wait(); // 等待一小段时间确保计时器有值

            // Act
            _timerService.Reset();

            // Assert
            Assert.False(_timerService.IsRunning);
            Assert.Equal(TimeSpan.Zero, _timerService.ElapsedTime);
        }

        [Fact]
        public async Task PauseResume_ShouldWorkCorrectly()
        {
            // Arrange
            _timerService.Start();
            await Task.Delay(100);

            // Act - Pause
            _timerService.Pause();
            var timeAtPause = _timerService.ElapsedTime;
            await Task.Delay(100);

            // Assert - Pause
            Assert.False(_timerService.IsRunning);
            Assert.Equal(timeAtPause.TotalSeconds, _timerService.ElapsedTime.TotalSeconds, 1);

            // Act - Resume
            _timerService.Resume();

            // Assert - Resume
            Assert.True(_timerService.IsRunning);
            await Task.Delay(100);
            Assert.True(_timerService.ElapsedTime > timeAtPause);
        }

        [Fact]
        public void GetFormattedTime_ShouldReturnCorrectFormat()
        {
            // Arrange
            var testTime = TimeSpan.FromSeconds(3665); // 1 hour, 1 minute, 5 seconds
            _timerService.Start(testTime);

            // Act
            var formattedTime = _timerService.GetFormattedTime();

            // Assert
            Assert.Matches(@"01:01:05", formattedTime);
        }
    }
} 