namespace MemoryGame.Client
{
    public class GameState
    {
        public Player CurrentPlayer { get; set; }
        public List<Card>? Cards { get; set; }
        public string? Message { get; set; }
        public Card? PreviousCard { get; set; }
        public List<Player>? PlayerNames { get; set; }
    }
    public class Player
    {
        public string? Name { get; set; }
        public List<object>? UniformGridItems { get; set; } 
        public int? Score { get; set; }
        public bool Connected { get; set; }
    }
}

