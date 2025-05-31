using System;

namespace SudokuGame.Models
{
    public class Contest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ContestParticipant
    {
        public int Id { get; set; }
        public int ContestId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public DateTime JoinTime { get; set; }
        public int CompletedPuzzles { get; set; }
        public int TotalTime { get; set; }
    }
} 