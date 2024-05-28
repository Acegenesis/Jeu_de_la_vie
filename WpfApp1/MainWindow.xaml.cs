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
        // Grilles pour stocker les générations actuelle et suivante
        private bool[,] _currentGeneration;
        private bool[,] _nextGeneration;
        private int _rows;
        private int _cols;
        private const int CellSize = 20; // Taille d'une cellule en pixels
        private DispatcherTimer _timer; // Timer pour les itérations du jeu
        private int _iterations; // Compteur d'itérations
        private bool _isGameRunning; // Indicateur de l'état du jeu

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                // Charge l'état initial depuis un fichier
                LoadInitialState("C:\\Users\\maxim\\OneDrive\\Bureau\\input.txt");
                // Initialise la grille UI
                InitializeGrid();
                // Initialise le timer
                InitializeTimer();
                _isGameRunning = false; // Jeu initialement en pause
            }
            catch (Exception ex)
            {
                // Affiche un message d'erreur si le chargement échoue
                MessageBox.Show(ex.Message, "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Charge l'état initial depuis un fichier
        private void LoadInitialState(string filePath)
        {
            var lines = File.ReadAllLines(filePath); // Lit toutes les lignes du fichier

            // Vérifie si le fichier contient au moins deux lignes (une pour la taille et une pour la matrice)
            if (lines.Length < 2)
            {
                throw new Exception("Le fichier doit contenir au moins deux lignes.");
            }

            // Sépare la première ligne en deux entiers représentant les dimensions de la grille
            var size = lines[0].Split(' ').Select(int.Parse).ToArray();
            if (size.Length != 2)
            {
                throw new Exception("La première ligne du fichier doit contenir exactement deux entiers.");
            }

            _rows = size[0];
            _cols = size[1];
            _currentGeneration = new bool[_rows, _cols]; // Initialise la grille de la génération actuelle
            _nextGeneration = new bool[_rows, _cols]; // Initialise la grille de la prochaine génération

            // Vérifie si le nombre de lignes dans le fichier correspond au nombre de lignes spécifié
            if (lines.Length - 1 != _rows)
            {
                throw new Exception("Le nombre de lignes spécifié ne correspond pas au nombre de lignes dans la matrice.");
            }

            // Remplit la grille de la génération actuelle avec les données du fichier
            for (int i = 0; i < _rows; i++)
            {
                var line = lines[i + 1];
                if (line.Length != _cols)
                {
                    throw new Exception($"La longueur de la ligne {i + 2} ne correspond pas au nombre de colonnes spécifié.");
                }

                for (int j = 0; j < _cols; j++)
                {
                    if (line[j] != '0' && line[j] != '1')
                    {
                        throw new Exception($"La valeur à la ligne {i + 2}, colonne {j + 1} n'est pas valide (doit être '0' ou '1').");
                    }
                    _currentGeneration[i, j] = line[j] == '1';
                }
            }

            // Met à jour les textes des labels pour le nombre de lignes et de colonnes
            RowCountText.Text = _rows.ToString();
            ColCountText.Text = _cols.ToString();
        }

        // Initialise la grille UI
        private void InitializeGrid()
        {
            GameGrid.Rows = _rows; // Définit le nombre de lignes de la grille
            GameGrid.Columns = _cols; // Définit le nombre de colonnes de la grille
            GameGrid.Children.Clear(); // Vide la grille actuelle

            // Remplit la grille avec des cellules initialisées selon l'état initial
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    var cell = new Border
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Background = _currentGeneration[i, j] ? Brushes.Black : Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Margin = new Thickness(1) // Ajoute un espace entre les cellules
                    };
                    GameGrid.Children.Add(cell); // Ajoute la cellule à la grille
                }
            }
        }

        // Initialise le timer pour les itérations du jeu
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500); // Définit l'intervalle entre les itérations
            // Utilise le pattern Observer pour notifier l'événement `Tick`
            _timer.Tick += (s, e) => NextGeneration();
        }

        // Démarre le jeu
        private void StartGame()
        {
            if (!_isGameRunning)
            {
                _timer.Start(); // Démarre le timer
                _isGameRunning = true; // Met à jour l'état du jeu
            }
        }

        // Met le jeu en pause
        private void PauseGame()
        {
            _timer.Stop(); // Arrête le timer
            _isGameRunning = false; // Met à jour l'état du jeu
        }

        // Gestionnaire d'événement pour le bouton Play
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            
            IterationCountText.Text = _iterations.ToString();
            StartGame();
        }

        // Gestionnaire d'événement pour le bouton Pause
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

        // Calcule la prochaine génération du jeu
        private void NextGeneration()
        {
            bool isSameGeneration = true; // Indicateur si la génération est identique à la précédente

            // Utilise le parallélisme pour améliorer les performances
            Parallel.For(0, _rows, i =>
            {
                for (int j = 0; j < _cols; j++)
                {
                    int liveNeighbors = CountLiveNeighbors(i, j); // Compte les voisins vivants
                    if (_currentGeneration[i, j])
                    {
                        _nextGeneration[i, j] = liveNeighbors == 2 || liveNeighbors == 3;
                    }
                    else
                    {
                        _nextGeneration[i, j] = liveNeighbors == 3;
                    }

                    // Vérifie si la nouvelle génération est différente de l'actuelle
                    if (_nextGeneration[i, j] != _currentGeneration[i, j])
                    {
                        isSameGeneration = false;
                    }
                }
            });

            // Si la génération est la même que la précédente, arrête le jeu
            if (isSameGeneration)
            {
                PauseGame();
                MessageBox.Show("La génération n'a pas changé. Jeu terminé!", "Jeu de la Vie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Array.Copy(_nextGeneration, _currentGeneration, _currentGeneration.Length);
            UpdateGrid();

            _iterations++; // Incrémente le compteur d'itérations
            IterationCountText.Text = _iterations.ToString(); // Met à jour l'affichage du compteur
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

        // Compte les voisins vivants pour une cellule donnée
        private int CountLiveNeighbors(int row, int col)
        {
            int liveNeighbors = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue; // Ignore la cellule elle-même
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
