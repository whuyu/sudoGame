using MySql.Data.MySqlClient;
using System;
using System.Data;
using BCrypt.Net;

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
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password_hash VARCHAR(255) NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var cmd = new MySqlCommand(createTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public (bool success, string message) ValidateUser(string username, string password)
        {
            try
            {
                using (var cmd = new MySqlCommand("SELECT password_hash FROM users WHERE username = @username", _connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    var result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        return (false, "用户名或密码错误");
                    }

                    string storedHash = result.ToString()!;
                    bool verified = BCrypt.Net.BCrypt.Verify(password, storedHash);

                    return verified ? (true, "登录成功") : (false, "用户名或密码错误");
                }
            }
            catch (Exception ex)
            {
                return (false, $"登录失败: {ex.Message}");
            }
        }

        public (bool success, string message) RegisterUser(string username, string password)
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
                        return (false, "用户名已存在");
                    }
                }

                // 创建新用户
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
                using (var cmd = new MySqlCommand("INSERT INTO users (username, password_hash) VALUES (@username, @password_hash)", _connection))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    cmd.ExecuteNonQuery();
                    return (true, "注册成功");
                }
            }
            catch (Exception ex)
            {
                return (false, $"注册失败: {ex.Message}");
            }
        }
    }
} 