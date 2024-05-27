using System;
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
        private bool[,] _currentGeneration; // Matrice représentant la génération actuelle
        private bool[,] _nextGeneration; // Matrice représentant la prochaine génération
        private int _rows; // Nombre de lignes dans la grille
        private int _cols; // Nombre de colonnes dans la grille
        private const int CellSize = 20; // Taille des cellules dans la grille
        private DispatcherTimer _timer; // Timer pour contrôler l'itération des générations
        private int _iterations; // Compteur pour le nombre d'itérations
        private bool _isGameRunning; // Indicateur pour savoir si le jeu est en cours d'exécution

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                // Méthode pour charger l'état initial de la grille depuis un fichier
                LoadInitialState("C:\\Users\\maxim\\OneDrive\\Bureau\\input.txt");
                // Initialisation de la grille graphique
                InitializeGrid();
                // Initialiser le timer mais ne pas démarrer immédiatement
                InitializeTimer();
                // Initialiser l'indicateur de jeu
                _isGameRunning = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Méthode pour charger l'état initial de la grille depuis un fichier
        private void LoadInitialState(string filePath)
        {
            var lines = File.ReadAllLines(filePath); // Lire toutes les lignes du fichier
            if (lines.Length < 2)
            {
                throw new Exception("Le fichier doit contenir au moins deux lignes.");
            }

            var size = lines[0].Split(' ').Select(int.Parse).ToArray(); // Lire la taille de la grille
            if (size.Length != 2)
            {
                throw new Exception("La première ligne du fichier doit contenir exactement deux entiers.");
            }

            _rows = size[0];
            _cols = size[1];
            _currentGeneration = new bool[_rows, _cols];
            _nextGeneration = new bool[_rows, _cols];

            if (lines.Length - 1 != _rows)
            {
                throw new Exception("Le nombre de lignes spécifié ne correspond pas au nombre de lignes dans la matrice.");
            }

            for (int i = 0; i < _rows; i++)
            {
                var line = lines[i + 1]; // Lire chaque ligne du fichier pour obtenir l'état des cellules
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
            // Mettre à jour les TextBlock pour afficher le nombre de lignes et de colonnes
            RowCountText.Text = _rows.ToString();
            ColCountText.Text = _cols.ToString();
        }

        private void InitializeGrid()
        {
            GameGrid.Rows = _rows; // Définir le nombre de lignes dans la grille
            GameGrid.Columns = _cols; // Définir le nombre de colonnes dans la grille
            GameGrid.Children.Clear();

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
                        Margin = new Thickness(1) // Ajouter un espacement de 1 pixel autour de chaque cellule
                    };
                    GameGrid.Children.Add(cell); // Ajouter la cellule à la grille
                }
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500); // Définir l'intervalle de temps pour chaque génération
            _timer.Tick += (s, e) => NextGeneration(); // Définir l'événement à exécuter à chaque tick du timer
        }

        private void StartGame()
        {
            if (!_isGameRunning)
            {
                _timer.Start(); // Démarrer le timer
                _isGameRunning = true; // Mettre à jour l'indicateur de jeu
            }
        }

        private void PauseGame()
        {
            _timer.Stop(); // Arrêter le timer
            _isGameRunning = false; // Mettre à jour l'indicateur de jeu
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
            bool isSameGeneration = true;

            Parallel.For(0, _rows, i => // Parallélisation du calcul de la prochaine génération
            {
                for (int j = 0; j < _cols; j++)
                {
                    int liveNeighbors = CountLiveNeighbors(i, j); // Compter les voisins vivants
                    if (_currentGeneration[i, j])
                    {
                        _nextGeneration[i, j] = liveNeighbors == 2 || liveNeighbors == 3; // Règle de survie
                    }
                    else
                    {
                        _nextGeneration[i, j] = liveNeighbors == 3; // Règle de naissance
                    }

                    if (_nextGeneration[i, j] != _currentGeneration[i, j])
                    {
                        isSameGeneration = false; // Détecter un changement dans la génération
                    }
                }
            });

            if (isSameGeneration)
            {
                PauseGame();
                MessageBox.Show("La génération n'a pas changé. Jeu terminé!", "Jeu de la Vie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Mettre à jour la grille et échanger les générations
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    var cell = (Border)GameGrid.Children[i * _cols + j]; // Récupérer la cellule dans la grille
                    cell.Background = _nextGeneration[i, j] ? Brushes.Black : Brushes.White; // Mettre à jour la couleur de la cellule
                    _currentGeneration[i, j] = _nextGeneration[i, j]; // Mettre à jour la génération actuelle
                }
            }

            _iterations++; // Incrémenter le compteur d'itérations
            IterationCountText.Text = _iterations.ToString(); // Mettre à jour le texte affichant le nombre d'itérations
        }

        // Méthode pour compter les voisins vivants d'une cellule
        private int CountLiveNeighbors(int row, int col)
        {
            int liveNeighbors = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;  // Ignorer la cellule elle-même
                    int r = row + i;
                    int c = col + j;
                    if (r >= 0 && r < _rows && c >= 0 && c < _cols) // Vérifier si le voisin est dans les limites de la grille
                    {
                        if (_currentGeneration[r, c]) liveNeighbors++; // Compter le voisin s'il est vivant
                    }
                }
            }
            return liveNeighbors; // Retourner le nombre de voisins vivants
        }
    }
}
