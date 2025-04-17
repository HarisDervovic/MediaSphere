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
        public Startseite(MainWindow2 mainWindow2)
        {
            InitializeComponent();
            MainWindow2 = mainWindow2;
        }
    }
}
