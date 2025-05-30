using System;

namespace SudokuGame.Services
{
    public class SudokuGenerator
    {
        private readonly Random random = new();

        public (string initialBoard, string solution) GeneratePuzzle(string difficulty)
        {
            int[,] solution = new int[9, 9];
            int[,] puzzle = new int[9, 9];

            // 生成完整的解决方案
            GenerateSolution(solution);

            // 复制解决方案到谜题
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    puzzle[i, j] = solution[i, j];

            // 根据难度移除数字
            int cellsToRemove = difficulty switch
            {
                "简单" => 30,
                "困难" => 50,
                _ => 40 // 普通难度
            };

            // 随机移除数字
            while (cellsToRemove > 0)
            {
                int row = random.Next(9);
                int col = random.Next(9);
                if (puzzle[row, col] != 0)
                {
                    puzzle[row, col] = 0;
                    cellsToRemove--;
                }
            }

            return (BoardToString(puzzle), BoardToString(solution));
        }

        private bool GenerateSolution(int[,] board)
        {
            int row = -1, col = -1;
            bool isEmpty = false;

            // 找到一个空位置
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (board[i, j] == 0)
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

            // 如果没有空位置，说明已经完成
            if (!isEmpty)
                return true;

            // 尝试填入1-9
            var numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Shuffle(numbers);

            foreach (int num in numbers)
            {
                if (IsSafe(board, row, col, num))
                {
                    board[row, col] = num;
                    if (GenerateSolution(board))
                        return true;
                    board[row, col] = 0;
                }
            }
            return false;
        }

        private bool IsSafe(int[,] board, int row, int col, int num)
        {
            // 检查行
            for (int x = 0; x < 9; x++)
                if (board[row, x] == num)
                    return false;

            // 检查列
            for (int x = 0; x < 9; x++)
                if (board[x, col] == num)
                    return false;

            // 检查3x3方块
            int startRow = row - row % 3, startCol = col - col % 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[i + startRow, j + startCol] == num)
                        return false;

            return true;
        }

        private void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T temp = array[k];
                array[k] = array[n];
                array[n] = temp;
            }
        }

        private string BoardToString(int[,] board)
        {
            var result = new char[81];
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    result[i * 9 + j] = board[i, j].ToString()[0];
            return new string(result);
        }
    }
} 