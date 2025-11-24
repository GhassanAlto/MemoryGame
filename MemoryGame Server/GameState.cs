namespace MemoryGameServerWPF
{
    public class GameState
    {
        public int RequiredPlayers { get; set; }
        public int PairsCount { get; set; }
        public Player? CurrentPlayer { get; set; }
        public List<Card> Cards { get; set; }
        public string? Message { get; set; }
        public Card? PreviousCard { get; set; }
        public List<Player> PlayerNames { get; set; }
        public int CurrentPlayerIndex { get; set; }

        public GameState()
        {
            Cards = new List<Card>();
            PlayerNames = new List<Player>();
        }
    }
    public class Player
    {
        public string? Name { get; set; }
        public int Score { get; set; }
        public bool Connected { get; set; }

    }
    public class Card
    {
        public string? ID { get; set; }
        public bool IsEnabled { get; set; }
        public int CardIndex { get; set; }
        public bool IsMatched {  get; set; }
    }
}
