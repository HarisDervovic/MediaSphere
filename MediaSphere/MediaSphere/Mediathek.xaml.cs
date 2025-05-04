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
using System.IO;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für Mediathek.xaml
    /// </summary>
    /// 

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

            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });

            Application.Current.Shutdown();
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

            MainWindow2.aktuellePlaylist = null;
            MainWindow2.StarteMedium(medium);
        }

        private void ButtonLoeschen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;
            if (medium == null) return;

            try
            {
                if (File.Exists(medium.Pfad))
                    File.Delete(medium.Pfad);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Datei konnte nicht gelöscht werden: {ex.Message}");
                return;
            }

            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM Medien WHERE MedienID = @id";
                using (var command = new SQLiteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", medium.MedienID);
                    command.ExecuteNonQuery();
                }
            }

            
            DisplayView = CollectionViewSource.GetDefaultView(LadeAlleMedien());
            DataContext = DisplayView;
        }

        private void ButtonDownloader_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new YouTubeDownloader(MainWindow2, _Gast));
        }

        private void ButtonPlaylist_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Playlists(MainWindow2, _Gast));
        }

        private void ButtonMediumPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;

            if (medium == null) return;

            var fenster = new MediumPlaylistHinzufügen(medium, MainWindow2.Benutzer);
            fenster.Owner = Window.GetWindow(this);
            fenster.ShowDialog();
        }
    }
}
