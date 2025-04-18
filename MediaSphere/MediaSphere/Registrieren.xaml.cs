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
using System.Security.Cryptography;
using System.Data.SQLite;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für Registrieren.xaml
    /// </summary>
    public partial class Registrieren : UserControl
    {
        private MainWindow MainWindow;
        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";
        public Registrieren(MainWindow mainwindow)
        {
            InitializeComponent();
            MainWindow = mainwindow;
        }

        private void ButtonZurück_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SwitchView(new Login(MainWindow));
        }

        private void TextBoxBenutzername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBenutzername.Text) || string.IsNullOrWhiteSpace(PasswortBox1.Password) || PasswortBox1.Password != PasswortBox2.Password)
            {
                ButtonRegistrieren.IsEnabled = false;
            }
            else
            {
                ButtonRegistrieren.IsEnabled = true;
            }
        }

        private void PasswortBox1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBenutzername.Text) || string.IsNullOrWhiteSpace(PasswortBox1.Password) || PasswortBox1.Password != PasswortBox2.Password)
            {
                ButtonRegistrieren.IsEnabled = false;
            }
            else
            {
                ButtonRegistrieren.IsEnabled = true;
            }


            if (!string.IsNullOrEmpty(PasswortBox2.Password) && PasswortBox1.Password != PasswortBox2.Password)
            {
                LabelÜbereinstimmen.Visibility = Visibility.Visible;
            }
            else
            {
                LabelÜbereinstimmen.Visibility = Visibility.Hidden;
            }
        }

        private void PasswortBox2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxBenutzername.Text) || string.IsNullOrWhiteSpace(PasswortBox1.Password) || PasswortBox1.Password != PasswortBox2.Password)
            {
                ButtonRegistrieren.IsEnabled = false;
            }
            else
            {
                ButtonRegistrieren.IsEnabled = true;
            }

            if (!string.IsNullOrEmpty(PasswortBox1.Password) && PasswortBox1.Password != PasswortBox2.Password)
            {
                LabelÜbereinstimmen.Visibility = Visibility.Visible;
            }
            else
            {
                LabelÜbereinstimmen.Visibility = Visibility.Hidden;
            }
        }



        private bool BenutzerExistiertKontrolle(string benutzername)
        {
            bool existiert = false;
            string query = "SELECT COUNT(*) FROM Benutzer WHERE Benutzername = @Benutzername";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Benutzername", benutzername);
                    existiert = Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }

            return existiert;
        }

        private void BenutzerHinzufügen(string benutzername, string passwort)
        {
            string query = "INSERT INTO Benutzer (Benutzername, Passwort) VALUES (@Benutzername, @Passwort)";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Benutzername", benutzername);
                    command.Parameters.AddWithValue("@Passwort", VERSCHLÜSSELN(passwort));
                    command.ExecuteNonQuery();
                }
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

        private void ButtonRegistrieren_Click(object sender, RoutedEventArgs e)
        {
            if (!BenutzerExistiertKontrolle(TextBoxBenutzername.Text.ToLower()))
            {
                BenutzerHinzufügen(TextBoxBenutzername.Text.ToLower(), PasswortBox1.Password);
                var dialog = new CustomDialog("Erfolgreich registriert!",true);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                MainWindow.SwitchView(new Login(MainWindow));
            }
            else
            {
                var dialog = new CustomDialog("Benutzer existiert bereits!",false);
                dialog.Owner = Window.GetWindow(this); 
                dialog.ShowDialog();
            }
        }
    }
}
