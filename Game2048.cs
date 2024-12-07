using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Game2048
{
    public partial class Game2048Form : Form
    {
        private const int GRID_SIZE = 4;
        private const int TILE_SIZE = 100;
        private const int TILE_MARGIN = 10;
        private int[,] board;
        private Label[,] tileLabels;
        private int score;
        private Label scoreLabel;
        private Button restartButton;
        private Dictionary<int, Color> tileColors;
        private Random random;
        private System.Windows.Forms.Timer animationTimer;
        private List<(Label label, Point start, Point end, float progress)> animatingTiles;
        private bool isAnimating = false;
        private bool moved = false;

        public Game2048Form()
        {
            random = new Random();
            InitializeComponents();
            InitializeGame();
            SetupAnimation();
        }

        private void SetupAnimation()
        {
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 16; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
            animatingTiles = new List<(Label label, Point start, Point end, float progress)>();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            const float animationSpeed = 0.2f; // Скорость анимации (0.0 - 1.0)
            bool allAnimationsComplete = true;

            for (int i = 0; i < animatingTiles.Count; i++)
            {
                var (label, start, end, progress) = animatingTiles[i];
                progress += animationSpeed;

                if (progress >= 1.0f)
                {
                    label.Location = end;
                }
                else
                {
                    allAnimationsComplete = false;
                    int newX = (int)(start.X + (end.X - start.X) * progress);
                    int newY = (int)(start.Y + (end.Y - start.Y) * progress);
                    label.Location = new Point(newX, newY);
                    animatingTiles[i] = (label, start, end, progress);
                }
            }

            if (allAnimationsComplete)
            {
                animationTimer.Stop();
                isAnimating = false;
                UpdateBoard();
                if (moved)
                {
                    AddRandomTile();
                    moved = false;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (isAnimating) return true;

            bool moved = false;
            Dictionary<Point, Point> movements = new Dictionary<Point, Point>();

            switch (keyData)
            {
                case Keys.Left:
                    moved = MoveLeft(movements);
                    break;
                case Keys.Right:
                    moved = MoveRight(movements);
                    break;
                case Keys.Up:
                    moved = MoveUp(movements);
                    break;
                case Keys.Down:
                    moved = MoveDown(movements);
                    break;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }

            if (moved)
            {
                StartAnimation(movements);
                this.moved = true;
            }

            return true;
        }

        private void StartAnimation(Dictionary<Point, Point> movements)
        {
            if (movements.Count == 0)
            {
                isAnimating = false;
                if (moved)
                {
                    AddRandomTile();
                    moved = false;
                }
                return;
            }

            isAnimating = true;
            animatingTiles.Clear();
            animationTimer.Interval = 16; // ~60 FPS

            // Сначала делаем все плитки видимыми и устанавливаем их поверх других
            foreach (var movement in movements)
            {
                var startPos = movement.Key;
                var label = tileLabels[startPos.Y, startPos.X];
                label.Visible = true;
                label.BringToFront();
            }

            foreach (var movement in movements)
            {
                var startPos = movement.Key;
                var endPos = movement.Value;
                var label = tileLabels[startPos.Y, startPos.X];
                
                if (startPos != endPos)
                {
                    var startPoint = new Point(
                        startPos.X * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN,
                        startPos.Y * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN
                    );
                    var endPoint = new Point(
                        endPos.X * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN,
                        endPos.Y * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN
                    );
                    
                    animatingTiles.Add((label, startPoint, endPoint, 0));
                }
            }

            if (animatingTiles.Count > 0)
            {
                animationTimer.Start();
            }
            else
            {
                isAnimating = false;
                if (moved)
                {
                    AddRandomTile();
                    moved = false;
                }
            }
        }

        private void InitializeComponents()
        {
            this.Size = new Size(GRID_SIZE * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN + 16, 
                               GRID_SIZE * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN + 100);
            this.Text = "2048 Game";
            this.BackColor = Color.FromArgb(250, 248, 239);
            this.KeyPreview = true;

            scoreLabel = new Label
            {
                Text = "Score: 0",
                Location = new Point(TILE_MARGIN, 10),
                AutoSize = true,
                Font = new Font("Arial", 16)
            };
            this.Controls.Add(scoreLabel);

            restartButton = new Button
            {
                Text = "Restart",
                Location = new Point(this.ClientSize.Width - 100 - TILE_MARGIN, 10),
                Size = new Size(100, 30)
            };
            restartButton.Click += RestartButton_Click;
            this.Controls.Add(restartButton);

            tileColors = new Dictionary<int, Color>
            {
                { 2, Color.FromArgb(238, 228, 218) },
                { 4, Color.FromArgb(237, 224, 200) },
                { 8, Color.FromArgb(242, 177, 121) },
                { 16, Color.FromArgb(245, 149, 99) },
                { 32, Color.FromArgb(246, 124, 95) },
                { 64, Color.FromArgb(246, 94, 59) },
                { 128, Color.FromArgb(237, 207, 114) },
                { 256, Color.FromArgb(237, 204, 97) },
                { 512, Color.FromArgb(237, 200, 80) },
                { 1024, Color.FromArgb(237, 197, 63) },
                { 2048, Color.FromArgb(237, 194, 46) }
            };

            // Создаем панель для фона сетки
            Panel gridBackground = new Panel
            {
                Size = new Size(GRID_SIZE * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN,
                              GRID_SIZE * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN),
                Location = new Point(TILE_MARGIN, 50),
                BackColor = Color.FromArgb(187, 173, 160)
            };
            this.Controls.Add(gridBackground);

            // Создаем пустые ячейки
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    var emptyCell = new Label
                    {
                        Size = new Size(TILE_SIZE, TILE_SIZE),
                        Location = new Point(j * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN,
                                          i * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN),
                        BackColor = Color.FromArgb(205, 193, 180)
                    };
                    gridBackground.Controls.Add(emptyCell);
                }
            }

            // Инициализируем плитки поверх пустых ячеек
            tileLabels = new Label[GRID_SIZE, GRID_SIZE];
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    var label = new Label
                    {
                        Size = new Size(TILE_SIZE, TILE_SIZE),
                        Location = new Point(j * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN,
                                          i * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 24, FontStyle.Bold),
                        BackColor = Color.FromArgb(205, 193, 180),
                        Visible = false
                    };
                    tileLabels[i, j] = label;
                    gridBackground.Controls.Add(label);
                }
            }
        }

        private void InitializeGame()
        {
            board = new int[GRID_SIZE, GRID_SIZE];
            score = 0;
            isAnimating = false;
            AddRandomTile();
            AddRandomTile();
            UpdateBoard();
        }

        private void AddRandomTile()
        {
            var emptyCells = new List<(int, int)>();
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    if (board[i, j] == 0)
                        emptyCells.Add((i, j));
                }
            }

            if (emptyCells.Count > 0)
            {
                var index = random.Next(emptyCells.Count);
                var (row, col) = emptyCells[index];
                board[row, col] = random.NextDouble() < 0.9 ? 2 : 4;
            }
        }

        private void UpdateBoard()
        {
            scoreLabel.Text = $"Score: {score}";
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    var value = board[i, j];
                    var label = tileLabels[i, j];
                    label.Text = value > 0 ? value.ToString() : "";
                    label.BackColor = value > 0 ? tileColors.GetValueOrDefault(value, Color.FromArgb(237, 194, 46)) 
                                              : Color.FromArgb(205, 193, 180);
                    label.ForeColor = value <= 4 ? Color.FromArgb(119, 110, 101) : Color.White;
                    label.Location = new Point(j * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN,
                                           i * (TILE_SIZE + TILE_MARGIN) + TILE_MARGIN);
                    label.Visible = value > 0;
                    if (value > 0)
                    {
                        label.BringToFront();
                    }
                }
            }
        }

        private bool MoveLeft(Dictionary<Point, Point> movements)
        {
            bool moved = false;
            bool[,] merged = new bool[GRID_SIZE, GRID_SIZE];

            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 1; j < GRID_SIZE; j++)
                {
                    if (board[i, j] == 0) continue;

                    int col = j;
                    while (col > 0 && 
                          ((board[i, col - 1] == 0) || 
                           (!merged[i, col - 1] && !merged[i, col] && board[i, col - 1] == board[i, col])))
                    {
                        if (board[i, col - 1] == 0)
                        {
                            board[i, col - 1] = board[i, col];
                            board[i, col] = 0;
                            movements[new Point(col, i)] = new Point(col - 1, i);
                            moved = true;
                        }
                        else if (board[i, col - 1] == board[i, col])
                        {
                            board[i, col - 1] *= 2;
                            board[i, col] = 0;
                            score += board[i, col - 1];
                            merged[i, col - 1] = true;
                            movements[new Point(col, i)] = new Point(col - 1, i);
                            moved = true;
                            break;
                        }
                        col--;
                    }
                }
            }
            return moved;
        }

        private bool MoveRight(Dictionary<Point, Point> movements)
        {
            bool moved = false;
            bool[,] merged = new bool[GRID_SIZE, GRID_SIZE];

            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = GRID_SIZE - 2; j >= 0; j--)
                {
                    if (board[i, j] == 0) continue;

                    int col = j;
                    while (col < GRID_SIZE - 1 && 
                          ((board[i, col + 1] == 0) || 
                           (!merged[i, col + 1] && !merged[i, col] && board[i, col + 1] == board[i, col])))
                    {
                        if (board[i, col + 1] == 0)
                        {
                            board[i, col + 1] = board[i, col];
                            board[i, col] = 0;
                            movements[new Point(col, i)] = new Point(col + 1, i);
                            moved = true;
                        }
                        else if (board[i, col + 1] == board[i, col])
                        {
                            board[i, col + 1] *= 2;
                            board[i, col] = 0;
                            score += board[i, col + 1];
                            merged[i, col + 1] = true;
                            movements[new Point(col, i)] = new Point(col + 1, i);
                            moved = true;
                            break;
                        }
                        col++;
                    }
                }
            }
            return moved;
        }

        private bool MoveUp(Dictionary<Point, Point> movements)
        {
            bool moved = false;
            bool[,] merged = new bool[GRID_SIZE, GRID_SIZE];

            for (int j = 0; j < GRID_SIZE; j++)
            {
                for (int i = 1; i < GRID_SIZE; i++)
                {
                    if (board[i, j] == 0) continue;

                    int row = i;
                    while (row > 0 && 
                          ((board[row - 1, j] == 0) || 
                           (!merged[row - 1, j] && !merged[row, j] && board[row - 1, j] == board[row, j])))
                    {
                        if (board[row - 1, j] == 0)
                        {
                            board[row - 1, j] = board[row, j];
                            board[row, j] = 0;
                            movements[new Point(j, row)] = new Point(j, row - 1);
                            moved = true;
                        }
                        else if (board[row - 1, j] == board[row, j])
                        {
                            board[row - 1, j] *= 2;
                            board[row, j] = 0;
                            score += board[row - 1, j];
                            merged[row - 1, j] = true;
                            movements[new Point(j, row)] = new Point(j, row - 1);
                            moved = true;
                            break;
                        }
                        row--;
                    }
                }
            }
            return moved;
        }

        private bool MoveDown(Dictionary<Point, Point> movements)
        {
            bool moved = false;
            bool[,] merged = new bool[GRID_SIZE, GRID_SIZE];

            for (int j = 0; j < GRID_SIZE; j++)
            {
                for (int i = GRID_SIZE - 2; i >= 0; i--)
                {
                    if (board[i, j] == 0) continue;

                    int row = i;
                    while (row < GRID_SIZE - 1 && 
                          ((board[row + 1, j] == 0) || 
                           (!merged[row + 1, j] && !merged[row, j] && board[row + 1, j] == board[row, j])))
                    {
                        if (board[row + 1, j] == 0)
                        {
                            board[row + 1, j] = board[row, j];
                            board[row, j] = 0;
                            movements[new Point(j, row)] = new Point(j, row + 1);
                            moved = true;
                        }
                        else if (board[row + 1, j] == board[row, j])
                        {
                            board[row + 1, j] *= 2;
                            board[row, j] = 0;
                            score += board[row + 1, j];
                            merged[row + 1, j] = true;
                            movements[new Point(j, row)] = new Point(j, row + 1);
                            moved = true;
                            break;
                        }
                        row++;
                    }
                }
            }
            return moved;
        }

        private void CheckGameOver()
        {
            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    if (board[i, j] == 2048)
                    {
                        MessageBox.Show("Поздравляем! Вы выиграли!", "Победа!", MessageBoxButtons.OK);
                        return;
                    }
                }
            }

            bool hasEmpty = false;
            bool canMerge = false;

            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    if (board[i, j] == 0)
                    {
                        hasEmpty = true;
                    }
                    if (i < GRID_SIZE - 1 && board[i, j] == board[i + 1, j])
                    {
                        canMerge = true;
                    }
                    if (j < GRID_SIZE - 1 && board[i, j] == board[i, j + 1])
                    {
                        canMerge = true;
                    }
                }
            }

            if (!hasEmpty && !canMerge)
            {
                MessageBox.Show($"Игра окончена! Ваш счет: {score}", "Конец игры", MessageBoxButtons.OK);
                InitializeGame();
            }
        }

        private void RestartButton_Click(object sender, EventArgs e)
        {
            InitializeGame();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Game2048Form());
        }
    }
}
