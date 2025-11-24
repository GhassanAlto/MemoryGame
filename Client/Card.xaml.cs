using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MemoryGame.Client
{
    public partial class Card : UserControl // Hier sollte die Klasse von UserControl abgeleitet sein
    {
        public string ID { get; set; }

        public static readonly DependencyProperty FrontContentProperty = DependencyProperty.Register(
            nameof(FrontContent), typeof(object), typeof(Card), new PropertyMetadata(default(object)));

        public object FrontContent
        {
            get { return GetValue(FrontContentProperty); }
            set { SetValue(FrontContentProperty, value); }
        }

        public static readonly DependencyProperty BackContentProperty = DependencyProperty.Register(
            nameof(BackContent), typeof(object), typeof(Card), new PropertyMetadata(default(object)));

        public object BackContent
        {
            get { return GetValue(BackContentProperty); }
            set { SetValue(BackContentProperty, value); }
        }

        public static readonly DependencyProperty FlipProperty = DependencyProperty.Register(
            nameof(Flip), typeof(bool), typeof(Card), new PropertyMetadata(default(bool), OnFlipCallback));

        public bool Flip
        {
            get { return (bool)GetValue(FlipProperty); }
            set { SetValue(FlipProperty, value); }
        }

        private bool _isFlipped; // Flag, das angibt, ob die Karte umgedreht ist
        public bool IsFaceUp
        {
            get { return _isFlipped; }
            set { _isFlipped = value; } // Setter hinzugefügt
        }
        public int CardIndex { get; set; }

        private bool isMatched;
        public bool IsMatched
        { 
            get { return isMatched; }
            set { isMatched = value; } // Setter hinzugefügt
        }
        public static void OnFlipCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Wenn die Flip-Eigenschaft auf true gesetzt wird, wird die OnFlip-Methode aufgerufen
            if ((bool)e.NewValue)
                ((Card)d).OnFlip();
        }

        public void OnFlip()
        {
            // Erstellen eines MediaPlayer-Objekts zur Wiedergabe des Kartenflip-Geräuschs
            MediaPlayer mediaPlayer = new MediaPlayer();
            mediaPlayer.Open(new Uri("Sounds/flip-card.mp3", UriKind.Relative));
            mediaPlayer.Play(); // Abspielen des Flip-Geräuschs

            // Holen der Storyboards für die Animationen
            var storyboardBegin = FindResource("FlipFirst") as Storyboard;
            var storyboardReverse = FindResource("FlipLast") as Storyboard;

            // Überprüfen, ob die Karte aktuell umgedreht ist
            if (!_isFlipped)
            {
                storyboardReverse?.Stop(); // Stoppen der Rückflip-Animation
                storyboardBegin?.Begin(); // Starten der Vorflip-Animation
                _isFlipped = true; // Flag setzen, dass die Karte umgedreht ist
            }
            else
            {
                storyboardBegin?.Stop(); // Stoppen der Vorflip-Animation
                storyboardReverse?.Begin(); // Starten der Rückflip-Animation
                _isFlipped = false; // Flag setzen, dass die Karte wieder normal ist
            }
            if (isMatched)
            {
                // Ändere das Aussehen der Karte, wenn sie aufgedeckt ist
                this.Opacity = 0.5; // Beispiel: Mache die Karte halbtransparent
                this.IsHitTestVisible = false; // Deaktiviere die Interaktivität
            }
            //else
            //{
            //    this.Opacity = 1; // Setze die Transparenz zurück
            //    this.IsHitTestVisible = false; // Aktiviere die Interaktivität
            //}
        }

        public Card()
        {
            InitializeComponent(); // Initialisierung der Komponenten des UserControls
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (!IsFaceUp) // Überprüfen, ob die Karte nicht aufgedeckt ist
            {
                OnFlip(); // Aufruf der OnFlip-Methode, wenn die Karte angeklickt wird
            }
        }
    }
}
