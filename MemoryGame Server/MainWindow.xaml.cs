// Datei: MainWindow.xaml.cs
// Dieses File enthält die Haupt-WPF-Fensterklasse für den Memory-Game-Server.

using Microsoft.AspNetCore.Builder;
using System.Windows;
using System.Windows.Controls;

namespace MemoryGameServerWPF
{
    // Hauptfenster-Klasse der WPF-Anwendung
    // Partial, da es eine zugehörige XAML-Datei gibt.
    public partial class MainWindow : Window
    {
        // CancellationTokenSource zum Abbrechen des laufenden Servers/Verbindungen
        private CancellationTokenSource? _cts;

        // Task-Referenz zum Server-Task, damit wir darauf warten oder ihn prüfen können
        private Task? _serverTask;

        // GameState hält Spielzustand wie Spieleranzahl, Karten usw.
        private GameState _gameState;

        // WebApplication-Instanz des eingebetteten ASP.NET Core Servers
        private WebApplication? _app; 

        // Konstruktor: Initialisiert Komponenten, GameState und Logger
        public MainWindow()
        {
            InitializeComponent(); // Initialisiert die WPF-Controls aus XAML

            // Neues GameState-Objekt erstellen und in das Feld speichern
            _gameState = new GameState();

            // Logger konfigurieren: Callback setzen, damit Log-Nachrichten in die UI gelangen
            Logger.Instance.LogAction = LogMessage;
        }

        // LogMessage: Schreibt Nachricht in Debug-Ausgabe und in die LogTextBox der UI
        public void LogMessage(string message)
        {
            // Debug-Ausgabe in Visual Studio Output
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now}: {message}");

            // UI-Operationen müssen im UI-Thread ausgeführt werden => Dispatcher.Invoke
            Dispatcher.Invoke(() =>
            {
                // Text der LogTextBox erweitern
                LogTextBox.AppendText($"{DateTime.Now}: {message}\n");
                // Scrollt die TextBox ans Ende, damit neue Nachrichten sichtbar sind
                LogTextBox.ScrollToEnd();
            });
        }

        // Event-Handler für Klick auf den Start-Button
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Prüfen, ob eine Spieleranzahl ausgewählt wurde
            if (PlayerCountComboBox.SelectedItem == null)
            {
                // InfoLabel in der UI setzen, falls keine Auswahl vorhanden ist
                InfoLabel.Content = "Bitte eine gültige Spieleranzahl auswählen.";
                return; // Methode beenden, Server nicht starten
            }

            // Ausgewählten Wert aus dem ComboBoxItem extrahieren und in int parsen
            int playerCount = int.Parse(((ComboBoxItem)PlayerCountComboBox.SelectedItem).Content.ToString());

            // Im GameState die benötigte Spieleranzahl setzen
            _gameState.RequiredPlayers = playerCount;

            // Prüfen, ob eine Kartenanzahl ausgewählt wurde
            if (CardCountComboBox.SelectedItem == null)
            {
                // InfoLabel setzen, wenn keine Kartenanzahl gewählt wurde
                InfoLabel.Content = "Bitte eine gültige Kartenanzahl auswählen.";
                return; // Serverstart abbrechen
            }

            // Ausgewählten Kartenwert parsen (Annahme: Gesamtanzahl Karten, nicht Paare)
            int cardCount = int.Parse(((ComboBoxItem)CardCountComboBox.SelectedItem).Content.ToString());

            // PaareCount ist cardCount / 2 (zwei Karten pro Paar)
            _gameState.PairsCount = cardCount / 2;

            // Startet den Server asynchron in einem Hintergrund-Task
            StartServer();

            // UI-Buttons: Start deaktivieren, Stop aktivieren
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        // StartServer: Initialisiert und startet den ASP.NET Core WebApplication Server
        private void StartServer()
        {
            try
            {
                // CancellationTokenSource erstellen, um später abbrechen zu können
                _cts = new CancellationTokenSource();

                // Server-Task in Hintergrund starten, damit UI nicht blockiert
                _serverTask = Task.Run(() =>
                {
                    try
                    {
                        // Erstelle einen WebApplicationBuilder (Standard-Host)
                        var builder = WebApplication.CreateBuilder();

                        // Baue die App aus dem Builder
                        _app = builder.Build();

                        // WebSocket-Middleware aktivieren
                        _app.UseWebSockets();

                        // Erstelle einen Handler, der WebSocket-Verbindungen verarbeiten kann
                        var webSocketHandler = new WebSocketHandler();

                        // Mappe den Root-Pfad "/" auf eine Middleware, die WebSocket-Requests entgegennimmt
                        _app.Map("/", async context =>
                        {
                            // Prüfe, ob die eingehende Anfrage eine WebSocket-Anfrage ist
                            if (context.WebSockets.IsWebSocketRequest)
                            {
                                // Akzeptiere die WebSocket-Verbindung
                                var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                                // Übergib die WebSocket-Verbindung an den Handler und das aktuelle GameState sowie CancellationToken
                                await webSocketHandler.HandleWebSocketConnection(webSocket, _gameState, _cts.Token);
                            }
                            else
                            {
                                // Wenn keine WebSocket-Anfrage, antworte mit 400 Bad Request
                                context.Response.StatusCode = 400;
                                LogMessage("Ungültige Anfrage empfangen.");
                            }
                        });

                        // Starte den Server und binde an alle Netzwerkinterfaces auf Port 5000
                        _app.Run("http://0.0.0.0:5000");
                    }
                    catch (Exception ex)
                    {
                        // Fehler beim Starten des Servers loggen
                        LogMessage($"Fehler beim Starten des Servers: {ex.Message}");
                    }
                });

                // UI-Thread: Buttons aktualisieren (wenn nötig)
                Dispatcher.Invoke(() =>
                {
                    StartButton.IsEnabled = false; // Start soll deaktiviert bleiben
                    StopButton.IsEnabled = true;   // Stop aktivieren
                });

                // Info-Log, dass der Server gestartet wurde
                LogMessage("Server gestartet und wartet auf Verbindungen...");
            }
            catch (Exception ex)
            {
                // Fehler beim Initialisieren der Server-Startprozedur loggen
                LogMessage($"Fehler beim Initialisieren des Servers: {ex.Message}");
            }
        }

        // Event-Handler für Klick auf den Stop-Button
        // Diese Methode ist async void, weil sie als Event-Handler verwendet wird
        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // UI: Beide Buttons temporär deaktivieren
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = false;

            // Wenn es eine CancellationTokenSource gibt, dann abbrechen und aufräumen
            if (_cts != null)
            {
                // Fordere das Abbrechen aller asynchronen Operationen an
                _cts.Cancel();

                // GameLogic: Verbundene Clients zurücksetzen (statische Methode)
                GameLogic.ResetConnectedClients();

                // GameState zurücksetzen: keine erforderlichen Spieler, leere Namenliste, keine Paare
                _gameState.RequiredPlayers = 0;
                _gameState.PlayerNames.Clear();
                _gameState.PairsCount = 0;

                // Wenn die WebApplication-Instanz existiert, versuche sie sauber zu stoppen
                if (_app != null)
                {
                    try
                    {
                        // Stoppe die App asynchron (sauberer Shutdown)
                        await _app.StopAsync();
                        LogMessage("Server wurde gestoppt.");
                    }
                    catch (Exception ex)
                    {
                        // Fehler beim Stoppen der App loggen
                        LogMessage($"Fehler beim Stoppen des Servers: {ex.Message}");
                    }
                }

                try
                {
                    // Wenn ein Server-Task existiert, warte darauf, dass er beendet wird
                    if (_serverTask != null)
                    {
                        await _serverTask;
                    }
                }
                catch (Exception ex)
                {
                    // Fehler beim Warten/Aufräumen loggen
                    LogMessage($"Fehler beim Stoppen des Servers: {ex.Message}");
                }
                finally
                {
                    // Server-Task- und CancellationToken-Referenzen zurücksetzen
                    _serverTask = null;
                    _cts = null;
                }
            }

            // UI-Thread: Start-Button wieder aktivieren, Stop-Button deaktivieren
            Dispatcher.Invoke(() =>
            {
                StartButton.IsEnabled = true;  // Start-Button aktivieren
                StopButton.IsEnabled = false;  // Stop-Button deaktivieren
            });
        }

    }
}
