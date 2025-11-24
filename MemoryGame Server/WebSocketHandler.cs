using System.Net.WebSockets;
using System.Text;

namespace MemoryGameServerWPF
{
    public class WebSocketHandler
    {
        private readonly List<WebSocket> _connections = new(); // Liste der aktiven Verbindungen
        private GameLogic? _gameLogic;

        public async Task HandleWebSocketConnection(WebSocket webSocket, GameState gameState, CancellationToken cancellationToken)
        {
            lock (_connections)
            {
                _connections.Add(webSocket);
            }

            if (_gameLogic == null)
            {
                _gameLogic = new GameLogic(gameState);
            }

            var buffer = new byte[1024 * 4];

            try
            {
                while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await _gameLogic.HandleMessage(webSocket, message);
                    }
                }
            }
            finally
            {
                lock (_connections)
                {
                    _connections.Remove(webSocket);
                }

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                }
            }
        }

    }
}
