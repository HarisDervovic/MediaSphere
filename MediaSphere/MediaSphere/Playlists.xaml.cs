using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für Playlists.xaml
    /// </summary>
    public partial class Playlists : UserControl
    {
        private MainWindow2 MainWindow2;
        bool _Gast;
        int BenutzerID = 0;

        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";
        public Playlists(MainWindow2 mainwindow2, bool Gast)
        {
            InitializeComponent();
            _Gast = Gast;
            MainWindow2 = mainwindow2;
            string Benutzer = MainWindow2.Benutzer;

            
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT BenutzerID FROM Benutzer WHERE Benutzername = @Benutzername";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Benutzername", Benutzer);
                    var result = command.ExecuteScalar();

                    BenutzerID = Convert.ToInt32(result);


                }
            }
            LadePlaylists();
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

        private void ButtonMediathek_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Mediathek(MainWindow2, _Gast));
        }

        private void ButtonDownloader_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new YouTubeDownloader(MainWindow2, _Gast));
        }

        private void TextBoxNeuePlaylist_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxNeuePlaylist.Text == "Name der Playlist...")
            {
                TextBoxNeuePlaylist.Text = "";
                TextBoxNeuePlaylist.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void TextBoxNeuePlaylist_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxNeuePlaylist.Text))
            {
                TextBoxNeuePlaylist.Text = "Name der Playlist...";
                TextBoxNeuePlaylist.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void ButtonErstellen_Click(object sender, RoutedEventArgs e)
        {
            string playlistName = TextBoxNeuePlaylist.Text.Trim();

            if (string.IsNullOrWhiteSpace(playlistName) || playlistName == "Name der Playlist...")
            {
                var dialog = new CustomDialog("Bitte gib einen gültigen Namen für die Playlist ein.", false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                return;
            }
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string insert = "INSERT INTO Playlist (BenutzerID, Name, Erstellungsdatum) VALUES (@BenutzerID, @Name, @Datum)";
                    using (var command = new SQLiteCommand(insert, connection))
                    {
                        command.Parameters.AddWithValue("@BenutzerID", BenutzerID);
                        command.Parameters.AddWithValue("@Name", playlistName);
                        command.Parameters.AddWithValue("@Datum", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.ExecuteNonQuery();
                    }
                }

                // UI zurücksetzen
                TextBoxNeuePlaylist.Text = "Name der Playlist...";
                TextBoxNeuePlaylist.Foreground = new SolidColorBrush(Colors.Gray);

                // Playlists neu laden
                LadePlaylists();
            }
            catch (Exception ex)
            {
                var dialog = new CustomDialog("Fehler beim Erstellen der Playlist: " + ex.Message, false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }

        private void LadePlaylists()
        {
            ListBoxPlaylists.Items.Clear();

            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT PlaylistID, Name, Erstellungsdatum FROM Playlist WHERE BenutzerID = @BenutzerID";
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BenutzerID", BenutzerID);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Playlist playlist = new Playlist
                                {
                                    PlaylistID = Convert.ToInt32(reader["PlaylistID"]),
                                    BenutzerID = BenutzerID,
                                    Name = reader["Name"].ToString(),
                                    Erstellungsdatum = DateTime.Parse(reader["Erstellungsdatum"].ToString())
                                };

                                ListBoxPlaylists.Items.Add(playlist);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var dialog = new CustomDialog("Fehler beim Laden der Playlists: " + ex.Message, false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }

    }
}
