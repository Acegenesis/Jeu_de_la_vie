using System.Windows;
using WpfApp1.Controllers;

namespace WpfApp1.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowController _controller;

        public MainWindow()
        {
            InitializeComponent();
            _controller = new MainWindowController(this);
        }
    }
}
