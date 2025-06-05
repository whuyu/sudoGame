using System;
using System.Collections.Generic;
using System.Linq;

namespace SudokuGame.Services
{
    public class SudokuGenerator
    {
        private readonly Random _random = new Random();

        public (string initialBoard, string solution) GeneratePuzzle(string difficulty)
        {
            var solution = GenerateCompleteSudoku();
            var initialBoard = CreatePuzzleFromSolution(solution, difficulty);
            return (initialBoard, solution);
        }

        private string GenerateCompleteSudoku()
        {
            var grid = new int[9, 9];
            FillGrid(grid);
            return GridToString(grid);
        }

        private bool FillGrid(int[,] grid)
        {
            int row = -1, col = -1;
            bool isEmpty = false;

            // 找到一个空位置
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (grid[i, j] == 0)
                    {
                        row = i;
                        col = j;
                        isEmpty = true;
                        break;
                    }
                }
                if (isEmpty)
                    break;
            }

            // 如果没有空位置，说明已完成
            if (!isEmpty)
                return true;

            // 尝试填入1-9
            var numbers = Enumerable.Range(1, 9).ToList();
            Shuffle(numbers);

            foreach (int num in numbers)
            {
                if (IsSafe(grid, row, col, num))
                {
                    grid[row, col] = num;

                    if (FillGrid(grid))
                        return true;

                    grid[row, col] = 0;
                }
            }
            return false;
        }

        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private bool IsSafe(int[,] grid, int row, int col, int num)
        {
            // 检查行
            for (int x = 0; x < 9; x++)
                if (grid[row, x] == num)
                    return false;

            // 检查列
            for (int x = 0; x < 9; x++)
                if (grid[x, col] == num)
                    return false;

            // 检查3x3方块
            int startRow = row - row % 3, startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (grid[i + startRow, j + startCol] == num)
                        return false;

            return true;
        }

        private string CreatePuzzleFromSolution(string solution, string difficulty)
        {
            var grid = StringToGrid(solution);
            int cellsToRemove = difficulty switch
            {
                "简单" => 35,
                "普通" => 45,
                "困难" => 55,
                _ => 45
            };

            var positions = Enumerable.Range(0, 81).ToList();
            Shuffle(positions);

            for (int i = 0; i < cellsToRemove; i++)
            {
                int pos = positions[i];
                int row = pos / 9;
                int col = pos % 9;
                grid[row, col] = 0;
            }

            return GridToString(grid);
        }

        private string GridToString(int[,] grid)
        {
            var result = new char[81];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    result[i * 9 + j] = grid[i, j].ToString()[0];
            return new string(result);
        }

        private int[,] StringToGrid(string str)
        {
            var grid = new int[9, 9];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    grid[i, j] = str[i * 9 + j] - '0';
            return grid;
        }
    }
} 