﻿using System;
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

namespace MediaSphere
{
    /// <summary>
    /// Interaktionslogik für CustomDialog.xaml
    /// </summary>
    public partial class CustomDialog : Window
    {
        public CustomDialog(string message, bool Erfolg)
        {
            InitializeComponent();
            TextMessage.Text = message;
            if(Erfolg)
            {
                IconErfolg.Visibility = Visibility.Visible;
            }
            else
            {
                IconWarnung.Visibility = Visibility.Visible;
            }
        }

        private void DialogBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }


        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
