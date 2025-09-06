namespace SudokuGame.Models
{
    public class GameState
    {
        // Lưu lại đề bài gốc để phân biệt số của máy và số của người chơi
        public int[,]? InitialPuzzle { get; set; }

        // Lưu lại trạng thái hiện tại của bàn cờ
        public int[,]? CurrentBoard { get; set; }

        // Lưu lại số giây đã trôi qua
        public int SecondsElapsed { get; set; }
    }
}