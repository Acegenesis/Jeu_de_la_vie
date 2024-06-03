using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using WpfApp1.Models;
using WpfApp1.Views;

namespace WpfApp1.Controllers
{
    public class MainWindowController
    {
        private MainWindow _view;
        private GameOfLifeModel _model;
        private DispatcherTimer _timer;

        private int _iterations;
        private bool _isGameRunning;

        // Params
        private int _cellSize = 20;
        private const int DefaultSpeed = 100;

        public MainWindowController()
        {
        }

        public MainWindowController(MainWindow view)
        {
            _view = view;
            _view.LoadButton.Click += LoadButton_Click;
            _view.PauseButton.Click += PauseButton_Click;
            _view.PlayButton.Click += PlayButton_Click;

            _model = new GameOfLifeModel("C:\\Users\\maxim\\OneDrive\\Bureau\\input.txt", this);
            InitializeGrid();

            _iterations = 0;
            _view.IterationCountText.Text = _iterations.ToString();
            _view.RowCountText.Text = _model.Rows.ToString();
            _view.ColCountText.Text = _model.Cols.ToString();

            InitializeTimer();
            _isGameRunning = false;

            PauseGame();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(DefaultSpeed);
            _timer.Tick += (s, e) => NextGeneration();
        }

        private void StartGame()
        {
            if (!_isGameRunning)
            {
                _timer.Start();
                _isGameRunning = true;
                _view.PlayButton.IsEnabled = false; // Désactiver le bouton "Play" pendant l'exécution du jeu
                _view.PauseButton.IsEnabled = true; // Activer le bouton "Pause" pendant l'exécution du jeu
            }
        }

        public void PauseGame()
        {
            if (_timer != null) // Vérifiez si le timer est initialisé avant de l'arrêter
            {
                _timer.Stop();
                _isGameRunning = false;
            }

            // Assurez-vous que _view est également initialisé avant de l'utiliser
            if (_view != null)
            {
                _view.PlayButton.IsEnabled = true;
                _view.PauseButton.IsEnabled = false;
            }
        }

        public DispatcherTimer GetTimer()
        {
            return _timer;
        }


        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            PauseGame();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            PauseGame();

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _model = new GameOfLifeModel(openFileDialog.FileName, this);
                    InitializeGrid();

                    _iterations = 0;
                    _view.IterationCountText.Text = _iterations.ToString();
                    _view.RowCountText.Text = _model.Rows.ToString();
                    _view.ColCountText.Text = _model.Cols.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NextGeneration()
        {
            _model.NextGeneration();
            UpdateGrid();
            if(_isGameRunning) {
                _iterations++;
                _view.IterationCountText.Text = _iterations.ToString();
            }
            
        }

        private void UpdateGrid()
        {
            if (_model == null) return; // Ajout d'une vérification pour s'assurer que le modèle est initialisé avant de mettre à jour la grille

            _view.GameGrid.Children.Clear();
            for (int i = 0; i < _model.Rows; i++)
            {
                for (int j = 0; j < _model.Cols; j++)
                {
                    var cell = new Border
                    {
                        Width = _cellSize,
                        Height = _cellSize,
                        Background = _model.CurrentGenerationArray[i, j] ? Brushes.Black : Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Margin = new Thickness(1)
                    };
                    _view.GameGrid.Children.Add(cell);
                }
            }
        }

        private void InitializeGrid()
        {
            _view.GameGrid.Rows = _model.Rows;
            _view.GameGrid.Columns = _model.Cols;
            UpdateGrid();
        }
    }
}
