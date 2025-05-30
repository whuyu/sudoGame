using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using BCrypt.Net;
using SudokuGame.Models;

namespace SudokuGame.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly MySqlConnection _connection;

        public DatabaseService()
        {
            _connectionString = "Server=localhost;Uid=root;Pwd=123456;Allow User Variables=True;";
            _connection = new MySqlConnection(_connectionString);
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                _connection.Open();
                
                // 创建数据库（如果不存在）
                using (var cmd = new MySqlCommand("CREATE DATABASE IF NOT EXISTS sudoku_game;", _connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 关闭当前连接
                _connection.Close();

                // 更新连接字符串以包含数据库名称
                _connection.ConnectionString = $"{_connectionString};Database=sudoku_game;";
                _connection.Open();

                // 创建users表
                string createUsersTableQuery = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password_hash VARCHAR(255) NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var cmd = new MySqlCommand(createUsersTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 创建sudoku_puzzles表
                string createPuzzlesTableQuery = @"
                    CREATE TABLE IF NOT EXISTS sudoku_puzzles (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        user_id INT NOT NULL,
                        initial_board VARCHAR(81) NOT NULL,
                        current_board VARCHAR(81) NOT NULL,
                        solution VARCHAR(81) NOT NULL,
                        difficulty VARCHAR(10) NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        last_played_at TIMESTAMP NULL,
                        total_play_time INT DEFAULT 0,
                        is_completed BOOLEAN DEFAULT FALSE,
                        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
                    );";

                using (var cmd = new MySqlCommand(createPuzzlesTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<SudokuPuzzle> GetUserPuzzles(int userId)
        {
            var puzzles = new List<SudokuPuzzle>();
            string query = @"
                SELECT * FROM sudoku_puzzles 
                WHERE user_id = @userId 
                ORDER BY created_at DESC";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@userId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        puzzles.Add(new SudokuPuzzle
                        {
                            Id = reader.GetInt32("id"),
                            UserId = reader.GetInt32("user_id"),
                            InitialBoard = reader.GetString("initial_board"),
                            CurrentBoard = reader.GetString("current_board"),
                            Solution = reader.GetString("solution"),
                            Difficulty = reader.GetString("difficulty"),
                            CreatedAt = reader.GetDateTime("created_at"),
                            LastPlayedAt = reader.IsDBNull(reader.GetOrdinal("last_played_at")) 
                                ? null 
                                : reader.GetDateTime("last_played_at"),
                            TotalPlayTime = TimeSpan.FromSeconds(reader.GetInt32("total_play_time")),
                            IsCompleted = reader.GetBoolean("is_completed")
                        });
                    }
                }
            }
            return puzzles;
        }

        public void SavePuzzle(SudokuPuzzle puzzle)
        {
            string query = @"
                INSERT INTO sudoku_puzzles (
                    user_id, initial_board, current_board, solution, 
                    difficulty, last_played_at, total_play_time, is_completed
                ) VALUES (
                    @userId, @initialBoard, @currentBoard, @solution,
                    @difficulty, @lastPlayedAt, @totalPlayTime, @isCompleted
                )";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@userId", puzzle.UserId);
                cmd.Parameters.AddWithValue("@initialBoard", puzzle.InitialBoard);
                cmd.Parameters.AddWithValue("@currentBoard", puzzle.CurrentBoard);
                cmd.Parameters.AddWithValue("@solution", puzzle.Solution);
                cmd.Parameters.AddWithValue("@difficulty", puzzle.Difficulty);
                cmd.Parameters.AddWithValue("@lastPlayedAt", puzzle.LastPlayedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@totalPlayTime", (int)puzzle.TotalPlayTime.TotalSeconds);
                cmd.Parameters.AddWithValue("@isCompleted", puzzle.IsCompleted);
                cmd.ExecuteNonQuery();
                
                // 获取自动生成的ID
                puzzle.Id = (int)cmd.LastInsertedId;
            }
        }

        public void UpdatePuzzle(SudokuPuzzle puzzle)
        {
            string query = @"
                UPDATE sudoku_puzzles 
                SET current_board = @currentBoard,
                    last_played_at = @lastPlayedAt,
                    total_play_time = @totalPlayTime,
                    is_completed = @isCompleted
                WHERE id = @id AND user_id = @userId";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@id", puzzle.Id);
                cmd.Parameters.AddWithValue("@userId", puzzle.UserId);
                cmd.Parameters.AddWithValue("@currentBoard", puzzle.CurrentBoard);
                cmd.Parameters.AddWithValue("@lastPlayedAt", puzzle.LastPlayedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@totalPlayTime", (int)puzzle.TotalPlayTime.TotalSeconds);
                cmd.Parameters.AddWithValue("@isCompleted", puzzle.IsCompleted);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePuzzle(int puzzleId)
        {
            string query = "DELETE FROM sudoku_puzzles WHERE id = @id";
            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@id", puzzleId);
                cmd.ExecuteNonQuery();
            }
        }

        public (bool success, string message, int userId) ValidateUser(string username, string password)
        {
            try
            {
                using (var cmd = new MySqlCommand("SELECT id, password_hash FROM users WHERE username = @username", _connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return (false, "用户名或密码错误", 0);
                        }

                        int userId = reader.GetInt32("id");
                        string storedHash = reader.GetString("password_hash");
                        bool verified = BCrypt.Net.BCrypt.Verify(password, storedHash);

                        return verified ? (true, "登录成功", userId) : (false, "用户名或密码错误", 0);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"登录失败: {ex.Message}", 0);
            }
        }

        public (bool success, string message, int userId) RegisterUser(string username, string password)
        {
            try
            {
                // 检查用户名是否已存在
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username = @username", _connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    long count = (long)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        return (false, "用户名已存在", 0);
                    }
                }

                // 创建新用户
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                using (var cmd = new MySqlCommand(@"
                    INSERT INTO users (username, password_hash) 
                    VALUES (@username, @password_hash);
                    SELECT LAST_INSERT_ID();", _connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    int userId = Convert.ToInt32(cmd.ExecuteScalar());
                    return (true, "注册成功", userId);
                }
            }
            catch (Exception ex)
            {
                return (false, $"注册失败: {ex.Message}", 0);
            }
        }
    }
} 