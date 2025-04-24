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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für MainWindow2.xaml
    /// </summary>
    public partial class MainWindow2 : Window
    {

        private bool isDraggingSlider = false;

        public DispatcherTimer Timer;

        public void InitMediaTimer()
        {
            if (Timer != null)
                return;

            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromMilliseconds(500);
            Timer.Tick += (s, e) =>
            {
                if (MediaPlayer.NaturalDuration.HasTimeSpan && !isDraggingSlider)
                {
                    var totalSeconds = MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                    var currentSeconds = MediaPlayer.Position.TotalSeconds;

                    if (DockPlayer.Visibility == Visibility.Visible)
                    {
                        SliderAudioProgress.Maximum = totalSeconds;
                        SliderAudioProgress.Value = currentSeconds;

                        TextBlockAktuelleZeitAudio.Text = TimeSpan.FromSeconds(currentSeconds).ToString(@"mm\:ss");
                        TextBlockVerbleibendeZeitAudio.Text = TimeSpan.FromSeconds(totalSeconds - currentSeconds).ToString(@"mm\:ss");
                    }

                    if (VideoOverlay.Visibility == Visibility.Visible)
                    {
                        SliderVideoProgress.Maximum = totalSeconds;
                        SliderVideoProgress.Value = currentSeconds;

                        TextBlockAktuelleZeitVideo.Text = TimeSpan.FromSeconds(currentSeconds).ToString(@"mm\:ss");
                        TextBlockVerbleibendeZeitVideo.Text = TimeSpan.FromSeconds(totalSeconds - currentSeconds).ToString(@"mm\:ss");
                    }
                }
            };
            Timer.Start();
        }



        public MainWindow2(string Benutzername, bool Gast)
        {
            InitializeComponent();
           
            SwitchView(new Startseite(this,Gast));
        }

        public void SwitchView(object NeueSeite)
        {
            MainContent.Content = NeueSeite;
        }

        private void CloseVideo_Click(object sender, RoutedEventArgs e)
        {
            VideoOverlay.Visibility = Visibility.Collapsed;
            MediaPlayer.Stop();

            
        }

        private void SliderMusikLautstärke_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Volume = SliderMusikLautstärke.Value / 100.0; 
            }
        }

        private void SliderVideoLautstärke_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(MediaPlayer != null)
            {
                MediaPlayer.Volume = SliderVideoLautstärke.Value / 100.0;
            }
        }

        private void SliderProgress_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDraggingSlider = true;
        }

        private void SliderProgress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDraggingSlider = false;

            if (sender is Slider slider && MediaPlayer.NaturalDuration.HasTimeSpan)
            {
                MediaPlayer.Position = TimeSpan.FromSeconds(slider.Value);
            }
        }

        private void SliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isDraggingSlider) return;

            // Echtzeit Zeitstempel aktualisierung während Dragging
            var position = TimeSpan.FromSeconds(e.NewValue);
            TextBlockAktuelleZeitAudio.Text = position.ToString(@"mm\:ss");
        }


    }
}
