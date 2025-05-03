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

        private void ListBoxPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ausgewaehltePlaylist = ListBoxPlaylists.SelectedItem as Playlist;
            if (ausgewaehltePlaylist == null)
                return;

            TextBlockPlaylistTitel.Text = $"📃 {ausgewaehltePlaylist.Name}";
            ListViewPlaylistMedien.ItemsSource = LadeMedienDerPlaylist(ausgewaehltePlaylist.PlaylistID);
            ListViewPlaylistMedien.Visibility = Visibility.Visible;
        }

        private List<Medium> LadeMedienDerPlaylist(int playlistId)
        {
            var medien = new List<Medium>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT m.MedienID, m.Pfad, m.Typ, m.Titel, m.Kategorie
                    FROM PlaylistMedien pm
                    INNER JOIN Medien m ON pm.MedienID = m.MedienID
                    WHERE pm.PlaylistID = @PlaylistID
                    ORDER BY pm.Reihenfolge";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PlaylistID", playlistId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            medien.Add(new Medium
                            {
                                MedienID = Convert.ToInt32(reader["MedienID"]),
                                Pfad = reader["Pfad"].ToString(),
                                Typ = reader["Typ"].ToString(),
                                Titel = reader["Titel"].ToString(),
                                Kategorie = reader["Kategorie"].ToString()
                            });
                        }
                    }
                }
            }
            return medien;
        }

        private void ButtonAbspielen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;

            if (medium == null || string.IsNullOrWhiteSpace(medium.Pfad))
                return;

            MainWindow2.StarteMedium(medium);

        }




        private void ButtonLöschen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;
            var ausgewaehltePlaylist = ListBoxPlaylists.SelectedItem as Playlist;

            if (medium == null || ausgewaehltePlaylist == null)
                return;

           
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();


                int aktuelleReihenfolge = GetReihenfolge(ausgewaehltePlaylist.PlaylistID, medium.MedienID);

                string deleteQuery = @"
                    DELETE FROM PlaylistMedien 
                    WHERE PlaylistID = @PlaylistID AND MedienID = @MedienID";

                using (var deleteCmd = new SQLiteCommand(deleteQuery, connection))
                {
                    deleteCmd.Parameters.AddWithValue("@PlaylistID", ausgewaehltePlaylist.PlaylistID);
                    deleteCmd.Parameters.AddWithValue("@MedienID", medium.MedienID);
                    deleteCmd.ExecuteNonQuery();
                }

                //Alle Medien mit höherer Reihenfolge anpassen (-1)
                string updateQuery = @"
                    UPDATE PlaylistMedien 
                    SET Reihenfolge = Reihenfolge - 1 
                    WHERE PlaylistID = @PlaylistID AND Reihenfolge > @AktuelleReihenfolge";

                using (var updateCmd = new SQLiteCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@PlaylistID", ausgewaehltePlaylist.PlaylistID);
                    updateCmd.Parameters.AddWithValue("@AktuelleReihenfolge", aktuelleReihenfolge);
                    updateCmd.ExecuteNonQuery();
                }
            }

            ListViewPlaylistMedien.ItemsSource = LadeMedienDerPlaylist(ausgewaehltePlaylist.PlaylistID); 
        }

        private void ButtonReihenfolgeHoch_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;
            var playlist = ListBoxPlaylists.SelectedItem as Playlist;

            if (medium == null || playlist == null)
                return;

            int aktuelleReihenfolge = GetReihenfolge(playlist.PlaylistID, medium.MedienID);
            if (aktuelleReihenfolge <= 1) return; // Schon ganz oben

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Finde das Medium darüber
                    string query = @"
                        SELECT MedienID FROM PlaylistMedien
                        WHERE PlaylistID = @pid AND Reihenfolge = @reihenfolge";

                    int andereMedienID = -1;
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@pid", playlist.PlaylistID);
                        command.Parameters.AddWithValue("@reihenfolge", aktuelleReihenfolge - 1);
                        var result = command.ExecuteScalar();
                        if (result == null) return;
                        andereMedienID = Convert.ToInt32(result);
                    }

                    // Vertausche die Reihenfolgen
                    string update1 = "UPDATE PlaylistMedien SET Reihenfolge = Reihenfolge + 1 WHERE PlaylistID = @pid AND MedienID = @mid";
                    using (var cmd1 = new SQLiteCommand(update1, connection))
                    {
                        cmd1.Parameters.AddWithValue("@pid", playlist.PlaylistID);
                        cmd1.Parameters.AddWithValue("@mid", andereMedienID);
                        cmd1.ExecuteNonQuery();
                    }

                    string update2 = "UPDATE PlaylistMedien SET Reihenfolge = Reihenfolge - 1 WHERE PlaylistID = @pid AND MedienID = @mid";
                    using (var cmd2 = new SQLiteCommand(update2, connection))
                    {
                        cmd2.Parameters.AddWithValue("@pid", playlist.PlaylistID);
                        cmd2.Parameters.AddWithValue("@mid", medium.MedienID);
                        cmd2.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            ListViewPlaylistMedien.ItemsSource = LadeMedienDerPlaylist(playlist.PlaylistID);
        }


        private void ButtonReihenfolgeRunter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medium = button?.DataContext as Medium;
            var playlist = ListBoxPlaylists.SelectedItem as Playlist;

            if (medium == null || playlist == null)
                return;

            int aktuelleReihenfolge = GetReihenfolge(playlist.PlaylistID, medium.MedienID);
            if (aktuelleReihenfolge <= 0) return;

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Finde das Medium darunter
                    string query = @"
                        SELECT MedienID FROM PlaylistMedien
                        WHERE PlaylistID = @pid AND Reihenfolge = @reihenfolge";

                    int andereMedienID = -1;
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@pid", playlist.PlaylistID);
                        command.Parameters.AddWithValue("@reihenfolge", aktuelleReihenfolge + 1);
                        var result = command.ExecuteScalar();
                        if (result == null) return;
                        andereMedienID = Convert.ToInt32(result);
                    }

                    // Vertausche die Reihenfolgen
                    string update1 = "UPDATE PlaylistMedien SET Reihenfolge = Reihenfolge - 1 WHERE PlaylistID = @pid AND MedienID = @mid";
                    using (var cmd1 = new SQLiteCommand(update1, connection))
                    {
                        cmd1.Parameters.AddWithValue("@pid", playlist.PlaylistID);
                        cmd1.Parameters.AddWithValue("@mid", andereMedienID);
                        cmd1.ExecuteNonQuery();
                    }

                    string update2 = "UPDATE PlaylistMedien SET Reihenfolge = Reihenfolge + 1 WHERE PlaylistID = @pid AND MedienID = @mid";
                    using (var cmd2 = new SQLiteCommand(update2, connection))
                    {
                        cmd2.Parameters.AddWithValue("@pid", playlist.PlaylistID);
                        cmd2.Parameters.AddWithValue("@mid", medium.MedienID);
                        cmd2.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }

            ListViewPlaylistMedien.ItemsSource = LadeMedienDerPlaylist(playlist.PlaylistID);
        }


        private int GetReihenfolge(int playlistId, int medienId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Reihenfolge FROM PlaylistMedien WHERE PlaylistID = @pid AND MedienID = @mid";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@pid", playlistId);
                    command.Parameters.AddWithValue("@mid", medienId);

                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }

        private void ButtonPlaylistSpielen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var playlist = button?.DataContext as Playlist;
            if (playlist == null)
                return;

            var medienListe = LadeMedienDerPlaylist(playlist.PlaylistID);
            if (medienListe.Count == 0)
            {
                var dialog = new CustomDialog("Diese Playlist enthält keine Medien.", false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                return;
            }

            MainWindow2.PlayPlaylist(medienListe); // Wiedergabe über MainWindow2
        }

        private void ButtonPlaylistLöschen_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var playlist = button?.DataContext as Playlist;

            if (playlist == null)
                return;

            
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string deletePlaylist = "DELETE FROM Playlist WHERE PlaylistID = @PlaylistID";
                    using (var cmd = new SQLiteCommand(deletePlaylist, connection))
                    {
                        cmd.Parameters.AddWithValue("@PlaylistID", playlist.PlaylistID);
                        cmd.ExecuteNonQuery();
                    }
                }

                LadePlaylists();
                ListViewPlaylistMedien.ItemsSource = null;
                ListViewPlaylistMedien.Visibility = Visibility.Collapsed;
                TextBlockPlaylistTitel.Text = "";
            }
            catch (Exception ex)
            {
                var dialog = new CustomDialog("Fehler beim Löschen der Playlist: " + ex.Message, false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }
    }
}
