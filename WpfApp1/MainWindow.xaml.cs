using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // Matrices generations
        private bool[,] _currentGeneration;
        private bool[,] _nextGeneration;

        private int _rows;
        private int _cols;

        private DispatcherTimer _timer;
        private int _iterations;
        private bool _isGameRunning;

        // Parametres du jeu
        private const int _CellSize = 20;
        private const int _Speed = 500;


        public MainWindow()
        {
            InitializeComponent();
            try
            {
                LoadInitialState("C:\\Users\\maxim\\OneDrive\\Bureau\\input.txt");

                InitializeGrid();
                InitializeTimer();
                _isGameRunning = false; // Mettre en pause
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Charge l'état initial depuis un fichier
        private void LoadInitialState(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            // Check si fichier comporte au moins deux lignes
            if (lines.Length < 2)
            {
                throw new Exception("Le fichier doit contenir au moins deux lignes.");
            }

            // Split la 1ere ligne en 2 entiers -> 2 dimensions
            var size = lines[0].Split(' ').Select(int.Parse).ToArray();
            if (size.Length != 2)
            {
                throw new Exception("La première ligne du fichier doit contenir exactement deux entiers.");
            }
            _rows = size[0];
            _cols = size[1];

            // Initialise les generations
            _currentGeneration = new bool[_rows, _cols];
            _nextGeneration = new bool[_rows, _cols]; 

            // Check si le fichier comporte le bon nombre de lignes
            if (lines.Length - 1 != _rows)
            {
                throw new Exception("Le nombre de lignes spécifié ne correspond pas au nombre de lignes dans la matrice.");
            }

            // Utiliser fichier pour remplir la matrice
            for (int i = 0; i < _rows; i++)
            {
                var line = lines[i + 1];

                // Gestion des erreurs
                if (line.Length != _cols)
                {
                    throw new Exception($"La longueur de la ligne {i + 2} ne correspond pas au nombre de colonnes spécifié.");
                }

                for (int j = 0; j < _cols; j++)
                {
                    // Gestion des erreurs
                    if (line[j] != '0' && line[j] != '1')
                    {
                        throw new Exception($"La valeur à la ligne {i + 2}, colonne {j + 1} n'est pas valide (doit être '0' ou '1').");
                    }

                    _currentGeneration[i, j] = line[j] == '1';
                }
            }

            // MAJ des textbox
            RowCountText.Text = _rows.ToString();
            ColCountText.Text = _cols.ToString();
        }

        private void InitializeGrid()
        {
            // Def param grille
            GameGrid.Rows = _rows; 
            GameGrid.Columns = _cols;
            GameGrid.Children.Clear();

            // Remplir la grille
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    var cell = new Border
                    {
                        Width = _CellSize,
                        Height = _CellSize,
                        Background = _currentGeneration[i, j] ? Brushes.Black : Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Margin = new Thickness(1)
                    };

                    GameGrid.Children.Add(cell);
                }
            }
        }

        // Initialise timer
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();

            // Intervalle entre les itérations
            _timer.Interval = TimeSpan.FromMilliseconds(_Speed);

            // Pattern Observer pour l'événement `Tick`
            _timer.Tick += (s, e) => NextGeneration();
        }

        private void StartGame()
        {
            if (!_isGameRunning)
            {
                _timer.Start();
                _isGameRunning = true;
            }
        }

        private void PauseGame()
        {
            _timer.Stop();
            _isGameRunning = false;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            
            IterationCountText.Text = _iterations.ToString();
            StartGame();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            PauseGame();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            PauseGame();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadInitialState(openFileDialog.FileName);
                    InitializeGrid();

                    _iterations = 0;
                    IterationCountText.Text = _iterations.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NextGeneration()
        {
            // Indicateur si generation i et i-1 sont identique
            bool isSameGeneration = true; 

            // Utilise parallelisme
            Parallel.For(0, _rows, i =>
            {
                for (int j = 0; j < _cols; j++)
                {
                    int liveNeighbors = CountLiveNeighbors(i, j);

                    if (_currentGeneration[i, j]) {
                        _nextGeneration[i, j] = liveNeighbors == 2 || liveNeighbors == 3;
                    } else {
                        _nextGeneration[i, j] = liveNeighbors == 3;
                    }

                    // Check si generation i et i-1 sont identique pour une coordonnée
                    if (_nextGeneration[i, j] != _currentGeneration[i, j])
                    {
                        isSameGeneration = false;
                    }
                }
            });

            // Check si generation i et i-1 sont identique
            if (isSameGeneration)
            {
                PauseGame();
                MessageBox.Show("La génération n'a pas changé. Jeu terminé!", "Jeu de la Vie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // copy _next -> _current
            Array.Copy(_nextGeneration, _currentGeneration, _currentGeneration.Length);
            UpdateGrid();

            _iterations++;
            IterationCountText.Text = _iterations.ToString();
        }

        private void UpdateGrid()
        {
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    var cell = (Border)GameGrid.Children[i * _cols + j];
                    cell.Background = _currentGeneration[i, j] ? Brushes.Black : Brushes.White;
                }
            }
        }

        private int CountLiveNeighbors(int row, int col)
        {
            int liveNeighbors = 0;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    // Ignore la celle actuelle
                    if (i == 0 && j == 0) continue; 

                    int r = row + i;
                    int c = col + j;

                    if (r >= 0 && r < _rows && c >= 0 && c < _cols)
                    {
                        if (_currentGeneration[r, c]) liveNeighbors++;
                    }
                }
            }
            return liveNeighbors;
        }
    }
}
