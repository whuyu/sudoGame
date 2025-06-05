using Xunit;
using SudokuGame.Services;

namespace SudokuGame.Tests
{
    public class SudokuGeneratorTests
    {
        private readonly SudokuGenerator _generator;

        public SudokuGeneratorTests()
        {
            _generator = new SudokuGenerator();
        }

        [Fact]
        public void GeneratePuzzle_ShouldReturnValidPuzzle()
        {
            // Act
            var (initialBoard, solution) = _generator.GeneratePuzzle("普通");

            // Assert
            Assert.Equal(81, initialBoard.Length);
            Assert.Equal(81, solution.Length);
            Assert.Contains('0', initialBoard); // 确保有空格
            Assert.DoesNotContain('0', solution); // 确保解答完整
        }

        [Theory]
        [InlineData("简单")]
        [InlineData("普通")]
        [InlineData("困难")]
        public void GeneratePuzzle_DifferentDifficulties_ShouldReturnValidPuzzles(string difficulty)
        {
            // Act
            var (initialBoard, solution) = _generator.GeneratePuzzle(difficulty);

            // Assert
            Assert.Equal(81, initialBoard.Length);
            Assert.Equal(81, solution.Length);
            Assert.True(IsValidSudoku(solution));
        }

        [Fact]
        public void GeneratePuzzle_SolutionShouldBeValid()
        {
            // Act
            var (initialBoard, solution) = _generator.GeneratePuzzle("普通");

            // Assert
            Assert.True(IsValidSudoku(solution));
            Assert.True(IsSolutionConsistentWithPuzzle(initialBoard, solution));
        }

        private bool IsValidSudoku(string board)
        {
            var grid = new int[9, 9];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    grid[i, j] = board[i * 9 + j] - '0';

            // 检查行
            for (int row = 0; row < 9; row++)
            {
                var used = new bool[10];
                for (int col = 0; col < 9; col++)
                {
                    int num = grid[row, col];
                    if (used[num]) return false;
                    used[num] = true;
                }
            }

            // 检查列
            for (int col = 0; col < 9; col++)
            {
                var used = new bool[10];
                for (int row = 0; row < 9; row++)
                {
                    int num = grid[row, col];
                    if (used[num]) return false;
                    used[num] = true;
                }
            }

            // 检查3x3方块
            for (int block = 0; block < 9; block++)
            {
                var used = new bool[10];
                int rowStart = (block / 3) * 3;
                int colStart = (block % 3) * 3;
                
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int num = grid[rowStart + i, colStart + j];
                        if (used[num]) return false;
                        used[num] = true;
                    }
                }
            }

            return true;
        }

        private bool IsSolutionConsistentWithPuzzle(string puzzle, string solution)
        {
            for (int i = 0; i < 81; i++)
            {
                if (puzzle[i] != '0' && puzzle[i] != solution[i])
                    return false;
            }
            return true;
        }
    }
} 