using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;


namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für YouTubeDownloader.xaml
    /// </summary>
    public partial class YouTubeDownloader : UserControl
    {
        private MainWindow2 MainWindow2;
        bool _Gast;

        private static readonly string appFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaSphere");
        private static readonly string databaseFile = System.IO.Path.Combine(appFolder, "MediaSphere.db");
        private static readonly string connectionString = $"Data Source={databaseFile};Version=3;";
        private static readonly string Mediathek = System.IO.Path.Combine(appFolder, "Mediathek");


        private string YouTubeUrl = "";
        private string VideoId = "";

        private Process currentDownloadProcess;
        private bool downloadCancelled = false;

        public YouTubeDownloader(MainWindow2 mainwindow2, bool Gast)
        {
            InitializeComponent();
            _Gast = Gast;
            MainWindow2 = mainwindow2;
            if (Gast)
            {
                ButtonPlaylist.IsEnabled = false;
            }
        }

        private void ButtonStartseite_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Startseite(MainWindow2, _Gast));
        }

        private void ButtonMediathekErweitern_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new MediathekErweitern(MainWindow2, _Gast));
        }

        private void ButtonMediathek_Click(object sender, RoutedEventArgs e)
        {
            MainWindow2.SwitchView(new Mediathek(MainWindow2, _Gast));
        }

        private void ButtonAbmelden_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AngemeldeterBenutzername = string.Empty;
            Properties.Settings.Default.Save();

            MainWindow login = new MainWindow();
            login.Show();
            MainWindow2.Close();
        }


        private void TextBoxYouTubeLink_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TextBoxYouTubeLink.Text == "YouTube-Link hier einfügen...")
            {
                TextBoxYouTubeLink.Text = "";
                TextBoxYouTubeLink.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void TextBoxYouTubeLink_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TextBoxYouTubeLink.Text))
            {
                TextBoxYouTubeLink.Text = "YouTube-Link hier einfügen...";
                TextBoxYouTubeLink.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }


        private async void ButtonAnalyse_Click(object sender, RoutedEventArgs e)
        {
            BorderVideoInfo.Visibility = Visibility.Collapsed; 
            ButtonAnalyse.IsEnabled = false;
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("pack://application:,,,/loading.gif");
            image.EndInit();

            WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage, image);
            LoadingImage.Visibility = Visibility.Visible;

            YouTubeUrl = TextBoxYouTubeLink.Text.Trim();

            if (string.IsNullOrWhiteSpace(YouTubeUrl) || YouTubeUrl == "YouTube-Link hier einfügen...")
            {
                LoadingImage.Visibility = Visibility.Collapsed;
                var dialog = new CustomDialog("Bitte einen gültigen YouTube-Link eingeben!", false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                return;
            }

            try
            {
                string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");

                var startInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"-j {YouTubeUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    var jsonDoc = JsonDocument.Parse(output);
                    var root = jsonDoc.RootElement;

                    string title = root.GetProperty("title").GetString();
                    VideoId = root.GetProperty("id").GetString();

                    TextBoxTitel.Text = title;
                    BorderVideoInfo.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoadingImage.Visibility = Visibility.Collapsed;
                var dialog = new CustomDialog("Fehler beim Analysieren. Youtube-Link überprüfen!", false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }

            LoadingImage.Visibility = Visibility.Collapsed;
            WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage, null);
            ButtonAnalyse.IsEnabled = true;
        }

        private async void ButtonAudio_Click(object sender, RoutedEventArgs e)
        {
            await DownloadAsync(audioOnly: true);
        }

        private async void ButtonVideo_Click(object sender, RoutedEventArgs e)
        {
            await DownloadAsync(audioOnly: false);
        }

        private async Task DownloadAsync(bool audioOnly)
        {
            if (string.IsNullOrWhiteSpace(YouTubeUrl) || string.IsNullOrWhiteSpace(VideoId))
            {
                var dialog = new CustomDialog("Bitte zuerst ein Video analysieren.", false);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                return;
            }

            MainWindow2.MediaPlayer.Pause();
            MainWindow2.DockPlayer.Visibility = Visibility.Collapsed; //Damit der ButtonAbbrechen nicht verdeckt wird.

            ButtonAnalyse.IsEnabled = false;
            ButtonAudio.IsEnabled = false;
            ButtonVideo.IsEnabled = false;
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("pack://application:,,,/loading.gif");  //Damit das Gif auch wirklich läuft
            image.EndInit();

            WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage2, image);
            LoadingImage2.Visibility = Visibility.Visible;

            ButtonAbbrechen.Visibility = Visibility.Visible;

            try
            {
                string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yt-dlp.exe");


                string tempFolder = Path.Combine(Path.GetTempPath(), "MediaSphereDownloads");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
                Directory.CreateDirectory(tempFolder);

                string arguments = audioOnly
                     ? $"-f bestaudio --extract-audio --audio-format mp3 -o \"{Path.Combine(tempFolder, "%(title)s.%(ext)s")}\" {YouTubeUrl}"
                     : $"-f \"(bestvideo[ext=mp4][vcodec^=avc1][height<=720]+bestaudio[ext=m4a]/best[ext=mp4][vcodec^=avc1][height<=720]/bestvideo[ext=mp4][vcodec^=avc1]+bestaudio[ext=m4a]/best[ext=mp4][vcodec^=avc1])\" -o \"{Path.Combine(tempFolder, "%(title)s.%(ext)s")}\" {YouTubeUrl}";



                var startInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8,
                    StandardErrorEncoding = System.Text.Encoding.UTF8
                };


                currentDownloadProcess = Process.Start(startInfo); 
                await currentDownloadProcess.WaitForExitAsync();
                currentDownloadProcess = null;

                if (downloadCancelled)
                {
                    throw new OperationCanceledException("Download vom Benutzer abgebrochen.");
                }


                // Gespeicherte Datei finden und verschieben
                string downloadedFile = Directory.GetFiles(tempFolder).FirstOrDefault();
                if (downloadedFile != null)
                {
                    string extension = Path.GetExtension(downloadedFile);
                    string baseFileName = TextBoxTitel.Text.Trim();
                    string newFileName = baseFileName + extension;
                    string targetPath = Path.Combine(Mediathek, newFileName);

                    // Prüfen ob Datei schon existiert und wenn ja: "(1)", "(2)" etc anhängen
                    int count = 1;
                    while (File.Exists(targetPath))
                    {
                        newFileName = $"{baseFileName} ({count}){extension}";
                        targetPath = Path.Combine(Mediathek, newFileName);
                        count++;
                    }

                    File.Move(downloadedFile, targetPath);


                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();

                        string typ = audioOnly ? "mp3" : "mp4";
                        string kategorie = (ComboBoxKategorie.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sonstiges";

                        var command = new SQLiteCommand("INSERT INTO Medien (Pfad, Typ, Titel, Kategorie) VALUES (@Pfad, @Typ, @Titel, @Kategorie)", connection);
                        command.Parameters.AddWithValue("@Pfad", targetPath);
                        command.Parameters.AddWithValue("@Typ", typ);
                        command.Parameters.AddWithValue("@Titel", TextBoxTitel.Text.Trim());
                        command.Parameters.AddWithValue("@Kategorie", kategorie);

                        command.ExecuteNonQuery();
                    }

                    LoadingImage2.Visibility = Visibility.Collapsed;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage2, null);

                    var dialog = new CustomDialog("Download abgeschlossen!", true);
                    dialog.Owner = Window.GetWindow(this);
                    dialog.ShowDialog();

                    
                    ButtonAnalyse.IsEnabled = true;
                    ButtonAudio.IsEnabled = true;
                    ButtonVideo.IsEnabled = true;
                }
                else
                {

                    LoadingImage2.Visibility = Visibility.Collapsed;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage2, null);


                    var dialog = new CustomDialog("Download fehlgeschlagen.", false);
                    dialog.Owner = Window.GetWindow(this);
                    dialog.ShowDialog();

                    
                    ButtonAnalyse.IsEnabled = true;
                    ButtonAudio.IsEnabled = true;
                    ButtonVideo.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {

                LoadingImage2.Visibility = Visibility.Collapsed;
                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage2, null);

                if(!downloadCancelled)
                {
                    var dialog = new CustomDialog($"Fehler beim Download: {ex.Message}", false);
                    dialog.Owner = Window.GetWindow(this);
                    dialog.ShowDialog();
                }
               

                downloadCancelled = false;
                ButtonAnalyse.IsEnabled = true;
                ButtonAudio.IsEnabled = true;
                ButtonVideo.IsEnabled = true;
            }
        }

        private void ButtonAbbrechen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentDownloadProcess != null && !currentDownloadProcess.HasExited)
                {
                    currentDownloadProcess.Kill(true); // true = auch Unterprozesse killen
                    downloadCancelled = true;
                    currentDownloadProcess = null;

                    ButtonAbbrechen.Visibility = Visibility.Collapsed;
                    LoadingImage.Visibility = Visibility.Collapsed;
                    LoadingImage2.Visibility = Visibility.Collapsed;
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage, null);
                    WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingImage2, null);
                    //var dialog = new CustomDialog("Download abgebrochen.", false);
                    //dialog.Owner = Window.GetWindow(this);
                    //dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abbrechen: {ex.Message}");
            }
            finally
            {
               
                ButtonAnalyse.IsEnabled = true;
                ButtonAudio.IsEnabled = true;
                ButtonVideo.IsEnabled = true;
                
            }
        }


    }
}
