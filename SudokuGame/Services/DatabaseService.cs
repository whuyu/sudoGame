using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using SudokuGame.Models;

namespace SudokuGame.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            if (!File.Exists(_connectionString.Split('=')[1].Split(';')[0]))
            {
                SQLiteConnection.CreateFile(_connectionString.Split('=')[1].Split(';')[0]);
            }

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Puzzles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InitialBoard TEXT NOT NULL,
                    Solution TEXT NOT NULL,
                    Difficulty TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS FavoritePuzzles (
                    UserId INTEGER,
                    PuzzleId INTEGER,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (UserId, PuzzleId),
                    FOREIGN KEY (PuzzleId) REFERENCES Puzzles(Id)
                );";
            command.ExecuteNonQuery();
        }

        public void SavePuzzle(SudokuPuzzle puzzle, int userId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Puzzles (InitialBoard, Solution, Difficulty)
                VALUES (@InitialBoard, @Solution, @Difficulty);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@InitialBoard", puzzle.InitialBoard);
            command.Parameters.AddWithValue("@Solution", puzzle.Solution);
            command.Parameters.AddWithValue("@Difficulty", puzzle.Difficulty);

            puzzle.Id = Convert.ToInt32(command.ExecuteScalar());
        }

        public SudokuPuzzle? LoadPuzzle(int puzzleId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Puzzles WHERE Id = @PuzzleId";
            command.Parameters.AddWithValue("@PuzzleId", puzzleId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new SudokuPuzzle
                {
                    Id = reader.GetInt32(0),
                    InitialBoard = reader.GetString(1),
                    Solution = reader.GetString(2),
                    Difficulty = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                };
            }

            return null;
        }

        public void FavoritePuzzle(int userId, int puzzleId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO FavoritePuzzles (UserId, PuzzleId)
                VALUES (@UserId, @PuzzleId)";

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PuzzleId", puzzleId);

            command.ExecuteNonQuery();
        }

        public void UnfavoritePuzzle(int userId, int puzzleId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM FavoritePuzzles WHERE UserId = @UserId AND PuzzleId = @PuzzleId";

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PuzzleId", puzzleId);

            command.ExecuteNonQuery();
        }

        public bool HasUserFavoritedPuzzle(int userId, int puzzleId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM FavoritePuzzles WHERE UserId = @UserId AND PuzzleId = @PuzzleId";

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@PuzzleId", puzzleId);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        public IEnumerable<SudokuPuzzle> GetFavoritePuzzles(int userId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT p.* 
                FROM Puzzles p
                INNER JOIN FavoritePuzzles f ON p.Id = f.PuzzleId
                WHERE f.UserId = @UserId
                ORDER BY f.CreatedAt DESC";

            command.Parameters.AddWithValue("@UserId", userId);

            var puzzles = new List<SudokuPuzzle>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                puzzles.Add(new SudokuPuzzle
                {
                    Id = reader.GetInt32(0),
                    InitialBoard = reader.GetString(1),
                    Solution = reader.GetString(2),
                    Difficulty = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }

            return puzzles;
        }
    }
} 