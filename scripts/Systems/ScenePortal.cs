using Godot;

public partial class ScenePortal : Area2D
{
    [ExportCategory("Target")]
    [Export(PropertyHint.File, "*.tscn")]
    public string TargetScenePath { get; set; } = "";

    [Export] public string TargetSpawnPointName { get; set; } = "SpawnPoint";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not PlayerController)
            return;

        if (string.IsNullOrWhiteSpace(TargetScenePath))
        {
            GD.PushWarning("ScenePortal hat keine Zielszene.");
            return;
        }

        GameState.NextSpawnPointName = TargetSpawnPointName;

        Error error = GetTree().ChangeSceneToFile(TargetScenePath);

        if (error != Error.Ok)
        {
            GD.PushError($"Szene konnte nicht geladen werden: {TargetScenePath}");
        }
    }
}