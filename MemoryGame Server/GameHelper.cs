namespace MemoryGameServerWPF
{
    public class GameHelper
    {
        public List<string> FoundPairs = new();  // Liste der gefundenen Paare
        //public int cardsCount = 6;


        public void AddPlayer(GameState _gameState, string name)
        {
            Player newPlayer = new Player();

            newPlayer.Name = name;
            newPlayer.Score = 0;
            newPlayer.Connected = true;

            _gameState.PlayerNames.Add(newPlayer);
        }

        public void InitializeAndShuffleCards(GameState gameState)
        {
            ResetGameData(gameState, FoundPairs);
            Logger.Instance.Log("[Server] Karten werden initialisiert und gemischt...");

            // Erstelle eine Liste der Kartenwerte (1 bis 9) und dupliziere sie    
            var baseCardValues = Enumerable.Range(1, gameState.PairsCount).ToList();
            var allCardValues = new List<int>(baseCardValues.Concat(baseCardValues));
            var random = new Random();

            // Mische die Karten zufällig    
            gameState.Cards = allCardValues
            .OrderBy(_ => random.Next())  // Zufälliges Mischen der Karten    
            .Select((value, index) => new Card
            {
                ID = value.ToString(),
                IsEnabled = true,  // Alle Karten sind zunächst aktiv    
                CardIndex = index,  // Speichert die aktuelle Position der Karte in der Liste    
            }).ToList();


            Logger.Instance.Log("[Server] Karten erfolgreich gemischt.");
        }

        public void WhoStarts(GameState gameState)
        {
            Random random = new Random();
            int zufallsWert = random.Next(gameState.PlayerNames.Count);
            gameState.CurrentPlayerIndex = zufallsWert;
            gameState.CurrentPlayer = gameState.PlayerNames[zufallsWert];
            Logger.Instance.Log($"[Server]: {gameState.CurrentPlayer.Name} beginnt das Spiel.");

        }

        public void SwitchCurrentPlayer(GameState gameState)
        {
            // Kein Paar, Spielerwechsel    
            var currentPlayerIndex = gameState.PlayerNames.IndexOf(gameState.CurrentPlayer);
            var nextPlayerIndex = (currentPlayerIndex + 1) % gameState.PlayerNames.Count;
            gameState.CurrentPlayer = gameState.PlayerNames[nextPlayerIndex];
            Logger.Instance.Log($"Kein Paar, Spielerwechsel.... {gameState.CurrentPlayer.Name} ist jetzt am Zug");

        }
        public void AwardPoint(GameState _gameState)
        {
            // Iteriere über alle Spieler  
            foreach (var player in _gameState.PlayerNames)
            {
                // Überprüfe, ob der aktuelle Spieler der Spieler ist, der den Punkt erhält  
                if (_gameState.CurrentPlayer.Name == player.Name)
                {
                    // Erhöhe den Score des Spielers um 1  
                    player.Score += 1;
                }
            }

            // Setze die Nachricht und gib sie auf der Konsole aus  
            _gameState.Message = $"Pärchen gefunden! {_gameState.CurrentPlayer.Name} erhält 1 Punkt.";
            Logger.Instance.Log(_gameState.Message);
        }

        public string WhoWins(GameState _gameState)
        {
            // Ermitteln des/der Spieler(s) mit dem höchsten Score  
            var sortedPlayers = _gameState.PlayerNames
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Name) // Für den Fall eines Gleichstands, sortiere alphabetisch  
                .ToList();

            string winnersNames;

            // Überprüfen, ob es einen eindeutigen Gewinner gibt oder ob es unentschieden ist  
            if (sortedPlayers.Count > 1 && sortedPlayers[0].Score == sortedPlayers[1].Score)
            {
                // Es gibt mehrere Gewinner (Unentschieden unter den Besten)  
                winnersNames = string.Join(", ", sortedPlayers.TakeWhile(p => p.Score == sortedPlayers[0].Score).Select(p => p.Name));
                var score = sortedPlayers[0].Score;

                // Klares Ausgabeformat für Gleichstände  
                Logger.Instance.Log($"Unentschieden zwischen {winnersNames} mit je {score} Punkten.");
                _gameState.Message = $"Unentschieden zwischen {winnersNames} mit je {score} Punkten.";
            }
            else
            {
                // Es gibt einen eindeutigen Gewinner  
                var winner = sortedPlayers.First();

                winnersNames = winner.Name;
                _gameState.Message = $"{winner.Name} hat das Spiel mit {winner.Score} Pärchen gewonnen";
                Logger.Instance.Log($"{winner.Name} hat das Spiel mit {winner.Score} Pärchen gewonnen");
            }

            return winnersNames;
        }

        private void ResetGameData(GameState gameState, List<string> _foundPairs)
        {
            if (_foundPairs != null)
            {
                _foundPairs.Clear();
            }
            gameState.Message = "";
            // Setze den Score für alle Spieler in der Liste zurück
            foreach (var player in gameState.PlayerNames)
            {
                player.Score = 0;
            }
            Logger.Instance.Log($"Gefundene Pärchenliste und Spielerscores erfolgreich zurückgesetzt!.");
        }
    }

}
