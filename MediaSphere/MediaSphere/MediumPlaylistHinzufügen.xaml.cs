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
using System.Windows.Shapes;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für MediumPlaylistHinzufügen.xaml
    /// </summary>
    /// 

    public partial class MediumPlaylistHinzufügen : Window
    {
        private static string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static string connectionString = $"Data Source={databaseFile};Version=3;";
        Medium medium;
        public MediumPlaylistHinzufügen(Medium _medium, string Benutzer)
        {
            InitializeComponent();
            medium = _medium;
            ComboBoxPlaylists.ItemsSource = LadePlaylistsFürBenutzer(Benutzer);
            ComboBoxPlaylists.DisplayMemberPath = "Name";
        }

        private void ButtonHinzufuegen_Click(object sender, RoutedEventArgs e)
        {
            var ausgewaehltePlaylist = ComboBoxPlaylists.SelectedItem as Playlist;

            if (ausgewaehltePlaylist == null)
            {
                var dialog = new CustomDialog("Bitte wähle eine Playlist aus.", false);
                dialog.Owner = this;
                dialog.ShowDialog();
                return;
            }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Prüfen ob Medium schon in der Playlist ist
                string checkQuery = @"
            SELECT COUNT(*) 
            FROM PlaylistMedien 
            WHERE PlaylistID = @PlaylistID AND MedienID = @MedienID";

                using (var checkCommand = new SQLiteCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@PlaylistID", ausgewaehltePlaylist.PlaylistID);
                    checkCommand.Parameters.AddWithValue("@MedienID", medium.MedienID);

                    long count = (long)checkCommand.ExecuteScalar();
                    if (count > 0)
                    {
                        var dialog = new CustomDialog("Dieses Medium ist bereits in der Playlist.", false);
                        dialog.Owner = this;
                        dialog.ShowDialog();
                        return;
                    }
                }

                // Reihenfolge = max + 1
                int neueReihenfolge = 1;
                string maxQuery = @"
                    SELECT COALESCE(MAX(Reihenfolge), 0) 
                    FROM PlaylistMedien 
                    WHERE PlaylistID = @PlaylistID"; //COALESCE damit es bei null Einträgen nicht zu Fehlern kommt.

                using (var maxCommand = new SQLiteCommand(maxQuery, connection))
                {
                    maxCommand.Parameters.AddWithValue("@PlaylistID", ausgewaehltePlaylist.PlaylistID);
                    neueReihenfolge = Convert.ToInt32(maxCommand.ExecuteScalar()) + 1;
                }

                // Medium zur Playlist hinzufügen
                string insertQuery = @"
                    INSERT INTO PlaylistMedien (PlaylistID, MedienID, Reihenfolge)
                    VALUES (@PlaylistID, @MedienID, @Reihenfolge)";

                using (var insertCommand = new SQLiteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@PlaylistID", ausgewaehltePlaylist.PlaylistID);
                    insertCommand.Parameters.AddWithValue("@MedienID", medium.MedienID);
                    insertCommand.Parameters.AddWithValue("@Reihenfolge", neueReihenfolge);
                    insertCommand.ExecuteNonQuery();
                }

                var successDialog = new CustomDialog("Medium erfolgreich zur Playlist hinzugefügt.", true);
                successDialog.Owner = this;
                successDialog.ShowDialog();
                this.Close();
            }
        }


        private void ButtonAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private List<Playlist> LadePlaylistsFürBenutzer(string benutzername)
        {
            var playlists = new List<Playlist>();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string query = @"
                    SELECT p.PlaylistID, p.BenutzerID, p.Name, p.Erstellungsdatum
                    FROM Playlist p
                    INNER JOIN Benutzer b ON p.BenutzerID = b.BenutzerID
                    WHERE b.Benutzername = @Benutzername";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Benutzername", benutzername);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            playlists.Add(new Playlist
                            {
                                PlaylistID = Convert.ToInt32(reader["PlaylistID"]),
                                BenutzerID = Convert.ToInt32(reader["BenutzerID"]),
                                Name = reader["Name"].ToString(),
                                Erstellungsdatum = DateTime.Parse(reader["Erstellungsdatum"].ToString())
                            });
                        }
                    }
                }
            }

            return playlists;
        }

    }
}
