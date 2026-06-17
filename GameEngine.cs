using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SnakeGameWinForms;

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public enum StepResult
{
    Moved,
    AteFood,
    GameOver,
    BoardCleared
}

public sealed class GameEngine
{
    private const int NormalFoodValue = 10;
    private const int BonusFoodValue = 50;
    private const int FoodLifetimeMilliseconds = 6000;

    private readonly Random _random = new();
    private readonly LinkedList<Point> _snake = new();
    private Direction _pendingDirection;
    private bool _nextFoodIsBonus;

    public GameEngine(int columns, int rows)
    {
        if (columns < 10 || rows < 10)
        {
            throw new ArgumentException("The board must be at least 10 by 10.");
        }

        Columns = columns;
        Rows = rows;
        Reset();
    }

    public int Columns { get; }
    public int Rows { get; }
    public Direction CurrentDirection { get; private set; }
    public Point Food { get; private set; }
    public int FoodValue { get; private set; } = NormalFoodValue;
    public DateTime FoodExpiresAtUtc { get; private set; }
    public int Score { get; private set; }
    public int Level => Math.Min(20, Score / 80 + 1);
    public int ComboCount { get; private set; }
    public int LastPointsEarned { get; private set; }
    public bool IsBonusFood => FoodValue == BonusFoodValue;
    public bool IsGameOver { get; private set; }
    public IReadOnlyCollection<Point> Snake => _snake;
    public Point Head => _snake.First!.Value;

    public void Reset()
    {
        _snake.Clear();

        int startX = Columns / 2;
        int startY = Rows / 2;
        _snake.AddFirst(new Point(startX, startY));
        _snake.AddLast(new Point(startX - 1, startY));
        _snake.AddLast(new Point(startX - 2, startY));

        CurrentDirection = Direction.Right;
        _pendingDirection = Direction.Right;
        Score = 0;
        ComboCount = 0;
        LastPointsEarned = 0;
        _nextFoodIsBonus = false;
        FoodValue = NormalFoodValue;
        IsGameOver = false;
        SpawnFood();
    }

    public void ChangeDirection(Direction requestedDirection)
    {
        if (IsOpposite(CurrentDirection, requestedDirection))
        {
            return;
        }

        _pendingDirection = requestedDirection;
    }

    public bool HasFoodExpired(DateTime utcNow)
    {
        return !IsGameOver && utcNow >= FoodExpiresAtUtc;
    }

    public void MoveFoodAfterMiss()
    {
        if (IsGameOver)
        {
            return;
        }

        ComboCount = 0;
        _nextFoodIsBonus = false;
        FoodValue = NormalFoodValue;
        SpawnFood();
    }

    public double GetFoodSecondsRemaining(DateTime utcNow)
    {
        double seconds = (FoodExpiresAtUtc - utcNow).TotalSeconds;
        return Math.Max(0, seconds);
    }

    public StepResult Step()
    {
        if (IsGameOver)
        {
            return StepResult.GameOver;
        }

        LastPointsEarned = 0;
        CurrentDirection = _pendingDirection;
        Point nextHead = GetNextHead();

        bool ateFood = nextHead == Food;
        Point tail = _snake.Last!.Value;
        bool hitsWall = nextHead.X < 0 || nextHead.X >= Columns || nextHead.Y < 0 || nextHead.Y >= Rows;
        bool hitsBody = _snake.Contains(nextHead) && !(nextHead == tail && !ateFood);

        if (hitsWall || hitsBody)
        {
            IsGameOver = true;
            return StepResult.GameOver;
        }

        _snake.AddFirst(nextHead);

        if (ateFood)
        {
            LastPointsEarned = FoodValue;
            Score += LastPointsEarned;
            AdvanceComboAfterEat();
            SpawnFood();
            return IsGameOver ? StepResult.BoardCleared : StepResult.AteFood;
        }

        _snake.RemoveLast();
        return StepResult.Moved;
    }

    private void AdvanceComboAfterEat()
    {
        if (FoodValue == BonusFoodValue)
        {
            ComboCount = 0;
            _nextFoodIsBonus = false;
            FoodValue = NormalFoodValue;
            return;
        }

        ComboCount++;
        if (ComboCount >= 5)
        {
            _nextFoodIsBonus = true;
        }
    }

    private Point GetNextHead()
    {
        Point head = Head;

        return CurrentDirection switch
        {
            Direction.Up => new Point(head.X, head.Y - 1),
            Direction.Down => new Point(head.X, head.Y + 1),
            Direction.Left => new Point(head.X - 1, head.Y),
            Direction.Right => new Point(head.X + 1, head.Y),
            _ => head
        };
    }

    private void SpawnFood()
    {
        FoodValue = _nextFoodIsBonus ? BonusFoodValue : NormalFoodValue;
        Food = CreateFood();
        FoodExpiresAtUtc = DateTime.UtcNow.AddMilliseconds(FoodLifetimeMilliseconds);
    }

    private Point CreateFood()
    {
        List<Point> emptyCells = new();

        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Columns; x++)
            {
                Point cell = new(x, y);
                if (!_snake.Contains(cell))
                {
                    emptyCells.Add(cell);
                }
            }
        }

        if (emptyCells.Count == 0)
        {
            IsGameOver = true;
            return Point.Empty;
        }

        return emptyCells[_random.Next(emptyCells.Count)];
    }

    private static bool IsOpposite(Direction current, Direction requested)
    {
        return current == Direction.Up && requested == Direction.Down
            || current == Direction.Down && requested == Direction.Up
            || current == Direction.Left && requested == Direction.Right
            || current == Direction.Right && requested == Direction.Left;
    }
}



