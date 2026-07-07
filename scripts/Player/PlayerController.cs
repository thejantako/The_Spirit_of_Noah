using Godot;

public partial class PlayerController : CharacterBody2D
{
	[ExportCategory("Movement")]
	[Export] public float MoveSpeed { get; set; } = 180f;
	[Export] public float Acceleration { get; set; } = 1200f;
	[Export] public float Friction { get; set; } = 1400f;

	[ExportCategory("Jump")]
	[Export] public float JumpVelocity { get; set; } = -420f;
	[Export] public float Gravity { get; set; } = 1200f;
	[Export] public float MaxFallSpeed { get; set; } = 900f;

	[ExportCategory("Dash")]
	[Export] public float DashSpeed { get; set; } = 520f;
	[Export] public float DashDuration { get; set; } = 0.16f;
	[Export] public float DashCooldown { get; set; } = 0.45f;

	[ExportCategory("References")]
	[Export] public WeaponHolder WeaponHolder { get; set; }
	[Export] public PlayerInventory Inventory { get; set; }
	[Export] public AnimatedSprite2D AnimatedSprite { get; set; }

	private float _dashTimer;
	private float _dashCooldownTimer;
	private bool _isDashing;
	private int _facingDirection = 1;

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		UpdateTimers(dt);
		HandleMovement(dt);
		HandleActions();

		MoveAndSlide();
		
		UpdateAnimations();
	}

	private void UpdateTimers(float delta)
	{
		if (_dashTimer > 0f)
		{
			_dashTimer -= delta;

			if (_dashTimer <= 0f)
			{
				_isDashing = false;
			}
		}

		if (_dashCooldownTimer > 0f)
		{
			_dashCooldownTimer -= delta;
		}
	}

	private void HandleMovement(float delta)
	{
		Vector2 velocity = Velocity;

		if (!_isDashing)
		{
			ApplyGravity(ref velocity, delta);
			HandleHorizontalMovement(ref velocity, delta);
			HandleJump(ref velocity);
			HandleDash(ref velocity);
		}

		Velocity = velocity;
	}

	private void ApplyGravity(ref Vector2 velocity, float delta)
	{
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * delta;
			velocity.Y = Mathf.Min(velocity.Y, MaxFallSpeed);
		}
	}

	private void HandleHorizontalMovement(ref Vector2 velocity, float delta)
	{
		float inputDirection = Input.GetAxis("move_left", "move_right");

		if (inputDirection != 0f)
		{
			_facingDirection = inputDirection > 0f ? 1 : -1;
			velocity.X = Mathf.MoveToward(
				velocity.X,
				inputDirection * MoveSpeed,
				Acceleration * delta
			);
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0f, Friction * delta);
		}
	}

	private void HandleJump(ref Vector2 velocity)
	{
		if (Input.IsActionJustPressed("jump") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}
	}

	private void HandleDash(ref Vector2 velocity)
	{
		if (!Input.IsActionJustPressed("dash"))
			return;

		if (_dashCooldownTimer > 0f)
			return;

		_isDashing = true;
		_dashTimer = DashDuration;
		_dashCooldownTimer = DashCooldown;

		velocity = new Vector2(_facingDirection * DashSpeed, 0f);
	}

	private void HandleActions()
	{
		if (WeaponHolder == null)
			return;

		if (Input.IsActionJustPressed("attack"))
		{
			WeaponHolder.UseMeleeWeapon(_facingDirection);
		}

		if (Input.IsActionJustPressed("shoot"))
		{
			WeaponHolder.UseRangedWeapon(_facingDirection);
		}
	}

	public int GetFacingDirection()
	{
		return _facingDirection;
	}
	
	private void UpdateAnimations()
	{
		if (AnimatedSprite == null) return;

		float inputDirection = Input.GetAxis("move_left", "move_right");

		// 1. Richtung flippen (nach links oder rechts schauen)
		if (inputDirection > 0)
		{
			AnimatedSprite.FlipH = false; // Schaut nach rechts
		}
		else if (inputDirection < 0)
		{
			AnimatedSprite.FlipH = true;  // Schaut nach links
		}

		// 2. Animation je nach Zustand auswählen
		if (!IsOnFloor())
		{
			AnimatedSprite.Play("Jump");
		}
		else if (inputDirection != 0)
		{
			AnimatedSprite.Play("Ride");
		}
		else
		{
			AnimatedSprite.Play("Idle");
		}
	}
}
