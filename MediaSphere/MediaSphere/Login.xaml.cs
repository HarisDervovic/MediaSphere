using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für Login.xaml
    /// </summary>
    public partial class Login : UserControl
    {
        private MainWindow MainWindow;
        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";

        public Login(MainWindow mainWindow)
        {
            InitializeComponent();
            MainWindow = mainWindow;
        }

        private void ButtonRegistrieren_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SwitchView(new Registrieren(MainWindow));
        }

        private void ButtonAnmelden_Click(object sender, RoutedEventArgs e)
        {
            string Benutzername = TextBoxBenutzername.Text;
            string Passwort = PasswortBox.Password;

            if (AnmeldedatenÜberprüfen(Benutzername.ToLower(), Passwort))
            {
                if (CheckBoxAngemeldetBleiben.IsChecked == true)
                {
                    Properties.Settings.Default.AngemeldeterBenutzername = Benutzername;
                }
                else
                {
                    Properties.Settings.Default.AngemeldeterBenutzername = string.Empty;
                }

                Properties.Settings.Default.Save();

                MainWindow2 mainWindow2 = new MainWindow2(Benutzername, false);
                mainWindow2.Show();
                MainWindow.Close();
                
            }
            else
            {
                LabelFalscheDaten.Visibility = Visibility.Visible;
            }
        }

        private bool AnmeldedatenÜberprüfen(string Benutzername, string Passwort)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                SQLiteCommand command = new SQLiteCommand("SELECT COUNT(*) FROM Benutzer WHERE Benutzername = @username AND Passwort = @password", connection);
                command.Parameters.AddWithValue("@username", Benutzername);
                command.Parameters.AddWithValue("@password", VERSCHLÜSSELN(Passwort));

                int result = Convert.ToInt32(command.ExecuteScalar());
                return result > 0;
            }
        }

        public static string VERSCHLÜSSELN(string Passwort)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Passwort));
                StringBuilder PasswortVerschlüsselt = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    PasswortVerschlüsselt.Append(bytes[i].ToString("x2"));
                }
                return PasswortVerschlüsselt.ToString();
            }
        }

        private void ButtonGast_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2 mainWindow2 = new MainWindow2("Gast",true);
            mainWindow2.Show();
            MainWindow.Close();
        }
    }
}
