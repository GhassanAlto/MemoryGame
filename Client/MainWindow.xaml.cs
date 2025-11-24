using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace MemoryGame.Client
{
    public partial class MainWindow : Window
    {
        private GameState _gameState;
        private ClientWebSocket? _webSocket;
        bool tryToConnect = true;
        bool connected = false;
        public GameBoard _board;
        private ActionProcessor _actionProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public string? _playerName;
        public string? ipAdresse;
        public int _revealedCardsCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            _board = new GameBoard(this); // MainWindow-Instanz  
            _actionProcessor = new ActionProcessor(this);
            StartSite.Visibility = Visibility.Visible;
            GameBoard.Visibility = Visibility.Hidden;
            // Emojis dynamisch erstellen und dem UniformGrid hinzufügen
            var emojiButtons = GenerateEmojiButtons();
            foreach (var button in emojiButtons)
            {
                // Setzt den Click-Handler für jeden Button und bindet ihn an die Instanz von MainWindow
                button.Click += Emoji_Click;
                EmojiUniformGrid.Children.Add(button);
            }
        }

        private async void HandleConnectButtonClicked(object sender, RoutedEventArgs e)
        {

            ConnectButton.IsEnabled = false;
            _playerName = PlayerNameTextBox.Text;

            if (string.IsNullOrEmpty(_playerName))
            {
                _board.UpdateStatus("Spielername darf nicht leer sein.");
                ConnectButton.IsEnabled = true;
                return;
            }
            if (string.IsNullOrEmpty(ServerIPBox.Text))
            {
                _board.UpdateStatus("IP-Adresse darf nicht leer sein");
                ConnectButton.IsEnabled = true;
                return;
            }
            ipAdresse = ServerIPBox.Text.Trim();    
            _board.UpdateStatus("Verbinde...");
            await ConnectToServerAsync();

            if (connected)
            {
                var actionMessage = new ActionMessage
                {
                    ActionType = ActionType.Register,
                    Name = _playerName,
                };
                await SendMessageAsync(actionMessage);
            } 
        }

        private async Task ConnectToServerAsync()
        {
            tryToConnect = true;
            int i = 0;
            while (tryToConnect)
            {
                try
                {
                    _webSocket = new ClientWebSocket();
                    await _webSocket.ConnectAsync(new Uri($"ws://{ipAdresse}:5000"), CancellationToken.None);
                    _board.UpdateStatus("Verbunden ...");
                    StatusTextBlock2.Text = "Warte auf weitere  Spieler......";
                    connected = true;
                    _ = ProcessIncomingMessagesAsync();
                    break;
                }
                catch (Exception ex)
                {
                    i++;
                    _board.UpdateStatus($"Verbindung fehlgeschlagen: {ex.Message}. Versuche erneut in 5 Sekunden...");
                    await Task.Delay(5000);
                }
                if (i > 1)
                {
                    _board.UpdateStatus($"Keine Verbindung möglich");
                    ConnectButton.IsEnabled = true;
                    tryToConnect = false;
                }
            }
        }

        public async Task SendMessageAsync(ActionMessage message)
        {
            var messageToSend = JsonSerializer.Serialize(message);

            try
            {
                var buffer = Encoding.UTF8.GetBytes(messageToSend);
                if (_webSocket != null)
                {
                    await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Senden der Nachricht: {ex.Message}");
            }
        }
        private async Task ProcessIncomingMessagesAsync()
        {
            var buffer = new byte[4096];
            if (_webSocket == null) { return; }
            while (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        var parts = message.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                        var actionMessageJson = parts[0];
                        var gameStateJson     = parts[1];
                        var actionMessage     = JsonSerializer.Deserialize<ActionMessage>(actionMessageJson);
                        var gameState         = JsonSerializer.Deserialize<GameState>(gameStateJson);
                        if (gameState != null && actionMessage != null)
                        {
                            _gameState = gameState;
                            _board.CreatePlayerGrids(gameState);
                            _board.UpdateInfoBar(gameState);
                            _actionProcessor.ProcessActionMessage(actionMessage, gameState);
                        }

                    }
                }
                catch (Exception ex)
                {
                    _board.UpdateStatus($"Fehler beim Empfangen: {ex.Message}");
                }
            }
        }

        public void StartGame(GameState gameState)
        {
            _board.ResetGameBoard(gameState);
            foreach (var card in gameState.Cards)
            {
                var cardControl = new Card
                {
                    ID = card.ID,
                    Flip = card.Flip,
                    IsFaceUp = card.IsFaceUp,


                    FrontContent = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Images/memoryQuestionMark.png"))
                    },
                    BackContent = new Image
                    {
                        Source = new BitmapImage(new Uri($"pack://application:,,,/Images/emoji{card.ID}.png"))
                    }
                };

                cardControl.MouseDown += Card_MouseDown;
                UniFormGrid_Table.Children.Add(cardControl);
            }
        }
        private async void Card_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_revealedCardsCount == 2) return;
            if (_gameState.CurrentPlayer.Name != _playerName)
            {
                MessageBox.Show("Du bist nicht am Zug!");
                return;
            }
            _revealedCardsCount++;
            if (sender is Card clickedCard && !clickedCard.IsFaceUp)
            {
                var cardIndex = UniFormGrid_Table.Children.IndexOf(clickedCard);
                var CardActionMessage = new ActionMessage
                {
                    ActionType = ActionType.GetNextAction,
                    ClickedCardIndex = cardIndex
                };

                try
                {
                    await SendMessageAsync(CardActionMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Erstellen oder Senden der Nachricht: {ex.Message}");
                }
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await HandleChatMessageSending();
            // Leere die RichTextBox nach dem Senden der Nachricht
            MessageTextBox.Document.Blocks.Clear();
        }
        private async Task HandleChatMessageSending()
        {
            // Extrahiere den Inhalt (Text und Bilder) aus der RichTextBox
            var contentList = ExtractOrderedContentFromRichTextBox(MessageTextBox);

            // Erstelle die ActionMessage
            var newActionMessage = new ActionMessage
            {
                ActionType = ActionType.Chat,
                Name = _playerName,
                ChatMessage = contentList // Liste von ChatContent als string
            };

            // Nachricht senden
            await SendMessageAsync(newActionMessage);
        }
        // Wird aufgerufen, wenn eine Taste im Textfeld gedrückt wird
        private async void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) // Prüft, ob Enter gedrückt wurde
            {
                e.Handled = true; // Verhindert, dass Enter als Zeilenumbruch eingefügt wird
                await HandleChatMessageSending();
                MessageTextBox.Document.Blocks.Clear();
            }
        }
        // Setzt den Fokus auf das Textfeld, wenn das Fenster geladen wird
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Focus();
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            // Popup anzeigen oder schließen
            EmojiPopup.IsOpen = !EmojiPopup.IsOpen;
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Überprüfen, ob Tag gesetzt ist
                if (button.Tag is string imagePath)
                {
                    // Erstelle das Emoji-Bild
                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri($"pack://application:,,,/{imagePath}", UriKind.RelativeOrAbsolute)),
                        Width = 30,
                        Height = 28,
                    };

                    // Überprüfen, ob der aktuelle Block im RichTextBox ein Paragraph ist
                    if (MessageTextBox.Document.Blocks.LastOrDefault() is Paragraph lastParagraph)
                    {
                        // Wenn der letzte Paragraph vorhanden ist, füge das Emoji als Inline-Element hinzu
                        lastParagraph.Inlines.Add(new InlineUIContainer(image));
                    }
                    else
                    {
                        // Wenn kein Paragraph vorhanden ist, erstelle einen neuen und füge das Emoji hinzu
                        var newParagraph = new Paragraph();
                        newParagraph.Inlines.Add(new InlineUIContainer(image));
                        MessageTextBox.Document.Blocks.Add(newParagraph);
                    }

                    // Den Textbereich immer nach unten scrollen
                    MessageTextBox.ScrollToEnd();

                    // Setze den Cursor ans Ende der RichTextBox **nach dem Hinzufügen des Bildes**
                    var caretPosition = MessageTextBox.CaretPosition;
                    MessageTextBox.CaretPosition = MessageTextBox.Document.ContentEnd;
                }

                // Emoji in das Textfeld einfügen
                MessageTextBox.Focus();

                // Popup schließen
                EmojiPopup.IsOpen = false;
            }
        }

        // Methode zum Erstellen einer Liste von Emoji-Buttons
        public List<Button> GenerateEmojiButtons()
        {
            var emojiButtons = new List<Button>();

            // Eine Liste der Bildpfade für die Emojis (von emoji1.png bis emoji25.png)
            List<string> emojiImagePaths = new List<string>();
            for (int i = 1; i <= 25; i++)
            {
                emojiImagePaths.Add($"Smilies/smilie{i}.png");
            }

            // Erstellen der Buttons
            foreach (var imagePath in emojiImagePaths)
            {
                var button = new Button
                {
                    Background = System.Windows.Media.Brushes.Transparent,
                    Tag = imagePath // Das Tag speichert den Pfad des Bildes
                };

                // Fügen Sie das Bild als Content des Buttons hinzu
                button.Content = new Image
                {
                    Source = new BitmapImage(new Uri(imagePath, UriKind.Relative)),
                };

                emojiButtons.Add(button);
            }

            return emojiButtons;
        }

        private List<ChatContent> ExtractOrderedContentFromRichTextBox(RichTextBox richTextBox)
        {
            var contentList = new List<ChatContent>();

            foreach (Block block in richTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is Run run)
                        {
                            // Textinhalt hinzufügen
                            contentList.Add(new ChatContent { Type = "Text", Content = run.Text });
                        }
                        else if (inline is InlineUIContainer inlineUIContainer &&
                          inlineUIContainer.Child is System.Windows.Controls.Image image)
                        {
                            // Bildinhalt als Base64-String speichern
                            if (image.Source is BitmapImage bitmapImage)
                            {
                                string imagePath = image.Source.ToString();
                                contentList.Add(new ChatContent { Type = "Image", Content = imagePath });
                            }
                        }
                    }
                }
            }

            return contentList;
        }

    }
}