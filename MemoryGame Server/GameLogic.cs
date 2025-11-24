using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MemoryGameServerWPF
{
    public class GameLogic
    {
        private GameState _gameState;
        private GameHelper _gameHelper;
        private HandleAction _handleAction;
        public static readonly ConcurrentDictionary<WebSocket, string> ConnectedClients = new();
        private bool[] _playerReadyStatus;

        public GameLogic(GameState gameState)
        {
            _gameHelper = new GameHelper();
            _handleAction = new HandleAction();
            _gameState = gameState;
        }
        public async Task HandleMessage(WebSocket webSocket, string receivedMessage)
        {
            _gameState.Message = "";
            try
            {
                var message = JsonSerializer.Deserialize<ActionMessage>(receivedMessage);
                await HandleActionMessage(webSocket, message);
                
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"Fehler bei der Verarbeitung der Nachricht: {ex.Message}");
            }
        }
        private async Task HandleActionMessage(WebSocket webSocket, ActionMessage message)
        {
            Logger.Instance.Log($"Aktionstyp: {message.ActionType}");

            switch (message.ActionType)
            {
                case ActionType.Register:
                    await HandleClientConnection(webSocket, message);
                    break;
                case ActionType.GetNextAction:
                    await HandleGetNextAction(message);
                    break;
                case ActionType.ReadyForNewGame:
                    await HandleReadyForNewGame(webSocket, message);
                    break;
                case ActionType.Chat:
                    await SendMessageToAllClientsAsync(message);
                    break;
                case ActionType.NoNewGame:
                    Logger.Instance.Log($"{message.Name} hat das Spiel verlassen");
                    break;
                default:
                    Logger.Instance.Log($"Unbekannter Aktionstyp: {message.ActionType}");
                    break;
            }
        }
        private async Task HandleGetNextAction(ActionMessage message)
        {
            var allowedAction = _handleAction.DetermineNextAction(_gameState, message.ClickedCardIndex);
            Logger.Instance.Log($"AllowedAction: {allowedAction.ActionType}");
            if (allowedAction.ActionType == ActionType.FirstCard)
            {
                _handleAction.FlipCard(message, _gameState);
                Logger.Instance.Log($"Aktion: Karte umdrehen");
                await SendMessageToAllClientsAsync(allowedAction);
                _gameState.PreviousCard = _gameState.Cards[allowedAction.ClickedCardIndex];
            }
            else if (allowedAction.ActionType == ActionType.SecondCard)
            {
                _handleAction.FlipCard(message, _gameState);
                Logger.Instance.Log($"Aktion: Karte umdrehen");
                await SendMessageToAllClientsAsync(allowedAction);
                int z = 1200;
                await Task.Delay(z);
                allowedAction = _handleAction.CheckForMatch(allowedAction, _gameState, _gameHelper);
                Logger.Instance.Log($"AllowedAction: {allowedAction.ActionType}");
                await SendMessageToAllClientsAsync(allowedAction);
                _gameState.PreviousCard = null;
                await CheckForGameOver();
            }
        }
        public async Task HandleReadyForNewGame(WebSocket webSocket, ActionMessage message)
        {
            // Ermitteln des Namens des Clients, der die Nachricht gesendet hat   
            var clientName = ConnectedClients[webSocket];

            // Ermitteln des Index des Spielers in der Liste der Spieler   
            var playerIndex = _gameState.PlayerNames.FindIndex(p => p.Name == clientName);

            // Überprüfen, ob der Spieler gefunden wurde   
            if (playerIndex != -1)
            {
                // wenn die Anzahl der Spieler geändert wurde   
                if (_playerReadyStatus == null || _playerReadyStatus.Length < _gameState.PlayerNames.Count)
                {
                    // Initialisieren der Liste der Spieler-Status   
                    _playerReadyStatus = new bool[_gameState.PlayerNames.Count];
                }

                // Setzen des Status des Spielers auf "bereit"   
                _playerReadyStatus[playerIndex] = true;
                Logger.Instance.Log($"{clientName} ist bereit für ein neues Spiel");

                // Überprüfen, ob alle erforderlichen Spieler bereit sind   
                if (_playerReadyStatus.Take(_gameState.RequiredPlayers).All(status => status))
                {
                    Logger.Instance.Log($"Alle Spieler sisd bereit für ein neues Spiel");

                    // Erstellen einer neuen Nachricht, um das Spiel zu starten   
                    var actionMessage = new ActionMessage
                    {
                        ActionType = ActionType.StarteGame,
                    };

                    // Starten des Spiels   
                    await StartGame(actionMessage);
                    var chatActionMessage = CreateChatMessage();
                    await SendMessageToAllClientsAsync(chatActionMessage);
                    // Zurücksetzen der Liste der Spieler-Status   
                    _playerReadyStatus = new bool[_gameState.PlayerNames.Count];
                }
            }
        }
        public async Task HandleClientConnection(WebSocket webSocket, ActionMessage resivedMessage)
        {
            var clientName = resivedMessage.Name;  // Extrahiere den Spielernamen
            Logger.Instance.Log($"Client verbunden: {clientName}");

            // Füge den Client und seinen Namen zum ConnectedClients-Dictionary hinzu
            _gameHelper.AddPlayer(_gameState, clientName);
            ConnectedClients[webSocket] = clientName;
            //Interlocked.Increment(ref connectedClientCount); // Erhöhe den Zähler der verbundenen Clients    
            Logger.Instance.Log($"{_gameState.RequiredPlayers - ConnectedClients.Count} Spieler fehlen/fehlt noch zum Spielbeginn");

            // Überprüfe, ob genug Spieler verbunden sind, um das Spiel zu starten    
            if (ConnectedClients.Count == _gameState.RequiredPlayers)
            {
                var actionMessage = new ActionMessage
                {
                    ActionType = ActionType.StarteGame,
                };
                await StartGame(actionMessage);
                Logger.Instance.Log("Spiel startet.....");
            }
        }
        private async Task StartGame(ActionMessage actionMessage)
        {
            _gameHelper.InitializeAndShuffleCards(_gameState);
            _gameHelper.WhoStarts(_gameState);
            await SendMessageToAllClientsAsync(actionMessage);

        }
        private async Task SendMessageToAllClientsAsync(ActionMessage actionMessage)
        {
            // Serialisiere die kombinierte Nachricht
            var actionMessageToSend = JsonSerializer.Serialize(actionMessage);
            var gameStateToSend = JsonSerializer.Serialize(_gameState);
            var messageToSend = $"{actionMessageToSend}|{gameStateToSend}";
            Logger.Instance.Log($"[Server] AktionType: {actionMessage.ActionType}");

            foreach (var client in ConnectedClients)
            {
                try
                {
                    var buffer = Encoding.UTF8.GetBytes(messageToSend);
                    await client.Key.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    Logger.Instance.Log($"Nachricht an {client.Value} gesendet.");
                }
                catch (Exception)
                {
                    Logger.Instance.Log($"Die Verbindung zu {client.Value} ist nicht offen.");
                }
            }
        }
        private async Task CheckForGameOver()
        {
            if (_gameHelper.FoundPairs.Count == _gameState.PairsCount)
            {
                Logger.Instance.Log($"Alle {_gameHelper.FoundPairs.Count} Pärchen gefunden. Das Spiel ist zu Ende.");

                var winner = _gameHelper.WhoWins(_gameState);
                var actionMessage = new ActionMessage
                {
                    ActionType = ActionType.Chat,
                    ChatMessage = new List<ChatContent>
                    {
                        new ChatContent
                        {
                            Type = "Text",
                            Content = $"Herzlichen Glückwunsch an {winner} 🏆🏆🏆🏆🏆🏆🏆.\nDas Spiel ist zu Ende."
                        }
                    }
                };
                await SendMessageToAllClientsAsync(actionMessage);
                await Task.Delay(1000);

                var newActionMessage = new ActionMessage
                {
                    ActionType = ActionType.NewGame
                };
                await SendMessageToAllClientsAsync(newActionMessage);
            }
        }
        public static void ResetConnectedClients()
        {
            // Entferne alle Clients aus der Liste
            foreach (var client in ConnectedClients.Keys.ToList())
            {
                ConnectedClients.TryRemove(client, out var playerName);
                Logger.Instance.LogAction($"Verbindung mit Client {playerName} entfernt.");
            }
            
            Logger.Instance.LogAction("Alle Clients wurden entfernt.");
        }

        private ActionMessage CreateChatMessage()
        {
            var actionMessage = new ActionMessage
            {
                ActionType = ActionType.Chat,
                ChatMessage = new List<ChatContent>
                {
                    new ChatContent
                    {
                        Type = "Text",
                        Content = "Willkommen zum neuen Spiel!" 
                    },
                    
                    new ChatContent
                    {
                        Type = "Image",
                        Content = "pack://application:,,,/Images/vielerfolg.png"
                    }
                }
            };
            return actionMessage;
        }
    }

}
