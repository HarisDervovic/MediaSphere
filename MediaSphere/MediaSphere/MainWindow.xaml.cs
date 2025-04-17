using System.Text;
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
using System.IO;
using System.Security.Cryptography;

namespace MediaSphere
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";
        public MainWindow()
        {
            InitializeComponent();
            

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            if (!File.Exists(databaseFile))
            {
                SQLiteConnection.CreateFile(databaseFile);
                CreateTables();
            }
            
            if (!string.IsNullOrEmpty(Properties.Settings.Default.AngemeldeterBenutzername))
            {
                MainWindow2 hauptfenster = new MainWindow2(Properties.Settings.Default.AngemeldeterBenutzername);
                hauptfenster.Show();
                this.Close();
            }
            else
            {
                SwitchView(new Login(this));
            }
        }


        public void SwitchView(object NeueSeite)
        {
            MainContent.Content = NeueSeite;
        }

        private static void CreateTables()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Fremdschlüssel aktivieren
                using (var command = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                {
                    command.ExecuteNonQuery();
                }

                string createBenutzerTable = @"
                        CREATE TABLE IF NOT EXISTS Benutzer (
                            BenutzerID INTEGER PRIMARY KEY AUTOINCREMENT,
                            Benutzername TEXT NOT NULL UNIQUE,
                            Passwort TEXT NOT NULL
                        );";

                string createMedienTable = @"
                    CREATE TABLE IF NOT EXISTS Medien (
                        MedienID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Pfad TEXT NOT NULL,
                        Typ TEXT NOT NULL,
                        Titel TEXT,
                        Kategorie TEXT
                    );";

                string createPlaylistTable = @"
                CREATE TABLE IF NOT EXISTS Playlist (
                    PlaylistID INTEGER PRIMARY KEY AUTOINCREMENT,
                    BenutzerID INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Erstellungsdatum TEXT NOT NULL,
                    FOREIGN KEY (BenutzerID) REFERENCES Benutzer(BenutzerID) ON DELETE CASCADE
                );";

                string createPlaylistMedienTable = @"
                    CREATE TABLE IF NOT EXISTS PlaylistMedien (
                    PlaylistID INTEGER NOT NULL,
                    MedienID INTEGER NOT NULL,
                    Reihenfolge INTEGER,
                    PRIMARY KEY (PlaylistID, MedienID),
                    FOREIGN KEY (PlaylistID) REFERENCES Playlist(PlaylistID) ON DELETE CASCADE,
                    FOREIGN KEY (MedienID) REFERENCES Medien(MedienID) ON DELETE CASCADE
                    );";

                ExecuteQuery(createBenutzerTable);
                ExecuteQuery(createMedienTable);
                ExecuteQuery(createPlaylistTable);
                ExecuteQuery(createPlaylistMedienTable);

                
            }
        }


        private static void ExecuteQuery(string query)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}