using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryGame.Client
{
    public class GameBoard
    {
        private MainWindow mainWindow;

        private readonly Brush[] playerColors = new Brush[] { Brushes.DeepPink, Brushes.White, Brushes.LightGoldenrodYellow, Brushes.PowderBlue };
        public GameBoard(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }
        public void CreatePlayerGrids(GameState gameState)
        {
            // Maximal mögliche Schriftgröße
            const double maxFontSize = 20;
            // Minimal mögliche Schriftgröße
            const double minFontSize = 10;

            // Schriftgröße basierend auf Spieleranzahl berechnen
            double fontSize = Math.Max(minFontSize, maxFontSize - (gameState.PlayerNames.Count - 1) * 2);

            // Anzahl der Spieler
            int playerCount = gameState.PlayerNames.Count;

            // Berechnung der Columns und Rows für UniformGrid
            int uniformGridColumns = Math.Max(16, (int)Math.Ceiling(30.0 / playerCount)); // Dynamisch abnehmende Spalten
            int uniformGridRows = (int)Math.Ceiling(playerCount / (double)uniformGridColumns); // Zeilenanzahl basierend auf Spalten



            for (int i = 0; i < playerCount; i++)
            {
                // Neues Grid erstellen
                Grid playerGrid = new Grid
                {
                    Margin = new Thickness(5)
                };

                // Spalten definieren
                playerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 1*
                playerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 1*
                playerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6, GridUnitType.Star) }); // 6*

                // Spielername hinzufügen (1. Spalte)
                TextBlock playerNameTextBlock = new TextBlock
                {
                    Text = "",
                    FontSize = fontSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                Grid.SetColumn(playerNameTextBlock, 0);
                playerGrid.Children.Add(playerNameTextBlock);

                // Score hinzufügen (2. Spalte)
                TextBlock scoreTextBlock = new TextBlock
                {
                    Text = "", // Startscore
                    FontSize = fontSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                Grid.SetColumn(scoreTextBlock, 1);
                playerGrid.Children.Add(scoreTextBlock);

                // UniformGrid hinzufügen (3. Spalte)
                UniformGrid uniformGrid = new UniformGrid
                {
                    Rows = uniformGridRows,
                    Columns = uniformGridColumns,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,

                };

                Grid.SetColumn(uniformGrid, 2);
                playerGrid.Children.Add(uniformGrid);

                // PlayerGrid zu MainGrid hinzufügen
                mainWindow.MainGrid.Children.Add(playerGrid);

                Grid.SetRow(playerGrid, i); // Grid in die richtige Zeile setzen
            }
        }

        public void UpdateInfoBar(GameState gameState)
        {
            // Aktualisiere den Label für den aktuellen Spieler  
            mainWindow.CurrentPlayerLabel.Content = gameState.CurrentPlayer.Name == mainWindow._playerName
               ? "Du bist am Zug!!"
               : $"{gameState.CurrentPlayer.Name} ist jetzt am Zug!!";
            mainWindow.CurrentPlayerLabel.Foreground = gameState.CurrentPlayer.Name == mainWindow._playerName
               ? Brushes.DarkRed // wenn der Spieler am Zug ist  
               : Brushes.White; //  wenn der Spieler wartet  


            // Überprüfen, ob gameState oder PlayerNames null ist
            if (gameState == null || gameState.PlayerNames == null)
            {
                throw new ArgumentNullException(nameof(gameState), "GameState oder PlayerNames ist null.");
            }

            // Überprüfen, ob MainGrid korrekt initialisiert ist
            if (mainWindow.MainGrid == null)
            {
                throw new InvalidOperationException("MainGrid ist nicht korrekt initialisiert.");
            }

            // Überprüfen, ob die Anzahl der Spieler mit den Zeilen im MainGrid übereinstimmt
            if (gameState.PlayerNames.Count > mainWindow.MainGrid.RowDefinitions.Count)
            {
                // Falls mehr Spieler als Zeilen vorhanden sind, fügen wir zusätzliche Zeilen hinzu
                for (int i = mainWindow.MainGrid.RowDefinitions.Count; i < gameState.PlayerNames.Count; i++)
                {
                    mainWindow.MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
            }
            var sortedPlayers = gameState.PlayerNames.OrderByDescending(p => p.Score).ToList();

            // Aktualisieren der Infos für jeden Spieler  
            for (int i = 0; i < gameState.PlayerNames.Count; i++)
            {
                var player = gameState.PlayerNames[i];

                // Überprüfen, ob das PlayerGrid existiert und der Index gültig ist  
                if (i < mainWindow.MainGrid.Children.Count && mainWindow.MainGrid.Children[i] is Grid playerGrid)
                {
                    // Spielername aktualisieren (1. Spalte)  
                    if (playerGrid.Children.Count > 0 && playerGrid.Children[0] is TextBlock playerNameTextBlock)
                    {
                        playerNameTextBlock.Text = $"{player.Name}";
                        // Farbe des Spielernamens anpassen    
                        if (player.Name == mainWindow._playerName)
                        {
                            playerNameTextBlock.Foreground = Brushes.Cyan;
                        }
                        else
                        {
                            // Farbe aus der Liste auswählen  
                            int colorIndex = (gameState.PlayerNames.IndexOf(player) + 1) % playerColors.Length;
                            playerNameTextBlock.Foreground = playerColors[colorIndex];
                        }
                    }

                    // Score aktualisieren (2. Spalte)  
                    if (playerGrid.Children.Count > 1 && playerGrid.Children[1] is TextBlock scoreTextBlock)
                    {
                        scoreTextBlock.Text = $"Score: {player.Score}";
                        // Farbe des Scores anpassen  
                        var playerIndex = sortedPlayers.FindIndex(p => p.Score == player.Score);
                        if (sortedPlayers.Count == 1 || (sortedPlayers.Count > 1 && playerIndex == 0 && sortedPlayers[0].Score == sortedPlayers[sortedPlayers.Count - 1].Score))
                        {
                            scoreTextBlock.Foreground = Brushes.White; // Alle Spieler haben den gleichen Score  
                        }
                        else if (playerIndex == 0)
                        {
                            scoreTextBlock.Foreground = Brushes.Gold; // Erster Platz: Gold  
                        }
                        else if (playerIndex == sortedPlayers.Count - 1)
                        {
                            scoreTextBlock.Foreground = Brushes.DarkRed; // Letzter Platz: Rot  
                        }
                        else if (playerIndex == sortedPlayers.Count - 2)
                        {
                            scoreTextBlock.Foreground = Brushes.Orange; // Vorletzter Platz: Weiß  
                        }
                        else
                        {
                            scoreTextBlock.Foreground = Brushes.White; // Alle anderen Plätze: Grün  
                        }
                    }
                }
            }

            mainWindow.ErfolgsLabel.Content = gameState.Message;
        }

        public void AddImagesToGrid(GameState gameState, string id)
        {
            var currentPlayer = gameState.CurrentPlayer;
            var playerName = currentPlayer.Name;

            UniformGrid? playerUniformGrid = null;

            // Gehe alle Spieler durch und bestimme das UniformGrid des aktuellen Spielers
            for (int i = 0; i < gameState.PlayerNames.Count; i++)
            {
                var player = gameState.PlayerNames[i];

                // Wenn der aktuelle Spieler gefunden wurde, dann das entsprechende UniformGrid zuweisen
                if (player.Name == playerName)
                {
                    // Das 3. Element der Zeile, das UniformGrid, holen
                    var playerGrid = mainWindow.MainGrid.Children[i] as Grid;
                    if (playerGrid != null && playerGrid.Children.Count > 2)
                    {
                        playerUniformGrid = playerGrid.Children[2] as UniformGrid;
                    }
                    break;
                }
            }
            // URI für das Bild basierend auf der ID
            Uri imageUri = new Uri($"pack://application:,,,/Images/emoji{id}.png", UriKind.Absolute);

            // BitmapImage erstellen und als Source für das Image setzen
            var bitmap = new BitmapImage(imageUri);

            // Zwei Image-Elemente erstellen
            var image1 = new Image { Source = bitmap };
            var image2 = new Image { Source = bitmap };
            if (playerUniformGrid != null)
            {
                // Die Bilder zum UniformGrid hinzufügen
                playerUniformGrid.Children.Add(image1);
                playerUniformGrid.Children.Add(image2);
            }
        }

        public void UpdateStatus(string status)
        {
            mainWindow.StatusTextBlock.Text = status;
        }

        public void ResetGameBoard(GameState gameState)
        {
            mainWindow.GameBoard.Visibility = Visibility.Visible;
            mainWindow.StartSite.Visibility = Visibility.Hidden;
            mainWindow.StatusTextBlock.Text = "";
            mainWindow.StatusTextBlock2.Text = "";
            mainWindow.UniFormGrid_Table.Children.Clear();

            // Leere den Inhalt aller UniformGrid-Elemente  
            for (int i = 0; i < mainWindow.MainGrid.Children.Count; i++)
            {
                var playerGrid = mainWindow.MainGrid.Children[i] as Grid;
                if (playerGrid != null && playerGrid.Children.Count > 2)
                {
                    var uniformGrid = playerGrid.Children[2] as UniformGrid;
                    if (uniformGrid != null)
                    {
                        uniformGrid.Children.Clear();
                    }
                }
            }
            
        }

    }
}
