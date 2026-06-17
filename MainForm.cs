using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnakeGameWinForms;

public sealed class MainForm : Form
{
    private const int Columns = 30;
    private const int Rows = 24;
    private const int BoardPadding = 20;
    private const int StartingInterval = 175;
    private const int FastestInterval = 46;
    private const int LoadingMilliseconds = 3500;

    private readonly GameEngine _game = new(Columns, Rows);
    private readonly SaveDataStore _saveDataStore = new();
    private readonly SoundService _sound = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly DoubleBufferedPanel _boardPanel = new();
    private readonly Label _scoreLabel = new();
    private readonly Label _levelLabel = new();
    private readonly Label _highScoreLabel = new();
    private readonly Label _lastScoreLabel = new();
    private readonly Label _gamesLabel = new();
    private readonly Label _foodLabel = new();
    private readonly Label _comboLabel = new();
    private readonly Label _speedLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Button _startButton = new();
    private readonly Button _pauseButton = new();
    private readonly Button _soundButton = new();
    private PlayerData _playerData;
    private DateTime _playStartedUtc;
    private bool _paused = true;
    private bool _hasStarted;
    private bool _isLoading;

    public MainForm()
    {
        _playerData = _saveDataStore.Load();

        Text = "Snake Game - C# WinForms";
        ClientSize = new Size(1040, 740);
        MinimumSize = new Size(860, 640);
        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        DoubleBuffered = true;
        KeyPreview = true;

        BuildUi();
        UpdateHud();

        _timer.Interval = StartingInterval;
        _timer.Tick += (_, _) => RunFrame();
        Shown += (_, _) => Focus();
    }

    private void BuildUi()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(15, 23, 42)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        TableLayoutPanel header = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(18, 10, 18, 10),
            ColumnCount = 2,
            RowCount = 1
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(header, 0, 0);

        BrandPanel brandPanel = new()
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        header.Controls.Add(brandPanel, 0, 0);

        TableLayoutPanel headerRight = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        headerRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        headerRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        header.Controls.Add(headerRight, 1, 0);

        _statusLabel.Text = "Press Start or Space | Move with WASD";
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Color.FromArgb(203, 213, 225);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.AutoEllipsis = true;
        headerRight.Controls.Add(_statusLabel, 0, 0);

        TableLayoutPanel commandRow = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0)
        };
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146));
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        headerRight.Controls.Add(commandRow, 0, 1);

        Label helpLabel = new()
        {
            Text = "WASD move | Space pause/restart | Red dot moves after 6 sec | 5x combo unlocks +50 food",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        commandRow.Controls.Add(helpLabel, 0, 0);

        _startButton.Text = "Start / Restart";
        StyleButton(_startButton, Color.FromArgb(22, 101, 52), Color.FromArgb(34, 197, 94));
        _startButton.Click += (_, _) => StartNewGame();
        commandRow.Controls.Add(_startButton, 1, 0);

        _pauseButton.Text = "Pause";
        StyleButton(_pauseButton, Color.FromArgb(51, 65, 85), Color.FromArgb(148, 163, 184));
        _pauseButton.Click += (_, _) => TogglePause();
        commandRow.Controls.Add(_pauseButton, 2, 0);

        _soundButton.Text = "Sound";
        StyleButton(_soundButton, Color.FromArgb(88, 28, 135), Color.FromArgb(192, 132, 252));
        _soundButton.Click += (_, _) => ToggleSound();
        commandRow.Controls.Add(_soundButton, 3, 0);

        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(18),
            BackColor = Color.FromArgb(15, 23, 42)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.Controls.Add(content, 0, 1);

        _boardPanel.Dock = DockStyle.Fill;
        _boardPanel.BackColor = Color.FromArgb(15, 23, 42);
        _boardPanel.Paint += BoardPanel_Paint;
        _boardPanel.Resize += (_, _) => _boardPanel.Invalidate();
        content.Controls.Add(_boardPanel, 0, 0);

        TableLayoutPanel sidebar = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(17, 24, 39),
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 10,
            Margin = new Padding(16, 0, 0, 0)
        };
        for (int i = 0; i < 8; i++)
        {
            sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        }
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(sidebar, 1, 0);

        Label statsTitle = new()
        {
            Text = "Game Data",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft
        };
        sidebar.Controls.Add(statsTitle, 0, 0);

        AddStatLabel(sidebar, _scoreLabel, 1, Color.FromArgb(250, 204, 21));
        AddStatLabel(sidebar, _levelLabel, 2, Color.FromArgb(125, 211, 252));
        AddStatLabel(sidebar, _speedLabel, 3, Color.FromArgb(251, 146, 60));
        AddStatLabel(sidebar, _foodLabel, 4, Color.FromArgb(248, 113, 113));
        AddStatLabel(sidebar, _comboLabel, 5, Color.FromArgb(74, 222, 128));
        AddStatLabel(sidebar, _highScoreLabel, 6, Color.FromArgb(196, 181, 253));
        AddStatLabel(sidebar, _lastScoreLabel, 7, Color.FromArgb(203, 213, 225));
        AddStatLabel(sidebar, _gamesLabel, 8, Color.FromArgb(203, 213, 225));
    }

    private static void AddStatLabel(TableLayoutPanel row, Label label, int rowIndex, Color color)
    {
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(0, 0, 0, 8);
        label.Padding = new Padding(12, 0, 12, 0);
        label.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        label.ForeColor = color;
        label.BackColor = Color.FromArgb(30, 41, 59);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoEllipsis = true;
        row.Controls.Add(label, 0, rowIndex);
    }

    private static void StyleButton(Button button, Color background, Color border)
    {
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(6, 3, 0, 3);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = border;
        button.ForeColor = Color.White;
        button.BackColor = background;
        button.TabStop = false;
    }

    private async void StartNewGame()
    {
        if (_isLoading)
        {
            return;
        }

        _timer.Stop();
        _game.Reset();
        _paused = true;
        _hasStarted = false;
        _isLoading = true;
        _startButton.Enabled = false;
        _pauseButton.Enabled = false;
        _statusLabel.Text = "Loading game... Developed By Ali Zain Khan";
        _pauseButton.Text = "Pause";
        UpdateHud();
        _sound.PlayStart();
        _boardPanel.Invalidate();
        Focus();

        await Task.Delay(LoadingMilliseconds);

        if (IsDisposed)
        {
            return;
        }

        _playStartedUtc = DateTime.UtcNow;
        _isLoading = false;
        _paused = false;
        _hasStarted = true;
        _startButton.Enabled = true;
        _pauseButton.Enabled = true;
        _timer.Interval = GetIntervalForCurrentSpeed();
        _timer.Start();
        _statusLabel.Text = "Playing | Move with WASD | Space pauses";
        _pauseButton.Text = "Pause";
        UpdateHud();
        _boardPanel.Invalidate();
        Focus();
    }

    private void TogglePause()
    {
        if (_isLoading || !_hasStarted || _game.IsGameOver)
        {
            return;
        }

        _paused = !_paused;
        _timer.Enabled = !_paused;
        _pauseButton.Text = _paused ? "Resume" : "Pause";
        _statusLabel.Text = _paused ? "Paused | Space resumes" : "Playing | Move with WASD | Space pauses";
        _sound.PlayPause();
        _boardPanel.Invalidate();
        Focus();
    }

    private void ToggleSound()
    {
        _sound.Enabled = !_sound.Enabled;
        _soundButton.Text = _sound.Enabled ? "Sound" : "Muted";
        _statusLabel.Text = _sound.Enabled ? "Sound on" : "Sound muted";
        if (_sound.Enabled)
        {
            _sound.PlayStart();
        }

        Focus();
    }

    private void RunFrame()
    {
        DateTime now = DateTime.UtcNow;
        if (_game.HasFoodExpired(now))
        {
            _game.MoveFoodAfterMiss();
            _statusLabel.Text = "Missed food - combo reset";
        }

        int previousLevel = _game.Level;
        StepResult result = _game.Step();

        if (result == StepResult.AteFood || result == StepResult.BoardCleared)
        {
            _sound.PlayEat();
            _statusLabel.Text = _game.IsBonusFood ? "5x combo! Bonus food ready: +50" : $"Food eaten: +{_game.LastPointsEarned}";
        }

        int nextInterval = GetIntervalForCurrentSpeed();
        if (_timer.Interval != nextInterval)
        {
            _timer.Interval = nextInterval;
        }

        if (_game.Level > previousLevel)
        {
            _statusLabel.Text = $"Level {_game.Level}! Speed increased";
            _sound.PlayLevelUp();
        }

        UpdateHud();

        if (result == StepResult.GameOver || result == StepResult.BoardCleared)
        {
            FinishGame(result);
        }

        _boardPanel.Invalidate();
    }

    private void FinishGame(StepResult result)
    {
        _timer.Stop();
        _paused = true;
        _pauseButton.Text = "Pause";

        _playerData.TotalGames++;
        _playerData.LastScore = _game.Score;
        bool newRecord = _game.Score > _playerData.HighScore;
        if (newRecord)
        {
            _playerData.HighScore = _game.Score;
        }

        _saveDataStore.Save(_playerData);
        UpdateHud();

        if (result == StepResult.BoardCleared)
        {
            _statusLabel.Text = "You cleared the board - saved!";
            _sound.PlayLevelUp();
        }
        else
        {
            _statusLabel.Text = newRecord ? "New high score saved!" : "Game over - score saved";
            _sound.PlayGameOver();
        }
    }

    private void UpdateHud()
    {
        DateTime now = DateTime.UtcNow;
        int shownBest = Math.Max(_playerData.HighScore, _game.Score);
        int speedPercent = Math.Clamp((StartingInterval - GetIntervalForCurrentSpeed()) * 100 / (StartingInterval - FastestInterval), 0, 100);
        _scoreLabel.Text = $"Score: {_game.Score}";
        _levelLabel.Text = $"Level: {_game.Level}";
        _speedLabel.Text = $"Speed: {speedPercent}%";
        _foodLabel.Text = $"Food: +{_game.FoodValue}  {_game.GetFoodSecondsRemaining(now):0.0}s";
        _comboLabel.Text = _game.IsBonusFood ? "Combo: BONUS" : $"Combo: {_game.ComboCount}/5";
        _highScoreLabel.Text = $"Best: {shownBest}";
        _lastScoreLabel.Text = $"Last: {_playerData.LastScore}";
        _gamesLabel.Text = $"Games: {_playerData.TotalGames}";
    }

    private int GetIntervalForCurrentSpeed()
    {
        if (!_hasStarted)
        {
            return StartingInterval;
        }

        int elapsedSeconds = Math.Max(0, (int)(DateTime.UtcNow - _playStartedUtc).TotalSeconds);
        int timeBoost = elapsedSeconds / 10 * 7;
        int scoreBoost = (_game.Level - 1) * 4;
        return Math.Max(FastestInterval, StartingInterval - timeBoost - scoreBoost);
    }

    private void BoardPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle board = GetBoardRectangle();

        using SolidBrush boardBrush = new(Color.FromArgb(2, 6, 23));
        using Pen borderPen = new(Color.FromArgb(226, 232, 240), 3);
        e.Graphics.FillRectangle(boardBrush, board);
        e.Graphics.DrawRectangle(borderPen, board);

        DrawGrid(e.Graphics, board);
        DrawFood(e.Graphics, board);
        DrawSnake(e.Graphics, board);

        if (_isLoading || !_hasStarted || _paused || _game.IsGameOver)
        {
            DrawOverlay(e.Graphics, board);
        }
    }

    private Rectangle GetBoardRectangle()
    {
        int availableWidth = Math.Max(1, _boardPanel.ClientSize.Width - BoardPadding * 2);
        int availableHeight = Math.Max(1, _boardPanel.ClientSize.Height - BoardPadding * 2);
        int cellSize = Math.Max(8, Math.Min(availableWidth / Columns, availableHeight / Rows));
        int width = cellSize * Columns;
        int height = cellSize * Rows;
        int left = (_boardPanel.ClientSize.Width - width) / 2;
        int top = (_boardPanel.ClientSize.Height - height) / 2;
        return new Rectangle(left, top, width, height);
    }

    private static Rectangle CellRectangle(Point cell, Rectangle board)
    {
        int cellSize = board.Width / Columns;
        int margin = Math.Max(2, cellSize / 8);
        return new Rectangle(
            board.Left + cell.X * cellSize + margin,
            board.Top + cell.Y * cellSize + margin,
            cellSize - margin * 2,
            cellSize - margin * 2);
    }

    private void DrawGrid(Graphics graphics, Rectangle board)
    {
        int cellSize = board.Width / Columns;
        using Pen gridPen = new(Color.FromArgb(30, 41, 59), 1);

        for (int x = 1; x < Columns; x++)
        {
            int px = board.Left + x * cellSize;
            graphics.DrawLine(gridPen, px, board.Top + 1, px, board.Bottom - 1);
        }

        for (int y = 1; y < Rows; y++)
        {
            int py = board.Top + y * cellSize;
            graphics.DrawLine(gridPen, board.Left + 1, py, board.Right - 1, py);
        }
    }

    private void DrawFood(Graphics graphics, Rectangle board)
    {
        Rectangle food = CellRectangle(_game.Food, board);
        Color foodColor = _game.IsBonusFood ? Color.FromArgb(250, 204, 21) : Color.FromArgb(239, 68, 68);
        using SolidBrush foodBrush = new(foodColor);
        using Pen ringPen = new(Color.White, _game.IsBonusFood ? 3 : 1);
        graphics.FillEllipse(foodBrush, food);
        graphics.DrawEllipse(ringPen, food);

        if (_game.IsBonusFood && food.Width >= 18)
        {
            using Font bonusFont = new("Segoe UI", Math.Max(7, food.Width / 4F), FontStyle.Bold);
            using SolidBrush textBrush = new(Color.FromArgb(17, 24, 39));
            string text = "50";
            SizeF size = graphics.MeasureString(text, bonusFont);
            graphics.DrawString(text, bonusFont, textBrush, food.Left + (food.Width - size.Width) / 2F, food.Top + (food.Height - size.Height) / 2F);
        }
    }

    private void DrawSnake(Graphics graphics, Rectangle board)
    {
        using SolidBrush bodyBrush = new(Color.FromArgb(34, 197, 94));
        using SolidBrush headBrush = new(Color.FromArgb(132, 204, 22));

        foreach (Point part in _game.Snake)
        {
            Rectangle cell = CellRectangle(part, board);
            Brush brush = part == _game.Head ? headBrush : bodyBrush;
            graphics.FillRoundedRectangle(brush, cell, Math.Max(4, cell.Width / 4));
        }
    }

    private void DrawOverlay(Graphics graphics, Rectangle board)
    {
        using SolidBrush overlayBrush = new(Color.FromArgb(176, 2, 6, 23));
        graphics.FillRectangle(overlayBrush, board);

        string message;
        string subMessage;
        string dataMessage;

        if (_isLoading)
        {
            message = "Loading...";
            subMessage = "Developed By Ali Zain Khan";
            dataMessage = "Get ready to play";
        }
        else if (_game.IsGameOver)
        {
            message = "Game Over";
            subMessage = "Press Space to restart";
            dataMessage = $"Score: {_game.Score} | Best: {Math.Max(_playerData.HighScore, _game.Score)}";
        }
        else if (_paused && _hasStarted)
        {
            message = "Paused";
            subMessage = "Press Space to resume";
            dataMessage = $"Score: {_game.Score} | Best: {Math.Max(_playerData.HighScore, _game.Score)}";
        }
        else
        {
            message = "Snake Game";
            subMessage = "Press Space to start";
            dataMessage = "Developed By Ali Zain Khan | Move with WASD";
        }

        using Font messageFont = new("Segoe UI", 26F, FontStyle.Bold);
        using Font subFont = new("Segoe UI", 13F, FontStyle.Bold);
        using Font dataFont = new("Segoe UI", 10F, FontStyle.Regular);
        using SolidBrush textBrush = new(Color.White);
        using SolidBrush subBrush = new(Color.FromArgb(203, 213, 225));
        using SolidBrush accentBrush = new(Color.FromArgb(74, 222, 128));

        SizeF messageSize = graphics.MeasureString(message, messageFont);
        SizeF subSize = graphics.MeasureString(subMessage, subFont);
        SizeF dataSize = graphics.MeasureString(dataMessage, dataFont);

        float centerX = board.Left + board.Width / 2F;
        float startY = board.Top + board.Height / 2F - 65;

        graphics.DrawString(message, messageFont, textBrush, centerX - messageSize.Width / 2F, startY);
        graphics.DrawString(subMessage, subFont, _isLoading ? accentBrush : subBrush, centerX - subSize.Width / 2F, startY + 56);
        graphics.DrawString(dataMessage, dataFont, subBrush, centerX - dataSize.Width / 2F, startY + 90);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;

        if (key == Keys.Space)
        {
            if (_isLoading)
            {
                return true;
            }

            if (_game.IsGameOver || !_hasStarted)
            {
                StartNewGame();
            }
            else
            {
                TogglePause();
            }

            return true;
        }

        if (key == Keys.M)
        {
            ToggleSound();
            return true;
        }

        if (_isLoading || _paused || _game.IsGameOver)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        switch (key)
        {
            case Keys.W:
                _game.ChangeDirection(Direction.Up);
                return true;
            case Keys.S:
                _game.ChangeDirection(Direction.Down);
                return true;
            case Keys.A:
                _game.ChangeDirection(Direction.Left);
                return true;
            case Keys.D:
                _game.ChangeDirection(Direction.Right);
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private sealed class BrandPanel : Panel
    {
        public BrandPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using Font logoFont = new("Georgia", 25F, FontStyle.Bold | FontStyle.Italic);
            using Font creditFont = new("Segoe UI", 10F, FontStyle.Bold);
            using LinearGradientBrush logoBrush = new(ClientRectangle, Color.FromArgb(250, 204, 21), Color.FromArgb(34, 197, 94), LinearGradientMode.Horizontal);
            using SolidBrush creditBrush = new(Color.FromArgb(226, 232, 240));
            using Pen accentPen = new(Color.FromArgb(125, 211, 252), 2F);

            string logo = "NIAZI";
            string credit = "Developed By Ali Zain Khan";
            e.Graphics.DrawString(logo, logoFont, logoBrush, 0, -4);
            e.Graphics.DrawLine(accentPen, 2, 38, 116, 38);
            e.Graphics.DrawString(credit, creditFont, creditBrush, 0, 40);
        }
    }

    private sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using GraphicsPath path = new();
        int diameter = radius * 2;
        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        graphics.FillPath(brush, path);
    }
}

