using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Collections.Generic;
using BCrypt.Net;
using SudokuGame.Models;
using System.Threading.Tasks;

namespace SudokuGame.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly MySqlConnection _connection;

        public DatabaseService()
        {
            _connectionString = "Server=localhost;Uid=root;Pwd=20234108@123;Allow User Variables=True;";
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
                        role VARCHAR(20) DEFAULT 'user' NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var cmd = new MySqlCommand(createUsersTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }

//////运行一遍有了role项和管理员账号之后可以把这一部分删了
                string checkAdminQuery = "SELECT COUNT(*) FROM users WHERE username = 'admin'";
                using (var cmd = new MySqlCommand(checkAdminQuery, _connection))
                {
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        // 创建管理员用户
                        string adminPassword = BCrypt.Net.BCrypt.HashPassword("1");
                        string createAdminQuery = @"
                            INSERT INTO users (username, password_hash, role) 
                            VALUES ('admin', @passwordHash, 'admin')";
                        
                        using (var adminCmd = new MySqlCommand(createAdminQuery, _connection))
                        {
                            adminCmd.Parameters.AddWithValue("@passwordHash", adminPassword);
                            adminCmd.ExecuteNonQuery();
                        }
                    }
                }

                // 检查并添加role列（如果不存在）
                string checkRoleColumnQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.COLUMNS 
                    WHERE TABLE_SCHEMA = 'sudoku_game' 
                    AND TABLE_NAME = 'users' 
                    AND COLUMN_NAME = 'role'";

                using (var cmd = new MySqlCommand(checkRoleColumnQuery, _connection))
                {
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    {
                        // role列不存在，添加它
                        string addRoleColumnQuery = @"
                            ALTER TABLE users 
                            ADD COLUMN role VARCHAR(20) DEFAULT 'user' NOT NULL";

                        using (var alterCmd = new MySqlCommand(addRoleColumnQuery, _connection))
                        {
                            alterCmd.ExecuteNonQuery();
                        }
                    }
                }
/////////////删到这里
                // 创建sudoku_puzzles表
                string createPuzzlesTableQuery = @"
                    CREATE TABLE IF NOT EXISTS sudoku_puzzles (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        initial_board VARCHAR(81) NOT NULL,
                        solution VARCHAR(81) NOT NULL,
                        difficulty VARCHAR(10) NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var cmd = new MySqlCommand(createPuzzlesTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 创建user_puzzles关系表
                string createUserPuzzlesTableQuery = @"
                    CREATE TABLE IF NOT EXISTS user_puzzles (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        user_id INT NOT NULL,
                        puzzle_id INT NOT NULL,
                        current_board VARCHAR(81) NOT NULL,
                        last_played_at TIMESTAMP NULL,
                        total_play_time INT DEFAULT 0,
                        is_completed BOOLEAN DEFAULT FALSE,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                        FOREIGN KEY (puzzle_id) REFERENCES sudoku_puzzles(id) ON DELETE CASCADE,
                        UNIQUE KEY unique_user_puzzle (user_id, puzzle_id)
                    );";

                using (var cmd = new MySqlCommand(createUserPuzzlesTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 创建比赛表
                string createContestsTableQuery = @"
                    CREATE TABLE IF NOT EXISTS contests (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        title VARCHAR(100) NOT NULL,
                        description TEXT,
                        start_time DATETIME NOT NULL,
                        duration INT NOT NULL COMMENT '比赛时长（分钟）',
                        status VARCHAR(20) DEFAULT 'pending' COMMENT 'pending, ongoing, finished',
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var cmd = new MySqlCommand(createContestsTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 创建比赛题目表
                string createContestPuzzlesTableQuery = @"
                    CREATE TABLE IF NOT EXISTS contest_puzzles (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        contest_id INT NOT NULL,
                        puzzle_id INT NOT NULL,
                        order_index INT NOT NULL,
                        FOREIGN KEY (contest_id) REFERENCES contests(id) ON DELETE CASCADE,
                        FOREIGN KEY (puzzle_id) REFERENCES sudoku_puzzles(id) ON DELETE CASCADE
                    );";

                using (var cmd = new MySqlCommand(createContestPuzzlesTableQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }

                // 创建比赛参与记录表
                string createContestParticipantsTableQuery = @"
                    CREATE TABLE IF NOT EXISTS contest_participants (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        contest_id INT NOT NULL,
                        user_id INT NOT NULL,
                        join_time DATETIME NOT NULL,
                        completed_puzzles INT DEFAULT 0,
                        total_time INT DEFAULT 0 COMMENT '总用时（秒）',
                        FOREIGN KEY (contest_id) REFERENCES contests(id) ON DELETE CASCADE,
                        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
                    );";

                using (var cmd = new MySqlCommand(createContestParticipantsTableQuery, _connection))
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
                SELECT p.id, p.initial_board, p.solution, p.difficulty, p.created_at,
                       up.current_board, up.last_played_at, up.total_play_time, up.is_completed
                FROM sudoku_puzzles p
                INNER JOIN user_puzzles up ON p.id = up.puzzle_id
                WHERE up.user_id = @userId 
                ORDER BY up.created_at DESC";

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

        public void SavePuzzle(SudokuPuzzle puzzle, int userId)
        {
            string puzzleQuery = @"
                INSERT INTO sudoku_puzzles (
                    initial_board, solution, difficulty
                ) VALUES (
                    @initialBoard, @solution, @difficulty
                )";

            using (var cmd = new MySqlCommand(puzzleQuery, _connection))
            {
                cmd.Parameters.AddWithValue("@initialBoard", puzzle.InitialBoard);
                cmd.Parameters.AddWithValue("@solution", puzzle.Solution);
                cmd.Parameters.AddWithValue("@difficulty", puzzle.Difficulty);
                cmd.ExecuteNonQuery();
                
                // 获取自动生成的ID
                puzzle.Id = (int)cmd.LastInsertedId;
            }

            // 创建用户-题目关系
            string userPuzzleQuery = @"
                INSERT INTO user_puzzles (
                    user_id, puzzle_id, current_board,
                    last_played_at, total_play_time, is_completed
                ) VALUES (
                    @userId, @puzzleId, @currentBoard,
                    @lastPlayedAt, @totalPlayTime, @isCompleted
                )";

            using (var cmd = new MySqlCommand(userPuzzleQuery, _connection))
            {
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@puzzleId", puzzle.Id);
                cmd.Parameters.AddWithValue("@currentBoard", puzzle.CurrentBoard);
                cmd.Parameters.AddWithValue("@lastPlayedAt", puzzle.LastPlayedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@totalPlayTime", (int)puzzle.TotalPlayTime.TotalSeconds);
                cmd.Parameters.AddWithValue("@isCompleted", puzzle.IsCompleted);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdatePuzzle(SudokuPuzzle puzzle, int userId)
        {
            string query = @"
                UPDATE user_puzzles 
                SET current_board = @currentBoard,
                    last_played_at = @lastPlayedAt,
                    total_play_time = @totalPlayTime,
                    is_completed = @isCompleted
                WHERE puzzle_id = @puzzleId AND user_id = @userId";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@puzzleId", puzzle.Id);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@currentBoard", puzzle.CurrentBoard);
                cmd.Parameters.AddWithValue("@lastPlayedAt", puzzle.LastPlayedAt ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@totalPlayTime", (int)puzzle.TotalPlayTime.TotalSeconds);
                cmd.Parameters.AddWithValue("@isCompleted", puzzle.IsCompleted);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeletePuzzle(int puzzleId, int userId)
        {
            // 首先删除用户-题目关系
            string deleteUserPuzzleQuery = "DELETE FROM user_puzzles WHERE puzzle_id = @puzzleId AND user_id = @userId";
            using (var cmd = new MySqlCommand(deleteUserPuzzleQuery, _connection))
            {
                cmd.Parameters.AddWithValue("@puzzleId", puzzleId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }

            // 检查是否还有其他用户收藏了这个题目
            string checkQuery = "SELECT COUNT(*) FROM user_puzzles WHERE puzzle_id = @puzzleId";
            using (var cmd = new MySqlCommand(checkQuery, _connection))
            {
                cmd.Parameters.AddWithValue("@puzzleId", puzzleId);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                
                // 如果没有其他用户收藏，则删除题目
                if (count == 0)
                {
                    string deletePuzzleQuery = "DELETE FROM sudoku_puzzles WHERE id = @puzzleId";
                    using (var deleteCmd = new MySqlCommand(deletePuzzleQuery, _connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@puzzleId", puzzleId);
                        deleteCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public (bool success, string message, int userId) ValidateUser(string username, string password)
        {
            try
            {
                using (var cmd = new MySqlCommand("SELECT id, password_hash, role FROM users WHERE username = @username", _connection))
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
                        string role = reader.GetString("role");
                        bool verified = BCrypt.Net.BCrypt.Verify(password, storedHash);

                        return verified ? (true, $"登录成功 - 角色：{role}", userId) : (false, "用户名或密码错误", 0);
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

        // 获取所有比赛列表
        public List<Contest> GetContests()
        {
            var contests = new List<Contest>();
            string query = @"
                SELECT * FROM contests 
                ORDER BY start_time DESC";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        contests.Add(new Contest
                        {
                            Id = reader.GetInt32("id"),
                            Title = reader.GetString("title"),
                            Description = reader.GetString("description"),
                            StartTime = reader.GetDateTime("start_time"),
                            Duration = reader.GetInt32("duration"),
                            Status = reader.GetString("status"),
                            CreatedAt = reader.GetDateTime("created_at")
                        });
                    }
                }
            }
            return contests;
        }

        // 获取比赛详情
        public Contest GetContest(int contestId)
        {
            string query = "SELECT * FROM contests WHERE id = @contestId";
            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@contestId", contestId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Contest
                        {
                            Id = reader.GetInt32("id"),
                            Title = reader.GetString("title"),
                            Description = reader.GetString("description"),
                            StartTime = reader.GetDateTime("start_time"),
                            Duration = reader.GetInt32("duration"),
                            Status = reader.GetString("status"),
                            CreatedAt = reader.GetDateTime("created_at")
                        };
                    }
                }
            }
            return null;
        }

        // 获取比赛题目列表
        public async Task<List<SudokuPuzzle>> GetContestPuzzles(int contestId)
        {
            var puzzles = new List<SudokuPuzzle>();
            string query = @"
                SELECT p.id, p.initial_board, p.solution, p.difficulty, p.created_at
                FROM sudoku_puzzles p
                INNER JOIN contest_puzzles cp ON p.id = cp.puzzle_id
                WHERE cp.contest_id = @contestId
                ORDER BY cp.order_index";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@contestId", contestId);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        puzzles.Add(new SudokuPuzzle
                        {
                            Id = reader.GetInt32("id"),
                            InitialBoard = reader.GetString("initial_board"),
                            CurrentBoard = reader.GetString("initial_board"), // 比赛题目初始状态
                            Solution = reader.GetString("solution"),
                            Difficulty = reader.GetString("difficulty"),
                            CreatedAt = reader.GetDateTime("created_at"),
                            LastPlayedAt = null,
                            TotalPlayTime = TimeSpan.Zero,
                            IsCompleted = false
                        });
                    }
                }
            }
            return puzzles;
        }

        // 加入比赛
        public (bool success, string message) JoinContest(int contestId, int userId)
        {
            try
            {
                // 检查比赛状态
                var contest = GetContest(contestId);
                if (contest == null)
                {
                    return (false, "比赛不存在");
                }

                if (contest.Status == "finished")
                {
                    return (false, "比赛已结束");
                }

                if (DateTime.Now < contest.StartTime)
                {
                    return (false, "比赛还未开始");
                }

                if (DateTime.Now > contest.StartTime.AddMinutes(contest.Duration))
                {
                    return (false, "比赛已结束");
                }

                // 检查是否已经加入
                string checkQuery = @"
                    SELECT COUNT(*) FROM contest_participants 
                    WHERE contest_id = @contestId AND user_id = @userId";

                using (var cmd = new MySqlCommand(checkQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@contestId", contestId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    long count = (long)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        // 如果已经加入，直接返回成功
                        return (true, "成功加入比赛");
                    }
                }

                // 加入比赛
                string insertQuery = @"
                    INSERT INTO contest_participants (contest_id, user_id, join_time)
                    VALUES (@contestId, @userId, @joinTime)";

                using (var cmd = new MySqlCommand(insertQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@contestId", contestId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@joinTime", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }

                return (true, "成功加入比赛");
            }
            catch (Exception ex)
            {
                return (false, $"加入比赛失败: {ex.Message}");
            }
        }

        // 更新比赛参与者的完成情况
        public void UpdateContestParticipant(int contestId, int userId, int completedPuzzles, int totalTime)
        {
            string query = @"
                UPDATE contest_participants 
                SET completed_puzzles = @completedPuzzles,
                    total_time = @totalTime
                WHERE contest_id = @contestId AND user_id = @userId";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@contestId", contestId);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@completedPuzzles", completedPuzzles);
                cmd.Parameters.AddWithValue("@totalTime", totalTime);
                cmd.ExecuteNonQuery();
            }
        }

        // 获取比赛排行榜
        public List<ContestParticipant> GetContestLeaderboard(int contestId)
        {
            var participants = new List<ContestParticipant>();
            string query = @"
                SELECT cp.*, u.username 
                FROM contest_participants cp
                INNER JOIN users u ON cp.user_id = u.id
                WHERE cp.contest_id = @contestId
                ORDER BY cp.completed_puzzles DESC, cp.total_time ASC";

            using (var cmd = new MySqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@contestId", contestId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        participants.Add(new ContestParticipant
                        {
                            Id = reader.GetInt32("id"),
                            ContestId = reader.GetInt32("contest_id"),
                            UserId = reader.GetInt32("user_id"),
                            Username = reader.GetString("username"),
                            JoinTime = reader.GetDateTime("join_time"),
                            CompletedPuzzles = reader.GetInt32("completed_puzzles"),
                            TotalTime = reader.GetInt32("total_time")
                        });
                    }
                }
            }
            return participants;
        }

        public string GetUserRole(int userId)
        {
            try
            {
                using (var cmd = new MySqlCommand("SELECT role FROM users WHERE id = @userId", _connection))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "user";
                }
            }
            catch (Exception)
            {
                return "user"; // 如果发生错误，默认返回普通用户角色
            }
        }
        
        // 检查用户是否已加入比赛
        public bool HasUserJoinedContest(int contestId, int userId)
        {
            try
            {
                string query = @"
                    SELECT COUNT(*) FROM contest_participants 
                    WHERE contest_id = @contestId AND user_id = @userId";

                using (var cmd = new MySqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@contestId", contestId);
                    cmd.Parameters.AddWithValue("@userId", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查用户参赛状态时出错: {ex.Message}");
                return false;
            }
        }
    }
} 