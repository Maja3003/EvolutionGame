using Godot;

namespace EvoTap
{
public partial class NeonowaRyba : Node2D
{
	[Export] public float IdleSpeed    = 1.0f;
	[Export] public float MoveSpeed    = 1.0f;
	[Export] public bool  AutoPlayIdle = true;

	private AnimationPlayer _anim;
	private string _currentAnim = "";
	private bool   _specialPlaying = false;

	public const string ANIM_IDLE              = "idle";
	public const string ANIM_MOVE              = "move";
	public const string ANIM_ROZBLYSK_NEONOWY  = "rozblysk_neonowy";
	public const string ANIM_SPRINT_PODWODNY   = "sprint_podwodny";
	public const string ANIM_SMIERC            = "smierc";

	public override void _Ready()
	{
		_anim = GetNode<AnimationPlayer>("AnimationPlayer");
		_anim.AnimationFinished += OnAnimationFinished;
		if (AutoPlayIdle) PlayIdle();
	}

	public void PlayIdle()
	{
		if (_specialPlaying) return;
		_PlayAnim(ANIM_IDLE, IdleSpeed);
	}

	public void PlayMove()
	{
		if (_specialPlaying) return;
		_PlayAnim(ANIM_MOVE, MoveSpeed);
	}

	public void StopMove()
	{
		if (_currentAnim == ANIM_MOVE) PlayIdle();
	}

	public void PlayRozblyskNeonowy()
	{
		_specialPlaying = true;
		_PlayAnim(ANIM_ROZBLYSK_NEONOWY, 1.0f);
	}

	// Alias dla wstecznej kompatybilnosci z CreatureClickArea
	public void PlaySpecial1() => PlayRozblyskNeonowy();

	public void PlaySprintPodwodny()
	{
		_specialPlaying = true;
		_PlayAnim(ANIM_SPRINT_PODWODNY, 1.0f);
	}

	public void PlaySpecial2() => PlaySprintPodwodny();

	public void PlaySmierc()
	{
		_specialPlaying = true;
		_PlayAnim(ANIM_SMIERC, 1.0f);
	}

	public void PlayAnim(string animName, float speed = 1.0f)
	{
		if (animName != ANIM_IDLE && animName != ANIM_MOVE) _specialPlaying = true;
		_PlayAnim(animName, speed);
	}

	private void _PlayAnim(string n, float speed)
	{
		if (_currentAnim == n && _anim.IsPlaying()) return;
		_currentAnim = n;
		_anim.SpeedScale = speed;
		_anim.Play(n);
	}

	private void OnAnimationFinished(StringName animName)
	{
		// Po smierci ryba zostaje niewidoczna — nie wracamy do idle
		if (animName == ANIM_SMIERC) return;

		_specialPlaying = false;
		PlayIdle();
	}
}
}
