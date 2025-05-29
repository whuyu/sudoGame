using MySql.Data.MySqlClient;
using System;
using System.Data;
using BCrypt.Net;

namespace SudokuGame.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;
        public bool IsInitialized { get; private set; }

        public DatabaseService()
        {
            // 基础连接字符串（不指定数据库）
            string serverConnectionString = "Server=localhost;Uid=root;Pwd=20234108@123;";
            connectionString = $"{serverConnectionString};Database=sudoku_game;";
            
            try
            {
                InitializeDatabase();
                IsInitialized = true;
                Console.WriteLine("数据库初始化成功");
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                Console.WriteLine($"数据库初始化失败: {ex.Message}");
                throw; // 重新抛出异常，让调用者知道初始化失败
            }
        }

        private void InitializeDatabase()
        {
            Console.WriteLine("开始初始化数据库...");
            
            // 第一步：创建数据库（如果不存在）
            using (var serverConnection = new MySqlConnection("Server=localhost;Uid=root;Pwd=20234108@123;"))
            {
                serverConnection.Open();
                Console.WriteLine("已连接到 MySQL 服务器");
                
                using (var createDbCommand = new MySqlCommand(
                    "CREATE DATABASE IF NOT EXISTS sudoku_game", 
                    serverConnection))
                {
                    createDbCommand.ExecuteNonQuery();
                    Console.WriteLine("数据库 sudoku_game 已创建或已存在");
                }
            }

            // 第二步：连接到 sudoku_game 并创建表
            using (var dbConnection = new MySqlConnection(connectionString))
            {
                dbConnection.Open();
                Console.WriteLine("已连接到 sudoku_game 数据库");
                
                // 检查 users 表是否存在
                using (var checkTableCommand = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'sudoku_game' AND table_name = 'users'", 
                    dbConnection))
                {
                    var tableExists = Convert.ToInt32(checkTableCommand.ExecuteScalar()) > 0;
                    Console.WriteLine($"Users 表状态: {(tableExists ? "已存在" : "不存在")}");
                }

                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS users (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            username VARCHAR(50) UNIQUE NOT NULL,
                            password VARCHAR(255) NOT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
                    command.ExecuteNonQuery();
                    Console.WriteLine("Users 表已创建或已存在");
                }
            }
        }

        public (bool success, string message) RegisterUser(string username, string password)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO users (username, password) VALUES (@username, @password)";
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(password));
                        command.ExecuteNonQuery();
                        return (true, "注册成功");
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // 唯一性约束错误
                {
                    return (false, "用户名已存在");
                }
                return (false, $"数据库错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"未知错误: {ex.Message}");
            }
        }

        public (bool success, string message) ValidateUser(string username, string password)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT password FROM users WHERE username = @username";
                        command.Parameters.AddWithValue("@username", username);
                        var result = command.ExecuteScalar();
                        
                        if (result != null)
                        {
                            bool isValid = BCrypt.Net.BCrypt.Verify(password, result.ToString());
                            return (isValid, isValid ? "登录成功" : "密码错误");
                        }
                        return (false, "用户不存在");
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"验证失败: {ex.Message}");
            }
        }
    }
} 