using System;

namespace SudokuGame.Models
{
    public class SudokuPuzzle
    {
        public int Id { get; set; }
        public required string InitialBoard { get; set; }
        public required string Solution { get; set; }
        public required string Difficulty { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 