using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static MemoryGame.Client.MainWindow;




namespace MemoryGame.Client
{

    public class ActionProcessor
    {
        private readonly MainWindow _mainWindow;
        private  GameState _gameState;
       
        public ActionProcessor(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void ProcessActionMessage(ActionMessage actionMessage, GameState gameState)
        {
            _gameState = gameState;
            // Verarbeite die Aktion basierend auf ihrem Typ  
            switch (actionMessage.ActionType)
            {
                case ActionType.StarteGame:
                    // Starte ein neues Spiel  
                    _mainWindow.StartGame(gameState);
                    break;
                case ActionType.FirstCard:
                case ActionType.SecondCard:
                    // Drehe die erste/zweite Karte um  
                    FlipCard(actionMessage.ClickedCardIndex);
                    break;
                case ActionType.NoMatch:
                    // Verarbeite den Fall, dass die Karten nicht übereinstimmen  
                    HandleNoMatch(actionMessage.ClickedCardIndex, gameState);
                    break;
                case ActionType.MatchFound:
                    // Verarbeite den Fall, dass die Karten übereinstimmen  
                    HandleMatchFound(actionMessage.ClickedCardIndex, gameState);
                    break;
                case ActionType.NewGame:
                    // Verarbeite den Fall, dass ein neues Spiel gestartet werden soll  
                    HandleNewGame(gameState);
                    break;
                case ActionType.Ciao:
                    // Verarbeite den Fall, dass ein Spieler kein neues Spiel starten will  
                    HandleNoNewGame(gameState);
                    break;
                case ActionType.Chat:
                    // Verarbeite den Fall, dass ein Spieler kein neues Spiel starten will  
                    HandleChatMessage(actionMessage);
                    break;
                default:
                    MessageBox.Show("Unerwarteter Aktionstyp");
                    break;
            }
        }
        private void HandleChatMessage(ActionMessage _chatMessage)
        {
            // Stelle sicher, dass ChatMessage nicht null ist und die Liste korrekt verarbeitet wird
            if (_chatMessage.ChatMessage != null)
            {
                string timeOnly = DateTime.Now.ToString("HH:mm:ss");
                string playerName = _chatMessage.Name ?? "[Server]"; // Falls der Name null ist, verwenden wir "Unbekannt"

                var paragraph = new Paragraph{ Margin = new Thickness(0) };                
                // Füge den Zeitstempel und den Spielernamen vor der Nachricht hinzu
                Brush textColor = playerName == _mainWindow._playerName ? Brushes.Red : Brushes.Gray; // Grau, wenn Bedingung erfüllt, sonst Schwarz
                paragraph.Inlines.Add(new Run($"{timeOnly} {playerName}: ") { Foreground = textColor });
                // Gehe durch jedes Element in der ChatMessage-Liste
                foreach (var item in _chatMessage.ChatMessage)
                {
                    // Prüfe, ob das Element vom Typ ChatContent ist
                    if (item is ChatContent content)
                    {
                        // Je nach Typ des Inhalts fügen wir Text oder ein Bild hinzu
                        if (content.Type == "Text" && content.Content is string text)
                        {
                            // Füge Text hinzu
                            paragraph.Inlines.Add(new Run(text));
                        }
                        else if (content.Type == "Image" && content.Content is string imagePath)
                        {
                            try
                            {
                                // Erstelle ein Bild und füge es hinzu
                                var image = new Image
                                {
                                    Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute)),
                                    Width = 30,
                                    Height = 28,
                                };
                                paragraph.Inlines.Add(new InlineUIContainer(image));
                            }
                            catch (Exception ex)
                            {
                                // Fehlerbehandlung beim Laden des Bildes
                                MessageBox.Show($"Fehler beim Laden des Bildes: {ex.Message}");
                                paragraph.Inlines.Add(new Run("[Bild konnte nicht geladen werden]"));
                            }
                        }
                    }
                }

                // Füge den Paragraphen dem RichTextBox-Dokument hinzu und scrolle nach unten
                _mainWindow.ChatBox.Document.Blocks.Add(paragraph);
                _mainWindow.ChatBox.ScrollToEnd();  // Scrollt zum neuesten Eintrag
            }
        }

        public void FlipCard(int cardIndex)
        {
            // Hole die Karte aus dem Grid  
            var cardControl = (Card)_mainWindow.UniFormGrid_Table.Children[cardIndex];
            if (cardControl != null)
            {
                cardControl.OnFlip();
            }
        }
        public void HandleNoMatch(int cardIndex, GameState gameState)
        {
            // Setze die Anzahl der aufgedeckten Karten zurück  
            _mainWindow._revealedCardsCount = 0;
            // Hole die Karte und die vorherige Karte aus dem Grid  
            var cardControl = (Card)_mainWindow.UniFormGrid_Table.Children[cardIndex];
            var previousCardControl = (Card)_mainWindow.UniFormGrid_Table.Children[gameState.PreviousCard.CardIndex];

            if (cardControl != null && previousCardControl != null)
            {
                // Drehe beide Karten um  
                cardControl.OnFlip();
                previousCardControl.OnFlip();
            }
        }

        public async void HandleMatchFound(int cardIndex, GameState gameState)
        {
            // Setze die Anzahl der aufgedeckten Karten zurück  
            _mainWindow._revealedCardsCount = 0;
            // Hole die Karte aus dem Grid  
            var cardControl = (Card)_mainWindow.UniFormGrid_Table.Children[cardIndex];
            var previousCardControl = (Card)_mainWindow.UniFormGrid_Table.Children[gameState.PreviousCard.CardIndex];
            cardControl.IsMatched = true;
            previousCardControl.IsMatched = true;
            await FlipCardsAsync(10, cardControl, previousCardControl);

            // Füge die Karte zum Grid hinzu  
            _mainWindow._board.AddImagesToGrid(gameState, cardControl.ID);
            // Spiele ein Erfolgssignal ab  
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri("Sounds/success.wav", UriKind.Relative));
            mediaPlayer.Play();
        }
        public async Task FlipCardsAsync(int repeatCount, Card cardControl, Card previousCardControl)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                // Berechne die Verzögerung. Anfangs langsam, dann immer schneller.
                int delayTime = Math.Max(10, 30 - (i * 2));

                // Flip die Karten
                cardControl.OnFlip();           // Flip die aktuelle Karte
                previousCardControl.OnFlip();   // Flip die vorherige Karte

                // Warte die berechnete Verzögerung
                await Task.Delay(delayTime);

                // Flip die Karten zurück
                cardControl.OnFlip();           // Flip die aktuelle Karte zurück
                previousCardControl.OnFlip();   // Flip die vorherige Karte zurück

                // Warte die berechnete Verzögerung
                await Task.Delay(delayTime);
            }
        }

        public async void HandleNewGame(GameState gameState)
        {
            await Task.Delay(300);
            // Hole die Spieler-Scores  
            var spielerScores = gameState.PlayerNames.OrderByDescending(x => x.Score);
            // Hole den höchsten Score  
            var hoechsterScore = spielerScores.FirstOrDefault().Score;
            // Hole den Gewinner  
            var gewinner = spielerScores.Where(x => x.Score == hoechsterScore).ToList();
            // Erstelle eine Nachricht für den Benutzer  
            var messageText = CreateMessageText(gewinner, spielerScores);
            // Zeige eine MessageBox an  
            var newGameResult = MessageBox.Show(messageText, "Neues Spiel", MessageBoxButton.YesNo, MessageBoxImage.Question);
            // Verarbeite das Ergebnis der MessageBox  
            HandleNewGameResult(newGameResult);
        }

        private string CreateMessageText(List<Player> gewinner, IOrderedEnumerable<Player> spielerScores)
        {
            var messageText = new StringBuilder($"Der Gewinner ist: \n");
            foreach (var spieler in gewinner)
            {
                messageText.AppendLine($"{spieler.Name} mit {spieler.Score} Punkten");
            }
            messageText.AppendLine("\nAktuelle Highscores:\n");
            foreach (var spieler in spielerScores)
            {
                messageText.AppendLine($"{spieler.Name}: {spieler.Score} Punkte");
            }
            messageText.AppendLine("\nMöchten Sie ein neues Spiel starten?");
            return messageText.ToString();
        }
 
        private void HandleNewGameResult(MessageBoxResult newGameResult)
        {
            // Verarbeite das Ergebnis  
            if (newGameResult == MessageBoxResult.Yes)
            {
                ReadyForNewGame();
            }
            else if (newGameResult == MessageBoxResult.No)
            {
              CloseGame();
            }
        }

        private void ReadyForNewGame()
        {
            // Erstelle eine neue Aktion  
            var newActionMessage = new ActionMessage
            {
                ActionType = ActionType.ReadyForNewGame,
            };
            // Aktualisiere den Status  
            _mainWindow._board.UpdateStatus("Warte auf Antwort von anderen Spielern....");
            // Zeige die Startseite an  
            _mainWindow.StartSite.Visibility = Visibility.Visible;
            // Verstecke die Spielbrett-Seite  
            _mainWindow.GameBoard.Visibility = Visibility.Hidden;
            // Verstecke das Eingabe-Feld  
            _mainWindow.EingabeField.Visibility = Visibility.Hidden;
           
            // Sende die Aktion  
            _mainWindow.SendMessageAsync(newActionMessage).Wait();
        }

        private async void CloseGame()
        {
            // Erstelle eine neue Aktion  
            var newActionMessage = new ActionMessage
            {
                ActionType = ActionType.NoNewGame,
                Name = _mainWindow._playerName,
            };
            // Sende die Aktion  
            _mainWindow.SendMessageAsync(newActionMessage).Wait();
            await Task.Delay(100);
            // Schließe das Hauptfenster  
            _mainWindow.Close();
            // Beende die Anwendung  
            Application.Current.Shutdown();
        }

        public void HandleNoNewGame(GameState gameState)
        {
            // Aktualisiere den Status  
            if(gameState.Message != null)
            _mainWindow._board.UpdateStatus(gameState.Message);
            // Zeige die Startseite an  
            _mainWindow.StartSite.Visibility = Visibility.Visible;
            _mainWindow.EingabeField.Visibility = Visibility.Hidden;
            // Verstecke die Spielbrett-Seite  
            _mainWindow.GameBoard.Visibility = Visibility.Hidden;
            
        }
    }
}
