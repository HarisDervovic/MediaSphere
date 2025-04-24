using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.ComponentModel;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für Mediathek.xaml
    /// </summary>
    /// 

    public class Medium
    {
        public int MedienID { get; set; }
        public string Pfad { get; set; }
        public string Typ { get; set; }
        public string Titel { get; set; }
        public string Kategorie { get; set; }
    }



    public partial class Mediathek : UserControl
    {
        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";

        private MainWindow2 MainWindow2;
        bool _Gast;
        private ICollectionView DisplayView;
        public Mediathek(MainWindow2 mainwindow2, bool Gast)
        {
            InitializeComponent();
            _Gast = Gast;
            MainWindow2 = mainwindow2;
            if (Gast)
            {
                ButtonPlaylist.IsEnabled = false;
            }
            DisplayView = CollectionViewSource.GetDefaultView(LadeAlleMedien());
            DataContext = DisplayView;
        }
    

        private List<Medium> LadeAlleMedien()
        {
            List<Medium> medienListe = new List<Medium>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT MedienID, Pfad, Typ, Titel, Kategorie FROM Medien";
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Medium medium = new Medium
                        {
                            MedienID = Convert.ToInt32(reader["MedienID"]),
                            Pfad = reader["Pfad"].ToString(),
                            Typ = reader["Typ"].ToString(),
                            Titel = reader["Titel"].ToString(),
                            Kategorie = reader["Kategorie"].ToString()
                        };
                        medienListe.Add(medium);
                    }
                }
            }

            return medienListe;
        }

        private void ButtonStartseite_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Startseite(MainWindow2, _Gast));
        }

        private void ButtonMediathekErweitern_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new MediathekErweitern(MainWindow2, _Gast));
        }

        private void ButtonAbmelden_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AngemeldeterBenutzername = string.Empty;
            Properties.Settings.Default.Save();

            MainWindow login = new MainWindow();
            login.Show();
            MainWindow2.Close();
        }

        private void TextBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAktualisieren();  
        }

        private void ComboBoxTypFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterAktualisieren();
        }

        private void FilterAktualisieren()
        {
            if (TextBoxFilter == null || ComboBoxTypFilter == null || DisplayView == null)
            {
                return; //Bugfix
            }

            string filter = TextBoxFilter.Text?.ToLower() ?? "";
            string typ = (ComboBoxTypFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Alle";

            if (typ == "Alle")
            {
                DisplayView.Filter = x => ((Medium)x).Titel.ToLower().Contains(filter) || ((Medium)x).Kategorie.ToLower().Contains(filter);
            }
            else if (typ == "mp3")
            {
                DisplayView.Filter = x => ((Medium)x).Typ.Equals("mp3") && (((Medium)x).Titel.ToLower().Contains(filter) || ((Medium)x).Kategorie.ToLower().Contains(filter));
            }
            else if (typ == "mp4")
            {
                DisplayView.Filter = x => ((Medium)x).Typ.Equals("mp4") && (((Medium)x).Titel.ToLower().Contains(filter) || ((Medium)x).Kategorie.ToLower().Contains(filter));
            }
        }

        private void ButtonAbspielen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;

            if (medium == null || string.IsNullOrWhiteSpace(medium.Pfad))
                return;

            try
            {
                var mediaPlayer = MainWindow2.FindName("MediaPlayer") as MediaElement;
                var titelTextBlockAudio = MainWindow2.FindName("TextBlockAktuellerTitelAudio") as TextBlock;
                var titelTextBlockVideo = MainWindow2.FindName("TextBlockAktuellerTitelVideo") as TextBlock;
                var dockPanel = MainWindow2.FindName("DockPlayer") as DockPanel;
                var videoOverlay = MainWindow2.FindName("VideoOverlay") as Grid;

                if (mediaPlayer != null)
                {
                    mediaPlayer.Stop();
                    mediaPlayer.Source = new Uri(medium.Pfad, UriKind.Absolute);
                    mediaPlayer.Play();
                }

                

                if (medium.Typ.ToLower() == "mp3")
                {

                    titelTextBlockAudio.Text = $"🎵 {medium.Titel}";

                    if (dockPanel != null)
                        dockPanel.Visibility = Visibility.Visible;

                    if (videoOverlay != null)
                        videoOverlay.Visibility = Visibility.Collapsed;
                }
                else if (medium.Typ.ToLower() == "mp4")
                {

                    titelTextBlockVideo.Text = $"🎬 {medium.Titel}";

                    if (videoOverlay != null)
                        videoOverlay.Visibility = Visibility.Visible;

                    if (dockPanel != null)
                        dockPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abspielen: {ex.Message}", "Abspielen", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            MainWindow2.InitMediaTimer();
        }
    }
}
