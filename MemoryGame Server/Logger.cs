namespace MemoryGameServerWPF
{
    public class Logger
    {
        private static Logger _instance;
        public static Logger Instance => _instance ??= new Logger();

        // Delegation oder Aktion für das Loggen
        public Action<string> LogAction { get; set; }

        // Private Konstruktor, um direkte Instanzierung zu verhindern
        private Logger() { }

        public void Log(string message)
        {
            LogAction?.Invoke(message); // Führt die zugewiesene Aktion aus
        }
    }
}
