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
using System.Threading;

namespace LightImgur
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ImgurAPI imgurApi;
        public MainWindow()
        {
            InitializeComponent();
            imgurApi = new ImgurAPI();
        }

        private void buttonGetToken_Click(object sender, RoutedEventArgs e)
        {
            imgurApi.RequestTokens(textBoxPing.Text);
            textBoxPing.IsEnabled = false;
            buttonGetToken.IsEnabled = false;
            textBoxPing.Text = "";
        }

        private void buttonRegisterApp_Click(object sender, RoutedEventArgs e)
        {
            imgurApi.OpenAuthorizationPage();
            textBoxPing.IsEnabled = true;
            buttonGetToken.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Thread.EndThreadAffinity();
        }
    }
}
