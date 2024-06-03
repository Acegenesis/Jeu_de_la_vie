using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Controllers;

namespace WpfApp1.Models
{
    public class GameOfLifeModel
    {
        public bool[,] CurrentGenerationArray { get; private set; }
        public bool[,] NextGenerationArray { get; private set; }
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        private bool isSameGeneration = false;

        private MainWindowController _controller;

        public GameOfLifeModel(MainWindowController controller)
        {
            _controller = controller;
        }

        public GameOfLifeModel(string filePath, MainWindowController controller)
        {
            _controller = controller;
            LoadInitialState(filePath);
        }


        public void LoadInitialState(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);

                if (lines.Length < 2) throw new Exception("Le fichier doit contenir au moins deux lignes.");

                var size = lines[0].Split(' ').Select(int.Parse).ToArray();
                if (size.Length != 2) throw new Exception("La première ligne du fichier doit contenir exactement deux entiers.");
                Rows = size[0];
                Cols = size[1];

                CurrentGenerationArray = new bool[Rows, Cols];
                NextGenerationArray = new bool[Rows, Cols];

                if (lines.Length - 1 != Rows) throw new Exception("Le nombre de lignes spécifié ne correspond pas au nombre de lignes dans la matrice.");

                for (int i = 0; i < Rows; i++)
                {
                    var line = lines[i + 1];

                    if (line.Length != Cols) throw new Exception($"La longueur de la ligne {i + 2} ne correspond pas au nombre de colonnes spécifié.");

                    for (int j = 0; j < Cols; j++)
                    {
                        if (line[j] != '0' && line[j] != '1') throw new Exception($"La valeur à la ligne {i + 2}, colonne {j + 1} n'est pas valide (doit être '0' ou '1').");

                        CurrentGenerationArray[i, j] = line[j] == '1';
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du fichier : {ex.Message}", "Erreur de chargement", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void NextGeneration()
        {
            isSameGeneration = true;

            Parallel.For(0, Rows, i =>
            {
                for (int j = 0; j < Cols; j++)
                {
                    int liveNeighbors = CountLiveNeighbors(i, j);

                    if (CurrentGenerationArray[i, j])
                        NextGenerationArray[i, j] = liveNeighbors == 2 || liveNeighbors == 3;
                    else
                        NextGenerationArray[i, j] = liveNeighbors == 3;

                    if (NextGenerationArray[i, j] != CurrentGenerationArray[i, j])
                        isSameGeneration = false;
                }
            });

            if (isSameGeneration)
            {
                _controller.PauseGame();
                MessageBox.Show("La génération n'a pas changé. Jeu terminé!", "Jeu de la Vie", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Array.Copy(NextGenerationArray, CurrentGenerationArray, CurrentGenerationArray.Length);
        }

        private int CountLiveNeighbors(int row, int col)
        {
            int liveNeighbors = 0;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    int r = row + i;
                    int c = col + j;

                    if (r >= 0 && r < Rows && c >= 0 && c < Cols && CurrentGenerationArray[r, c])
                        liveNeighbors++;
                }
            }
            return liveNeighbors;
        }
    }
}
