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
using Microsoft.Win32;
using System.IO;
using System.Data.SQLite;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für MediathekErweitern.xaml
    /// </summary>
    public partial class MediathekErweitern : UserControl
    {

        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";
        private static readonly string Mediathek = System.IO.Path.Combine(appFolder, "Mediathek");

        private MainWindow2 MainWindow2;
        bool _Gast;
        public MediathekErweitern(MainWindow2 mainWindow2, bool Gast)
        {
            InitializeComponent();
            _Gast = Gast;
            MainWindow2 = mainWindow2;
            if (Gast)
            {
                ButtonPlaylist.IsEnabled = false;
            }
        }

        private void ButtonStartseite_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Startseite(MainWindow2, _Gast));
        }

        private void ButtonAbmelden_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AngemeldeterBenutzername = string.Empty;
            Properties.Settings.Default.Save();

            MainWindow login = new MainWindow();
            login.Show();
            MainWindow2.Close();
        }

        private void ButtonDateiAuswählen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Medien-Dateien (*.mp3;*.mp4)|*.mp3;*.mp4|Alle Dateien (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                TextBoxDateipfad.Text = dialog.FileName;
                TextBoxTitel.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                TextBoxDateityp.Text = System.IO.Path.GetExtension(dialog.FileName).TrimStart('.').ToLower();
            }
        }

        private void ButtonEnabledStateChange(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextBoxDateipfad.Text) && !string.IsNullOrWhiteSpace(TextBoxTitel.Text) && !string.IsNullOrWhiteSpace(TextBoxDateityp.Text) && ComboBoxKategorie.SelectedItem != null)
            {
                ButtonMediathekErweitern.IsEnabled = true;
            }
            else
            {
                ButtonMediathekErweitern.IsEnabled = false;
            }
        }

        private void ButtonMediathekErweitern_Click(object sender, RoutedEventArgs e)
        {
            string quelle = TextBoxDateipfad.Text;
            string titel = TextBoxTitel.Text;
            string typ = TextBoxDateityp.Text;
            string kategorie = (ComboBoxKategorie.SelectedItem as ComboBoxItem)?.Content.ToString();

            string dateiname = System.IO.Path.GetFileName(quelle);
            string ziel = System.IO.Path.Combine(Mediathek, dateiname);

            try
            {

                if (!File.Exists(ziel))
                {
                    File.Copy(quelle, ziel);
                }
                else
                {
                    var dialog1 = new CustomDialog("Diese Datei existiert bereits im Mediathek-Ordner.", false);
                    dialog1.Owner = Window.GetWindow(this);
                    dialog1.ShowDialog();
                    return;
                }

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = new SQLiteCommand("INSERT INTO Medien (Pfad, Typ, Titel, Kategorie) VALUES (@pfad, @typ, @titel, @kategorie)", connection);
                    command.Parameters.AddWithValue("@pfad", ziel);
                    command.Parameters.AddWithValue("@typ", typ);
                    command.Parameters.AddWithValue("@titel", titel);
                    command.Parameters.AddWithValue("@kategorie", kategorie);
                    command.ExecuteNonQuery();
                }

                var dialog = new CustomDialog("Datei erfolgreich zur Mediathek hinzugefügt!", true);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();

                TextBoxDateipfad.Text = "";
                TextBoxTitel.Text = "";
                TextBoxDateityp.Text = "";
                ComboBoxKategorie.SelectedIndex = -1;
                ButtonMediathekErweitern.IsEnabled = false;
            }
            catch (Exception ex)
            {
                var dialog = new CustomDialog($"Fehler beim Hinzufügen: {ex.Message}", false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Mediathek(MainWindow2, _Gast));
        }
    }
}
