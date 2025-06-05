using Xunit;
using SudokuGame.Services;
using SudokuGame.Models;
using System;
using System.Linq;
using System.IO;

namespace SudokuGame.Tests
{
    public class DatabaseServiceTests : IDisposable
    {
        private readonly DatabaseService _databaseService;
        private readonly string _testDbPath;

        public DatabaseServiceTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_sudoku_{Guid.NewGuid()}.db");
            _databaseService = new DatabaseService(_testDbPath);
        }

        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch (IOException)
                {
                    // 如果文件仍被占用，我们可以忽略这个错误
                    // 文件会在下次测试运行时被新的GUID替代
                }
            }
        }

        [Fact]
        public void SaveAndLoadPuzzle_ShouldWork()
        {
            // Arrange
            var puzzle = new SudokuPuzzle
            {
                InitialBoard = "530070000600195000098000060800060003400803001700020006060000280000419005000080079",
                Solution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                Difficulty = "普通",
                CreatedAt = DateTime.Now
            };
            int userId = 1;

            // Act
            _databaseService.SavePuzzle(puzzle, userId);
            var loadedPuzzle = _databaseService.LoadPuzzle(puzzle.Id);

            // Assert
            Assert.NotNull(loadedPuzzle);
            Assert.Equal(puzzle.InitialBoard, loadedPuzzle.InitialBoard);
            Assert.Equal(puzzle.Solution, loadedPuzzle.Solution);
            Assert.Equal(puzzle.Difficulty, loadedPuzzle.Difficulty);
        }

        [Fact]
        public void FavoritePuzzle_ShouldWork()
        {
            // Arrange
            var puzzle = new SudokuPuzzle
            {
                InitialBoard = "530070000600195000098000060800060003400803001700020006060000280000419005000080079",
                Solution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                Difficulty = "普通",
                CreatedAt = DateTime.Now
            };
            int userId = 1;

            // Act
            _databaseService.SavePuzzle(puzzle, userId);
            _databaseService.FavoritePuzzle(userId, puzzle.Id);

            // Assert
            Assert.True(_databaseService.HasUserFavoritedPuzzle(userId, puzzle.Id));
        }

        [Fact]
        public void UnfavoritePuzzle_ShouldWork()
        {
            // Arrange
            var puzzle = new SudokuPuzzle
            {
                InitialBoard = "530070000600195000098000060800060003400803001700020006060000280000419005000080079",
                Solution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                Difficulty = "普通",
                CreatedAt = DateTime.Now
            };
            int userId = 1;

            // Act
            _databaseService.SavePuzzle(puzzle, userId);
            _databaseService.FavoritePuzzle(userId, puzzle.Id);
            _databaseService.UnfavoritePuzzle(userId, puzzle.Id);

            // Assert
            Assert.False(_databaseService.HasUserFavoritedPuzzle(userId, puzzle.Id));
        }

        [Fact]
        public void GetFavoritePuzzles_ShouldReturnCorrectPuzzles()
        {
            // Arrange
            var puzzle1 = new SudokuPuzzle
            {
                InitialBoard = "530070000600195000098000060800060003400803001700020006060000280000419005000080079",
                Solution = "534678912672195348198342567859761423426853791713924856961537284287419635345286179",
                Difficulty = "普通",
                CreatedAt = DateTime.Now
            };
            var puzzle2 = new SudokuPuzzle
            {
                InitialBoard = "100007090030020008009600500005300900010080002600004000300000010040000007007000300",
                Solution = "162857493534921678789643521475312968913586742628794135356478219241935687897162354",
                Difficulty = "简单",
                CreatedAt = DateTime.Now
            };
            int userId = 1;

            // Act
            _databaseService.SavePuzzle(puzzle1, userId);
            _databaseService.SavePuzzle(puzzle2, userId);
            _databaseService.FavoritePuzzle(userId, puzzle1.Id);
            _databaseService.FavoritePuzzle(userId, puzzle2.Id);

            var favoritePuzzles = _databaseService.GetFavoritePuzzles(userId).ToList();

            // Assert
            Assert.Equal(2, favoritePuzzles.Count);
            Assert.Contains(favoritePuzzles, p => p.InitialBoard == puzzle1.InitialBoard);
            Assert.Contains(favoritePuzzles, p => p.InitialBoard == puzzle2.InitialBoard);
        }
    }
} 