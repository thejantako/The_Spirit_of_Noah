using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;

    private const float PlayerSpeed = 370f;
    private const float PlayerRadius = 22f;
    private const float AnimalRadius = 18f;
    private const float DropRadius = 10f;

    private readonly LevelDefinition[] _levels =
    {
        new("Die ersten Tiere", 8, 70f, 0.72f, 0.24f, 235, 430, 1),
        new("Der starke Regen", 10, 62f, 0.58f, 0.19f, 280, 520, 2),
        new("Die dunkle Flut", 12, 55f, 0.46f, 0.15f, 330, 610, 3),
        new("Letzte Archefahrt", 14, 48f, 0.36f, 0.11f, 390, 710, 4)
    };

    private readonly Random _random = new();

    private NoahPlayer _player = null!;
    private Label _hud = null!;
    private Label _centerMessage = null!;

    private readonly List<Animal> _animals = new();
    private readonly List<StormDrop> _drops = new();
    private readonly List<Cloud> _clouds = new();

    private int _score;
    private int _rescuedThisLevel;
    private int _lives;
    private int _levelIndex;
    private float _timeLeft;
    private float _stormTimer;
    private float _levelMessageTimer;
    private bool _gameOver;

    public override void _Ready()
    {
        DisplayServer.WindowSetSize(new Vector2I(ScreenWidth, ScreenHeight));

        EnsureInputActions();

        CreateWorld();
        RestartGame();
    }

    private static void EnsureInputActions()
    {
        AddKeyAction("move_left", Key.A, Key.Left);
        AddKeyAction("move_right", Key.D, Key.Right);
        AddKeyAction("move_up", Key.W, Key.Up);
        AddKeyAction("move_down", Key.S, Key.Down);
        AddKeyAction("restart", Key.R);
    }

    private static void AddKeyAction(string actionName, params Key[] keys)
    {
        if (!InputMap.HasAction(actionName))
        {
            InputMap.AddAction(actionName);
        }

        foreach (Key key in keys)
        {
            InputEventKey inputEvent = new()
            {
                Keycode = key
            };

            bool alreadyExists = false;

            foreach (InputEvent existingEvent in InputMap.ActionGetEvents(actionName))
            {
                if (existingEvent is InputEventKey existingKey && existingKey.Keycode == key)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                InputMap.ActionAddEvent(actionName, inputEvent);
            }
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (Input.IsActionJustPressed("restart"))
        {
            RestartGame();
            return;
        }

        if (_gameOver)
        {
            QueueRedraw();
            return;
        }

        UpdateLevelMessage(dt);
        MovePlayer(dt);
        UpdateAnimals();
        UpdateStorm(dt);
        UpdateClock(dt);
        UpdateHud();

        QueueRedraw();
    }

    private void CreateWorld()
    {
        for (int i = 0; i < 7; i++)
        {
            Cloud cloud = new()
            {
                Position = new Vector2(_random.Next(0, ScreenWidth), _random.Next(85, 250)),
                Speed = _random.Next(15, 45)
            };

            _clouds.Add(cloud);
            AddChild(cloud);
        }

        _player = new NoahPlayer
        {
            Position = new Vector2(ScreenWidth / 2f, ScreenHeight / 2f)
        };
        AddChild(_player);

        _hud = new Label
        {
            Position = new Vector2(22, 16),
            Text = ""
        };
        _hud.AddThemeFontSizeOverride("font_size", 27);
        AddChild(_hud);

        _centerMessage = new Label
        {
            Position = new Vector2(0, 245),
            Size = new Vector2(ScreenWidth, 220),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = ""
        };
        _centerMessage.AddThemeFontSizeOverride("font_size", 43);
        AddChild(_centerMessage);
    }

    private void RestartGame()
    {
        foreach (Animal animal in _animals)
        {
            animal.QueueFree();
        }

        foreach (StormDrop drop in _drops)
        {
            drop.QueueFree();
        }

        _animals.Clear();
        _drops.Clear();

        _score = 0;
        _lives = 4;
        _levelIndex = 0;

        StartLevel();
    }

    private void StartLevel()
    {
        foreach (Animal animal in _animals)
        {
            animal.QueueFree();
        }

        foreach (StormDrop drop in _drops)
        {
            drop.QueueFree();
        }

        _animals.Clear();
        _drops.Clear();

        LevelDefinition level = CurrentLevel;

        _rescuedThisLevel = 0;
        _timeLeft = level.TimeLimit;
        _stormTimer = 1f;
        _gameOver = false;

        _player.Position = new Vector2(ScreenWidth / 2f, ScreenHeight / 2f);
        _player.Visible = true;
        _centerMessage.Text = $"LEVEL {_levelIndex + 1}: {level.Name}";
        _levelMessageTimer = 1.6f;

        CreateAnimals();
        UpdateHud();
    }

    private LevelDefinition CurrentLevel => _levels[_levelIndex];

    private void UpdateLevelMessage(float dt)
    {
        if (_levelMessageTimer <= 0f)
        {
            return;
        }

        _levelMessageTimer -= dt;

        if (_levelMessageTimer <= 0f)
        {
            _centerMessage.Text = "";
        }
    }

    private void CreateAnimals()
    {
        string[] names =
        {
            "Löwe",
            "Schaf",
            "Fuchs",
            "Taube",
            "Panda",
            "Katze",
            "Hund",
            "Hase",
            "Bär",
            "Ziege",
            "Eule",
            "Affe",
            "Kamel",
            "Reh"
        };

        for (int i = 0; i < CurrentLevel.AnimalCount; i++)
        {
            Animal animal = new()
            {
                AnimalName = names[i % names.Length],
                Position = new Vector2(
                    _random.Next(70, ScreenWidth - 70),
                    _random.Next(115, ScreenHeight - 80)
                )
            };

            _animals.Add(animal);
            AddChild(animal);
        }
    }

    private void MovePlayer(float dt)
    {
        Vector2 direction = Vector2.Zero;

        if (Input.IsActionPressed("move_left"))
        {
            direction.X -= 1;
        }

        if (Input.IsActionPressed("move_right"))
        {
            direction.X += 1;
        }

        if (Input.IsActionPressed("move_up"))
        {
            direction.Y -= 1;
        }

        if (Input.IsActionPressed("move_down"))
        {
            direction.Y += 1;
        }

        if (direction.LengthSquared() > 0)
        {
            direction = direction.Normalized();
        }

        _player.Position += direction * PlayerSpeed * dt;

        _player.Position = new Vector2(
            Mathf.Clamp(_player.Position.X, PlayerRadius, ScreenWidth - PlayerRadius),
            Mathf.Clamp(_player.Position.Y, 85 + PlayerRadius, ScreenHeight - PlayerRadius)
        );
    }

    private void UpdateAnimals()
    {
        for (int i = _animals.Count - 1; i >= 0; i--)
        {
            Animal animal = _animals[i];

            if (_player.Position.DistanceTo(animal.Position) <= PlayerRadius + AnimalRadius + 4)
            {
                _score++;
                _rescuedThisLevel++;
                _animals.RemoveAt(i);
                animal.QueueFree();
            }
        }

        if (_animals.Count == 0)
        {
            CompleteLevel();
        }
    }

    private void UpdateStorm(float dt)
    {
        _stormTimer -= dt;

        if (_stormTimer <= 0f)
        {
            LevelDefinition level = CurrentLevel;
            float difficulty = 1f - _timeLeft / level.TimeLimit;
            _stormTimer = Mathf.Lerp(level.MaxStormDelay, level.MinStormDelay, difficulty);
            SpawnDrop();
        }

        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            StormDrop drop = _drops[i];

            drop.Position += new Vector2(0, drop.Speed * dt);

            if (drop.Position.Y > ScreenHeight + 50)
            {
                _drops.RemoveAt(i);
                drop.QueueFree();
                continue;
            }

            if (_player.Position.DistanceTo(drop.Position) <= PlayerRadius + DropRadius)
            {
                _drops.RemoveAt(i);
                drop.QueueFree();

                _lives--;
                _player.HitFlash();

                if (_lives <= 0)
                {
                    Lose("Noah wurde vom Sturm besiegt.");
                }
            }
        }
    }

    private void SpawnDrop()
    {
        LevelDefinition level = CurrentLevel;
        StormDrop drop = new()
        {
            Position = new Vector2(_random.Next(10, ScreenWidth - 10), -25),
            Speed = _random.Next(level.MinDropSpeed, level.MaxDropSpeed)
        };

        _drops.Add(drop);
        AddChild(drop);
    }

    private void UpdateClock(float dt)
    {
        _timeLeft -= dt;

        if (_timeLeft <= 0)
        {
            _timeLeft = 0;
            Lose("Die Flut kam, bevor alle Tiere gerettet wurden.");
        }
    }

    private void UpdateHud()
    {
        LevelDefinition level = CurrentLevel;
        _hud.Text =
            $"Level {_levelIndex + 1}/{_levels.Length}: {level.Name}    Tiere: {_rescuedThisLevel}/{level.AnimalCount}    Gesamt: {_score}    Leben: {_lives}    Zeit: {Mathf.CeilToInt(_timeLeft)}    R = Neustart";
    }

    private void CompleteLevel()
    {
        if (_levelIndex >= _levels.Length - 1)
        {
            FinalWin();
            return;
        }

        _levelIndex++;
        StartLevel();
    }

    private void FinalWin()
    {
        _gameOver = true;
        _centerMessage.Text = "GEWONNEN!\nAlle Level sind geschafft und die Tiere sind sicher.\nDruecke R fuer eine neue Runde.";
        UpdateHud();
    }

    private void Win()
    {
        _gameOver = true;
        _centerMessage.Text = "GEWONNEN!\nAlle Tiere sind sicher auf der Arche.\nDrücke R für eine neue Runde.";
        UpdateHud();
    }

    private void Lose(string reason)
    {
        _gameOver = true;
        _centerMessage.Text = $"VERLOREN!\n{reason}\nDrücke R für Neustart.";
        UpdateHud();
    }

    public override void _Draw()
    {
        DrawSky();
        DrawWater();
        DrawArk();
        DrawBorder();
    }

    private void DrawSky()
    {
        Color skyColor = CurrentLevel.Sky switch
        {
            1 => new Color(0.08f, 0.12f, 0.21f),
            2 => new Color(0.06f, 0.08f, 0.14f),
            3 => new Color(0.04f, 0.05f, 0.1f),
            _ => new Color(0.02f, 0.03f, 0.07f)
        };

        DrawRect(new Rect2(Vector2.Zero, new Vector2(ScreenWidth, ScreenHeight)), skyColor);

        for (int i = 0; i < 70; i++)
        {
            float x = i * 19 % ScreenWidth;
            float y = 90 + i * 37 % 250;
            DrawCircle(new Vector2(x, y), 1.3f, new Color(1f, 1f, 0.75f, 0.35f));
        }
    }

    private void DrawWater()
    {
        DrawRect(new Rect2(0, 430, ScreenWidth, 290), new Color(0.02f, 0.18f, 0.28f));

        float t = Time.GetTicksMsec() / 380f;

        for (int x = -80; x < ScreenWidth + 80; x += 75)
        {
            float y = 540 + Mathf.Sin(t + x * 0.025f) * 9;
            DrawCircle(new Vector2(x, y), 42, new Color(0.07f, 0.39f, 0.55f, 0.42f));
        }

        for (int x = -80; x < ScreenWidth + 80; x += 95)
        {
            float y = 640 + Mathf.Sin(t * 1.25f + x * 0.02f) * 7;
            DrawCircle(new Vector2(x, y), 52, new Color(0.04f, 0.27f, 0.42f, 0.55f));
        }
    }

    private void DrawArk()
    {
        DrawPolygon(
            new[]
            {
                new Vector2(410, 495),
                new Vector2(870, 495),
                new Vector2(805, 590),
                new Vector2(475, 590)
            },
            new[]
            {
                new Color(0.43f, 0.23f, 0.08f)
            }
        );

        DrawPolygon(
            new[]
            {
                new Vector2(500, 495),
                new Vector2(780, 495),
                new Vector2(725, 425),
                new Vector2(555, 425)
            },
            new[]
            {
                new Color(0.58f, 0.34f, 0.13f)
            }
        );

        DrawLine(new Vector2(640, 425), new Vector2(640, 315), new Color(0.27f, 0.13f, 0.04f), 8);
        DrawPolygon(
            new[]
            {
                new Vector2(650, 325),
                new Vector2(750, 375),
                new Vector2(650, 420)
            },
            new[]
            {
                new Color(0.95f, 0.88f, 0.65f)
            }
        );

        DrawCircle(new Vector2(575, 465), 13, new Color(0.12f, 0.08f, 0.04f));
        DrawCircle(new Vector2(640, 465), 13, new Color(0.12f, 0.08f, 0.04f));
        DrawCircle(new Vector2(705, 465), 13, new Color(0.12f, 0.08f, 0.04f));
    }

    private void DrawBorder()
    {
        DrawRect(new Rect2(0, 70, ScreenWidth, 3), new Color(0.8f, 0.9f, 1f, 0.25f));
    }

    private partial class NoahPlayer : Node2D
    {
        private float _flashTime;

        public override void _Process(double delta)
        {
            if (_flashTime > 0)
            {
                _flashTime -= (float)delta;
                Modulate = new Color(1f, 0.35f, 0.35f);
            }
            else
            {
                Modulate = Colors.White;
            }

            QueueRedraw();
        }

        public void HitFlash()
        {
            _flashTime = 0.25f;
        }

        public override void _Draw()
        {
            DrawCircle(Vector2.Zero, PlayerRadius, new Color(0.11f, 0.75f, 0.92f));
            DrawCircle(new Vector2(0, -24), 13, new Color(0.9f, 0.72f, 0.52f));
            DrawCircle(new Vector2(-5, -27), 2.3f, Colors.Black);
            DrawCircle(new Vector2(5, -27), 2.3f, Colors.Black);
            DrawArc(new Vector2(0, -22), 6, 0.25f, 2.85f, 16, Colors.Black, 1.8f);
            DrawLine(new Vector2(-12, -5), new Vector2(-27, 8), Colors.White, 4);
            DrawLine(new Vector2(12, -5), new Vector2(27, 8), Colors.White, 4);
            DrawCircle(new Vector2(0, 4), 9, new Color(0.05f, 0.45f, 0.6f));
        }
    }

    private partial class Animal : Node2D
    {
        private readonly Color _color;
        private readonly float _offset;

        public string AnimalName { get; set; } = "Tier";

        public Animal()
        {
            Random random = new();
            _color = new Color(
                0.65f + random.NextSingle() * 0.3f,
                0.42f + random.NextSingle() * 0.35f,
                0.12f + random.NextSingle() * 0.25f
            );
            _offset = random.NextSingle() * 100f;
        }

        public override void _Process(double delta)
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            float bob = Mathf.Sin(Time.GetTicksMsec() / 260f + _offset) * 2.5f;

            DrawCircle(new Vector2(0, bob), AnimalRadius, _color);
            DrawCircle(new Vector2(-9, -14 + bob), 7, _color);
            DrawCircle(new Vector2(9, -14 + bob), 7, _color);
            DrawCircle(new Vector2(-6, -3 + bob), 3, Colors.Black);
            DrawCircle(new Vector2(6, -3 + bob), 3, Colors.Black);
            DrawCircle(new Vector2(0, 6 + bob), 4, new Color(0.12f, 0.06f, 0.03f));
            DrawString(ThemeDB.FallbackFont, new Vector2(-22, 38 + bob), AnimalName, HorizontalAlignment.Left, 70, 13,
                Colors.White);
        }
    }

    private partial class StormDrop : Node2D
    {
        public float Speed { get; set; } = 360f;

        public override void _Process(double delta)
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawCircle(Vector2.Zero, DropRadius, new Color(0.65f, 0.75f, 0.85f));
            DrawLine(new Vector2(0, -18), new Vector2(0, 16), new Color(0.82f, 0.92f, 1f), 3);
        }
    }

    private partial class Cloud : Node2D
    {
        public float Speed { get; set; } = 25f;

        public override void _Process(double delta)
        {
            Position += new Vector2(Speed * (float)delta, 0);

            if (Position.X > 1360)
            {
                Position = new Vector2(-120, Position.Y);
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            Color color = new(0.28f, 0.31f, 0.38f, 0.7f);

            DrawCircle(new Vector2(0, 0), 24, color);
            DrawCircle(new Vector2(25, -8), 30, color);
            DrawCircle(new Vector2(55, 0), 22, color);
            DrawRect(new Rect2(-5, 0, 65, 18), color);
        }
    }

    private sealed record LevelDefinition(
        string Name,
        int AnimalCount,
        float TimeLimit,
        float MaxStormDelay,
        float MinStormDelay,
        int MinDropSpeed,
        int MaxDropSpeed,
        int Sky
    );
}
