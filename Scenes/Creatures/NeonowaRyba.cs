using Godot;

namespace EvoTap
{
public partial class NeonowaRyba : Node2D
{
	// ── Inspector exports ──────────────────────────────────────────────────
	[Export] public float IdleSpeed    = 1.0f;   // mnożnik prędkości idle
	[Export] public float MoveSpeed    = 1.0f;   // mnożnik prędkości pływania
	[Export] public bool  AutoPlayIdle = true;   // automatyczny idle po starcie

	// ── Nodes ──────────────────────────────────────────────────────────────
	private AnimationPlayer _anim;

	// ── State ──────────────────────────────────────────────────────────────
	private string _currentAnim = "";
	private bool   _specialPlaying = false;

	// ── Nazwy animacji ─────────────────────────────────────────────────────
	public const string ANIM_IDLE     = "idle";
	public const string ANIM_MOVE     = "move";
	public const string ANIM_SPECIAL1 = "rozblysk_neonowy";
	public const string ANIM_SPECIAL2 = "sprint_podwodny";

	// ══════════════════════════════════════════════════════════════════════
	public override void _Ready()
	{
		_anim = GetNode<AnimationPlayer>("AnimationPlayer");

		// Podłącz zdarzenie końca animacji (dla special które nie są w pętli)
		_anim.AnimationFinished += OnAnimationFinished;

		if (AutoPlayIdle)
			PlayIdle();
	}

	// ══════════════════════════════════════════════════════════════════════
	//  Publiczne API animacji
	// ══════════════════════════════════════════════════════════════════════

	/// <summary>Lekkie unoszenie góra-dół, falowanie ogona i płetw, mruganie</summary>
	public void PlayIdle()
	{
		if (_specialPlaying) return;
		_PlayAnim(ANIM_IDLE, IdleSpeed);
	}

	/// <summary>Szybkie pływanie – mocne wymachiwanie ogonem</summary>
	public void PlayMove()
	{
		if (_specialPlaying) return;
		_PlayAnim(ANIM_MOVE, MoveSpeed);
	}

	/// <summary>Przestań pływać – wróć do idle</summary>
	public void StopMove()
	{
		if (_currentAnim == ANIM_MOVE)
			PlayIdle();
	}

	/// <summary>Rozbłysk neonowy – pulsujące rozpostanie płetw + powiększenie oka</summary>
	public void PlaySpecial1()
	{
		_specialPlaying = true;
		_PlayAnim(ANIM_SPECIAL1, 1.0f);
	}

	/// <summary>Sprint podwodny – błyskawiczny dash do przodu</summary>
	public void PlaySpecial2()
	{
		_specialPlaying = true;
		_PlayAnim(ANIM_SPECIAL2, 1.0f);
	}

	/// <summary>Graj animację po nazwie (dla wywołań zewnętrznych)</summary>
	public void PlayAnim(string animName, float speed = 1.0f)
	{
		bool isSpecial = animName == ANIM_SPECIAL1 || animName == ANIM_SPECIAL2;
		if (isSpecial) _specialPlaying = true;
		_PlayAnim(animName, speed);
	}

	// ══════════════════════════════════════════════════════════════════════
	//  Wewnętrzne
	// ══════════════════════════════════════════════════════════════════════

	private void _PlayAnim(string name, float speed)
	{
		if (_currentAnim == name && _anim.IsPlaying()) return;

		_currentAnim = name;
		_anim.SpeedScale = speed;
		_anim.Play(name);
	}

	private void OnAnimationFinished(StringName animName)
	{
		// Po zakończeniu specjalnej animacji wróć do idle
		if (animName == ANIM_SPECIAL1 || animName == ANIM_SPECIAL2)
		{
			_specialPlaying = false;
			PlayIdle();
		}
	}

	// ══════════════════════════════════════════════════════════════════════
	//  Integracja z CreatureClickArea (opcjonalna)
	//  Wywołaj z CreatureClickArea._Ready() lub OnClicked():
	//     var ryba = GetNode<NeonowaRyba>("NeonowaRyba");
	//     ryba.PlaySpecial1();
	// ══════════════════════════════════════════════════════════════════════
}
}
