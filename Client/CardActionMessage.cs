namespace MemoryGame.Client
{

    public class ActionMessage
    {
        public ActionType ActionType { get; set; }
        public int ClickedCardIndex { get; set; }
        public string? Name { get; set; }
        public List<ChatContent>? ChatMessage { get; set; }
    }
    public enum ActionType
    {
        Register,
        StarteGame,
        GetNextAction,
        FirstCard,
        SecondCard,
        NoMatch,
        MatchFound,
        NewGame,
        ReadyForNewGame,
        NoNewGame,
        Ciao,
        Chat
    }
    public class ChatContent
    {
        public string? Type { get; set; } // "Text" oder "Image"
        public string? Content { get; set; } // Der Inhalt 
    }
}
