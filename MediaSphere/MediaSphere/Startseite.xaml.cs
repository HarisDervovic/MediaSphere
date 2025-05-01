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

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für Startseite.xaml
    /// </summary>
    public partial class Startseite : UserControl
    {
        private MainWindow2 MainWindow2;
        bool _Gast;
        public Startseite(MainWindow2 mainWindow2,bool Gast)
        {
            InitializeComponent();
            _Gast = Gast;
            MainWindow2 = mainWindow2;
            if(Gast)
            {
                ButtonPlaylist.IsEnabled = false;
            }
        }

        private void ButtonAbmelden_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AngemeldeterBenutzername = string.Empty;
            Properties.Settings.Default.Save();

            MainWindow login = new MainWindow();
            login.Show();
            MainWindow2.Close();
        }

        private void ButtonMediathekErweitern_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new MediathekErweitern(MainWindow2,_Gast));
        }

        private void ButtonPlaylist_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Playlists(MainWindow2, _Gast));
        }

        private void ButtonMediathek_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Mediathek(MainWindow2, _Gast));
        }

        private void ButtonDownloader_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new YouTubeDownloader(MainWindow2, _Gast));
        }
    }
}
