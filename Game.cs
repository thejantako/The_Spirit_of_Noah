using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
    private enum GameState
    {
        Title,
        Playing,
        GameOver
    }

    private enum ObjectKind
    {
        Club,
        Taxi,
        Robot,
        FlyingRobot
    }

    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const float GroundY = 606f;
    private const float DinoX = 230f;
    private const float Gravity = 1450f;
    private const float LiftForce = -820f;
    private const float MaxFallSpeed = 760f;
    private const float DinoRadius = 38f;
    private const int ClubScore = 25;
    private const string HighscorePath = "user://dino_bike_highscore.save";

    private static class AssetPath
    {
        public const string DinoBike = "res://assets/game/dino_bike.svg";
        public const string Club = "res://assets/game/club.svg";
        public const string Taxi = "res://assets/game/self_driving_taxi.svg";
        public const string Robot = "res://assets/game/robot.svg";
        public const string FlyingRobot = "res://assets/game/flying_robot.svg";
        public const string CityAi = "res://assets/game/city_ai.svg";
    }

    private readonly Random _random = new();
    private readonly List<RunnerObject> _objects = new();
    private readonly List<Building> _buildings = new();

    private GameState _state = GameState.Title;
    private GameArt _art = null!;
    private Label _titleLabel = null!;
    private Label _helpLabel = null!;
    private Label _scoreLabel = null!;
    private Label _highscoreLabel = null!;
    private Label _gameOverLabel = null!;

    private Vector2 _dinoPosition = new(DinoX, GroundY - 70f);
    private float _dinoVelocity;
    private float _worldSpeed = 360f;
    private float _spawnTimer;
    private float _clubLineTimer;
    private float _distanceScore;
    private int _clubs;
    private int _bestScore;
    private float _bikeWheelSpin;
    private float _aiPulse;

    public override void _Ready()
    {
        DisplayServer.WindowSetSize(new Vector2I(ScreenWidth, ScreenHeight));
        EnsureInputActions();
        _art = GameArt.Load();
        _bestScore = LoadHighscore();
        BuildUi();
        CreateCity();
        ShowTitle();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _aiPulse += dt;

        if (StartPressed() && (_state == GameState.Title || _state == GameState.GameOver))
            StartGame();

        if (_state == GameState.Playing)
            UpdateGame(dt);

        QueueRedraw();
    }

    private static bool StartPressed()
    {
        return Input.IsActionJustPressed("jump")
            || Input.IsActionJustPressed("ui_accept")
            || Input.IsMouseButtonPressed(MouseButton.Left);
    }

    private static void EnsureInputActions()
    {
        AddKeyAction("jump", Key.Space, Key.W, Key.Up);
    }

    private static void AddKeyAction(string actionName, params Key[] keys)
    {
        if (!InputMap.HasAction(actionName))
            InputMap.AddAction(actionName);

        foreach (var key in keys)
        {
            var inputEvent = new InputEventKey { Keycode = key };
            var exists = false;

            foreach (var existingEvent in InputMap.ActionGetEvents(actionName))
            {
                if (existingEvent is InputEventKey existingKey && existingKey.Keycode == key)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                InputMap.ActionAddEvent(actionName, inputEvent);
        }
    }

    private void BuildUi()
    {
        _titleLabel = MakeLabel(new Vector2(0, 92), new Vector2(ScreenWidth, 90), 56, HorizontalAlignment.Center);
        AddChild(_titleLabel);

        _highscoreLabel = MakeLabel(new Vector2(0, 178), new Vector2(ScreenWidth, 42), 28, HorizontalAlignment.Center);
        AddChild(_highscoreLabel);

        _helpLabel = MakeLabel(new Vector2(0, 230), new Vector2(ScreenWidth, 118), 24, HorizontalAlignment.Center);
        AddChild(_helpLabel);

        _scoreLabel = MakeLabel(new Vector2(24, 18), new Vector2(600, 44), 28, HorizontalAlignment.Left);
        AddChild(_scoreLabel);

        _gameOverLabel = MakeLabel(new Vector2(0, 128), new Vector2(ScreenWidth, 170), 42, HorizontalAlignment.Center);
        AddChild(_gameOverLabel);
    }

    private static Label MakeLabel(Vector2 position, Vector2 size, int fontSize, HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Position = position,
            Size = size,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.75f));
        label.AddThemeConstantOverride("shadow_offset_x", 3);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        return label;
    }

    private void CreateCity()
    {
        _buildings.Clear();

        var x = -40f;
        while (x < ScreenWidth + 160)
        {
            var width = _random.Next(72, 128);
            var height = _random.Next(190, 420);
            _buildings.Add(new Building(x, width, height, _random.Next(0, 4)));
            x += width + _random.Next(8, 22);
        }
    }

    private void ShowTitle()
    {
        _state = GameState.Title;
        _titleLabel.Visible = true;
        _highscoreLabel.Visible = true;
        _helpLabel.Visible = true;
        _scoreLabel.Visible = false;
        _gameOverLabel.Visible = false;
        _titleLabel.Text = "Dino Bike: KI-Stadtflucht";
        _highscoreLabel.Text = $"Highscore: {_bestScore}";
        _helpLabel.Text = "Space, W, Pfeil hoch, Maus oder Touch: steigen\nLoslassen: sinken\nSammle Keulen und weiche selbstfahrenden Taxis und Robotern aus.";
    }

    private void StartGame()
    {
        _state = GameState.Playing;
        _objects.Clear();
        _dinoPosition = new Vector2(DinoX, GroundY - 72f);
        _dinoVelocity = 0;
        _worldSpeed = 360f;
        _spawnTimer = 0.25f;
        _clubLineTimer = 0.7f;
        _distanceScore = 0;
        _clubs = 0;
        _bikeWheelSpin = 0;

        _titleLabel.Visible = false;
        _highscoreLabel.Visible = false;
        _helpLabel.Visible = false;
        _gameOverLabel.Visible = false;
        _scoreLabel.Visible = true;
        UpdateScoreLabel();
    }

    private void EndGame()
    {
        _state = GameState.GameOver;

        if (CurrentScore > _bestScore)
        {
            _bestScore = CurrentScore;
            SaveHighscore(_bestScore);
        }

        _gameOverLabel.Visible = true;
        _highscoreLabel.Visible = true;
        _helpLabel.Visible = true;
        _gameOverLabel.Text = $"Crash!\nScore: {CurrentScore}";
        _highscoreLabel.Text = $"Highscore: {_bestScore}";
        _helpLabel.Text = "Die schwebende Stadt-KI hat den Verkehr uebernommen.\nDruecke Space oder klicke fuer einen neuen Versuch.";
    }

    private int CurrentScore => Mathf.RoundToInt(_distanceScore) + _clubs * ClubScore;

    private void UpdateGame(float dt)
    {
        var lifting = Input.IsActionPressed("jump") || Input.IsMouseButtonPressed(MouseButton.Left);
        _dinoVelocity += (lifting ? LiftForce : Gravity) * dt;
        _dinoVelocity = Mathf.Clamp(_dinoVelocity, -640f, MaxFallSpeed);
        _dinoPosition.Y += _dinoVelocity * dt;
        _dinoPosition.Y = Mathf.Clamp(_dinoPosition.Y, 96f, GroundY - 54f);

        if (Mathf.IsEqualApprox(_dinoPosition.Y, GroundY - 54f) && _dinoVelocity > 0)
            _dinoVelocity = 0;

        _worldSpeed += dt * 8f;
        _distanceScore += dt * 8f;
        _bikeWheelSpin += dt * _worldSpeed * 0.08f;

        UpdateBuildings(dt);
        UpdateSpawning(dt);
        UpdateObjects(dt);
        UpdateScoreLabel();
    }

    private void UpdateBuildings(float dt)
    {
        foreach (var building in _buildings)
            building.X -= _worldSpeed * 0.26f * dt;

        var rightEdge = 0f;
        foreach (var building in _buildings)
            rightEdge = Mathf.Max(rightEdge, building.X + building.Width);

        foreach (var building in _buildings)
        {
            if (building.X + building.Width < -80)
            {
                building.X = rightEdge + _random.Next(8, 24);
                building.Width = _random.Next(72, 130);
                building.Height = _random.Next(190, 420);
                building.Style = _random.Next(0, 4);
                rightEdge = building.X + building.Width;
            }
        }
    }

    private void UpdateSpawning(float dt)
    {
        _spawnTimer -= dt;
        _clubLineTimer -= dt;

        if (_spawnTimer <= 0)
        {
            SpawnObstacle();
            _spawnTimer = Mathf.Max(0.55f, 1.18f - _worldSpeed / 980f) + _random.NextSingle() * 0.45f;
        }

        if (_clubLineTimer <= 0)
        {
            SpawnClubs();
            _clubLineTimer = 1.15f + _random.NextSingle() * 1.15f;
        }
    }

    private void SpawnObstacle()
    {
        var roll = _random.Next(0, 100);
        ObjectKind kind;
        Vector2 size;
        float y;

        if (roll < 48)
        {
            kind = ObjectKind.Taxi;
            size = new Vector2(100, 52);
            y = GroundY - 36;
        }
        else if (roll < 78)
        {
            kind = ObjectKind.Robot;
            size = new Vector2(44, 86);
            y = GroundY - 62;
        }
        else
        {
            kind = ObjectKind.FlyingRobot;
            size = new Vector2(82, 42);
            y = _random.Next(135, 395);
        }

        _objects.Add(new RunnerObject(kind, new Vector2(ScreenWidth + 86, y), size));
    }

    private void SpawnClubs()
    {
        var startY = _random.Next(150, 470);
        var startX = ScreenWidth + 70f;

        for (var i = 0; i < 6; i++)
        {
            var y = startY + Mathf.Sin(i * 0.8f) * 32f;
            _objects.Add(new RunnerObject(ObjectKind.Club, new Vector2(startX + i * 44, y), new Vector2(34, 34)));
        }
    }

    private void UpdateObjects(float dt)
    {
        for (var i = _objects.Count - 1; i >= 0; i--)
        {
            var runnerObject = _objects[i];
            runnerObject.Position.X -= _worldSpeed * dt;
            runnerObject.Animation += dt;

            if (runnerObject.Position.X < -150)
            {
                _objects.RemoveAt(i);
                continue;
            }

            if (runnerObject.Kind == ObjectKind.Club)
            {
                if (_dinoPosition.DistanceTo(runnerObject.Position) < DinoRadius + 18f)
                {
                    _clubs++;
                    _objects.RemoveAt(i);
                }

                continue;
            }

            if (CollidesWithDino(runnerObject))
            {
                EndGame();
                return;
            }
        }
    }

    private bool CollidesWithDino(RunnerObject runnerObject)
    {
        var rect = new Rect2(runnerObject.Position - runnerObject.Size / 2f, runnerObject.Size);
        var closest = new Vector2(
            Mathf.Clamp(_dinoPosition.X, rect.Position.X, rect.Position.X + rect.Size.X),
            Mathf.Clamp(_dinoPosition.Y, rect.Position.Y, rect.Position.Y + rect.Size.Y)
        );

        return closest.DistanceTo(_dinoPosition) < DinoRadius * 0.86f;
    }

    private void UpdateScoreLabel()
    {
        _scoreLabel.Text = $"Score: {CurrentScore}    Keulen: {_clubs} x {ClubScore}    Speed: {Mathf.RoundToInt(_worldSpeed)}";
    }

    public override void _Draw()
    {
        DrawBackground();
        DrawCity();
        DrawRoad();
        DrawCentralAi();

        foreach (var runnerObject in _objects)
            DrawRunnerObject(runnerObject);

        DrawDinoBike();
    }

    private void DrawBackground()
    {
        DrawRect(new Rect2(0, 0, ScreenWidth, ScreenHeight), new Color(0.06f, 0.08f, 0.12f));
        DrawRect(new Rect2(0, 0, ScreenWidth, 360), new Color(0.11f, 0.17f, 0.24f));

        for (var i = 0; i < 38; i++)
        {
            var x = (i * 97 + Mathf.Sin(_aiPulse * 0.3f + i) * 14f) % ScreenWidth;
            var y = 44 + i * 53 % 260;
            DrawCircle(new Vector2(x, y), 1.8f, new Color(0.5f, 0.95f, 1f, 0.45f));
        }
    }

    private void DrawCity()
    {
        foreach (var building in _buildings)
        {
            var top = GroundY - building.Height;
            var color = building.Style switch
            {
                0 => new Color(0.18f, 0.24f, 0.31f),
                1 => new Color(0.13f, 0.18f, 0.24f),
                2 => new Color(0.23f, 0.2f, 0.26f),
                _ => new Color(0.16f, 0.25f, 0.28f)
            };

            DrawRect(new Rect2(building.X, top, building.Width, building.Height), color);

            for (var y = top + 24; y < GroundY - 24; y += 38)
            {
                for (var x = building.X + 14; x < building.X + building.Width - 16; x += 28)
                {
                    var lit = ((int)(x + y + building.Style * 17) % 3) != 0;
                    var windowColor = lit ? new Color(0.9f, 0.93f, 0.98f, 0.8f) : new Color(0.04f, 0.06f, 0.1f);
                    DrawRect(new Rect2(x, y, 12, 18), windowColor);
                }
            }
        }
    }

    private void DrawRoad()
    {
        DrawRect(new Rect2(0, GroundY, ScreenWidth, ScreenHeight - GroundY), new Color(0.08f, 0.08f, 0.09f));
        DrawRect(new Rect2(0, GroundY - 8, ScreenWidth, 8), new Color(0.38f, 0.44f, 0.48f));

        var offset = (Time.GetTicksMsec() / 1000f * _worldSpeed) % 130f;
        for (var x = -offset; x < ScreenWidth + 130; x += 130)
            DrawRect(new Rect2(x, GroundY + 52, 70, 8), new Color(0.98f, 0.83f, 0.2f));
    }

    private void DrawCentralAi()
    {
        var center = new Vector2(ScreenWidth * 0.5f, 86f + Mathf.Sin(_aiPulse * 1.7f) * 8f);
        var rect = RectAround(center, new Vector2(120, 120));
        var glow = 70f + Mathf.Sin(_aiPulse * 4f) * 10f;

        DrawCircle(center, glow, new Color(0.2f, 0.9f, 1f, 0.11f));
        if (!DrawAsset(_art.CityAi, rect))
            DrawCentralAiFallback(center);
    }

    private void DrawRunnerObject(RunnerObject runnerObject)
    {
        switch (runnerObject.Kind)
        {
            case ObjectKind.Club:
                DrawClub(runnerObject);
                break;
            case ObjectKind.Taxi:
                DrawTaxi(runnerObject);
                break;
            case ObjectKind.Robot:
                DrawRobot(runnerObject);
                break;
            case ObjectKind.FlyingRobot:
                DrawFlyingRobot(runnerObject);
                break;
        }
    }

    private void DrawClub(RunnerObject club)
    {
        var wobble = Mathf.Sin(club.Animation * 5f) * 4f;
        var rect = RectAround(club.Position + new Vector2(0, wobble), new Vector2(44, 44));
        if (DrawAsset(_art.Club, rect))
            return;

        DrawCircle(club.Position, 15, new Color(0.74f, 0.39f, 0.18f));
        DrawCircle(club.Position + new Vector2(-12, -4), 9, new Color(0.95f, 0.92f, 0.84f));
        DrawLine(club.Position + new Vector2(-2, 12), club.Position + new Vector2(-20, 30), new Color(0.53f, 0.29f, 0.14f), 9);
    }

    private void DrawTaxi(RunnerObject taxi)
    {
        if (DrawAsset(_art.Taxi, RectAround(taxi.Position, new Vector2(118, 64))))
            return;

        var p = taxi.Position;
        DrawRect(new Rect2(p.X - 50, p.Y - 24, 100, 38), new Color(1f, 0.78f, 0.06f));
        DrawRect(new Rect2(p.X - 22, p.Y - 44, 52, 24), new Color(1f, 0.85f, 0.12f));
        DrawCircle(p + new Vector2(-30, 16), 12, Colors.Black);
        DrawCircle(p + new Vector2(32, 16), 12, Colors.Black);
    }

    private void DrawRobot(RunnerObject robot)
    {
        if (DrawAsset(_art.Robot, RectAround(robot.Position, new Vector2(58, 94))))
            return;

        var p = robot.Position;
        DrawRect(new Rect2(p.X - 18, p.Y - 44, 36, 28), new Color(0.43f, 0.49f, 0.54f));
        DrawRect(new Rect2(p.X - 16, p.Y - 12, 32, 44), new Color(0.23f, 0.28f, 0.32f));
        DrawCircle(p + new Vector2(-7, -31), 4, new Color(0.35f, 0.94f, 1f));
        DrawCircle(p + new Vector2(7, -31), 4, new Color(0.35f, 0.94f, 1f));
    }

    private void DrawFlyingRobot(RunnerObject robot)
    {
        var hover = Mathf.Sin(robot.Animation * 8f) * 5f;
        if (DrawAsset(_art.FlyingRobot, RectAround(robot.Position + new Vector2(0, hover), new Vector2(94, 52))))
            return;

        var p = robot.Position;
        DrawRect(new Rect2(p.X - 26, p.Y - 14, 52, 28), new Color(0.18f, 0.22f, 0.26f));
        DrawCircle(p, 9, new Color(0.25f, 1f, 1f));
        DrawLine(p + new Vector2(-30, -4), p + new Vector2(-58, -18), new Color(0.55f, 0.95f, 1f), 3);
        DrawLine(p + new Vector2(30, -4), p + new Vector2(58, -18), new Color(0.55f, 0.95f, 1f), 3);
    }

    private void DrawDinoBike()
    {
        if (DrawAsset(_art.DinoBike, RectAround(_dinoPosition + new Vector2(18, -18), new Vector2(150, 104))))
            return;

        var p = _dinoPosition;
        var wheelA = p + new Vector2(-30, 34);
        var wheelB = p + new Vector2(46, 34);

        DrawCircle(wheelA, 22, Colors.Black);
        DrawCircle(wheelB, 22, Colors.Black);
        DrawCircle(wheelA, 14, new Color(0.56f, 0.62f, 0.66f));
        DrawCircle(wheelB, 14, new Color(0.56f, 0.62f, 0.66f));
        DrawLine(wheelA, p + new Vector2(6, 0), new Color(0.9f, 0.1f, 0.22f), 5);
        DrawLine(wheelB, p + new Vector2(6, 0), new Color(0.9f, 0.1f, 0.22f), 5);
        DrawLine(wheelA, wheelB, new Color(0.9f, 0.1f, 0.22f), 4);

        for (var i = 0; i < 6; i++)
        {
            var angle = _bikeWheelSpin + i * Mathf.Tau / 6f;
            DrawLine(wheelA, wheelA + Vector2.FromAngle(angle) * 20f, new Color(0.85f, 0.85f, 0.85f), 1.5f);
            DrawLine(wheelB, wheelB + Vector2.FromAngle(angle) * 20f, new Color(0.85f, 0.85f, 0.85f), 1.5f);
        }

        var body = new Color(0.26f, 0.67f, 0.27f);
        DrawPolygon(
            new[]
            {
                p + new Vector2(-36, -24), p + new Vector2(-14, -66), p + new Vector2(48, -58),
                p + new Vector2(78, -28), p + new Vector2(42, -8), p + new Vector2(-16, -6)
            },
            new[] { body });
        DrawCircle(p + new Vector2(66, -67), 24, body);
        DrawCircle(p + new Vector2(74, -75), 4, Colors.Black);
        DrawPolygon(
            new[]
            {
                p + new Vector2(-34, -44), p + new Vector2(-74, -62), p + new Vector2(-46, -22)
            },
            new[] { body });
    }

    private void DrawCentralAiFallback(Vector2 center)
    {
        DrawCircle(center, 46, new Color(0.05f, 0.16f, 0.22f));
        DrawArc(center, 54, 0, Mathf.Tau, 80, new Color(0.3f, 1f, 1f), 4);
        DrawCircle(center, 15, new Color(0.25f, 1f, 1f));
        DrawLine(center + new Vector2(-90, 26), center + new Vector2(-28, 8), new Color(0.3f, 1f, 1f, 0.45f), 3);
        DrawLine(center + new Vector2(90, 26), center + new Vector2(28, 8), new Color(0.3f, 1f, 1f, 0.45f), 3);
        DrawString(ThemeDB.FallbackFont, center + new Vector2(-42, 84), "CITY-AI", HorizontalAlignment.Center, 84, 18, Colors.White);
    }

    private bool DrawAsset(Texture2D texture, Rect2 rect)
    {
        if (texture is null)
            return false;

        DrawTextureRect(texture, rect, false);
        return true;
    }

    private static Rect2 RectAround(Vector2 center, Vector2 size)
    {
        return new Rect2(center - size / 2f, size);
    }

    private static int LoadHighscore()
    {
        if (!FileAccess.FileExists(HighscorePath))
            return 0;

        using var file = FileAccess.Open(HighscorePath, FileAccess.ModeFlags.Read);
        if (file is null)
            return 0;

        return int.TryParse(file.GetAsText().Trim(), out var score) ? score : 0;
    }

    private static void SaveHighscore(int score)
    {
        using var file = FileAccess.Open(HighscorePath, FileAccess.ModeFlags.Write);
        file?.StoreString(score.ToString());
    }

    private sealed class GameArt
    {
        private GameArt(
            Texture2D dinoBike,
            Texture2D club,
            Texture2D taxi,
            Texture2D robot,
            Texture2D flyingRobot,
            Texture2D cityAi)
        {
            DinoBike = dinoBike;
            Club = club;
            Taxi = taxi;
            Robot = robot;
            FlyingRobot = flyingRobot;
            CityAi = cityAi;
        }

        public Texture2D DinoBike { get; }
        public Texture2D Club { get; }
        public Texture2D Taxi { get; }
        public Texture2D Robot { get; }
        public Texture2D FlyingRobot { get; }
        public Texture2D CityAi { get; }

        public static GameArt Load()
        {
            return new GameArt(
                LoadTexture(AssetPath.DinoBike),
                LoadTexture(AssetPath.Club),
                LoadTexture(AssetPath.Taxi),
                LoadTexture(AssetPath.Robot),
                LoadTexture(AssetPath.FlyingRobot),
                LoadTexture(AssetPath.CityAi)
            );
        }

        private static Texture2D LoadTexture(string path)
        {
            return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
        }
    }

    private sealed class RunnerObject
    {
        public RunnerObject(ObjectKind kind, Vector2 position, Vector2 size)
        {
            Kind = kind;
            Position = position;
            Size = size;
        }

        public ObjectKind Kind { get; }
        public Vector2 Position;
        public Vector2 Size { get; }
        public float Animation;
    }

    private sealed class Building
    {
        public Building(float x, float width, float height, int style)
        {
            X = x;
            Width = width;
            Height = height;
            Style = style;
        }

        public float X;
        public float Width;
        public float Height;
        public int Style;
    }
}
