using System;

namespace SudokuGame.Models
{
    public class SudokuPuzzle
    {
        public int Id { get; set; }
        public string InitialBoard { get; set; } = string.Empty; // 初始题目状态
        public string CurrentBoard { get; set; } = string.Empty; // 当前填写状态
        public string Solution { get; set; } = string.Empty;     // 正确解答
        public DateTime CreatedAt { get; set; }                  // 创建时间
        public DateTime? LastPlayedAt { get; set; }              // 最后游玩时间
        public TimeSpan TotalPlayTime { get; set; }             // 总游玩时间
        public bool IsCompleted { get; set; }                    // 是否完成
        public int UserId { get; set; }                         // 关联的用户ID
        public string Difficulty { get; set; } = "普通";        // 难度级别
    }
} 