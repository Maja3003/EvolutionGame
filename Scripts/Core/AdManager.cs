using Godot;
using System;

namespace EvoTap
{
	/// <summary>
	/// Wrapper na reklamy nagradzane (AdMob przez oficjalny plugin Godot).
	/// Rejestruj jako AutoLoad: "AdManager" → res://Scripts/Core/AdManager.cs
	///
	/// SETUP:
	/// 1. Zainstaluj plugin: https://github.com/Poing-Studios/godot-admob-plugin
	/// 2. Wpisz swoje AdUnitId w eksportowanych polach (lub w Project Settings)
	/// 3. W trybie DEBUG automatycznie używane są testowe ID Google
	///
	/// Użycie:
	///   AdManager.Instance.ShowRewardedAd(OnRewarded);
	/// gdzie OnRewarded to Action wywoływana po zakończeniu reklamy.
	/// </summary>
	public partial class AdManager : Node
	{
		public static AdManager Instance { get; private set; }

		// Wpisz swoje ID z konsoli AdMob (lub zostaw puste — DEBUG używa testowych)
		[Export] public string RewardedAdUnitId { get; set; } = "";
		[Export] public bool ForceTesting       { get; set; } = true;

		// ── Testowe ID Google (nie zmieniaj) ──────────────────────────────
		private const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";

		private string ActiveAdUnitId => ForceTesting || string.IsNullOrEmpty(RewardedAdUnitId)
			? TestRewardedId
			: RewardedAdUnitId;

		// Callbacki oczekujące na nagrodę
		private Action _pendingRewardCallback;
		private bool _adReady = false;
		private bool _pluginAvailable = false;

		// Referencja do pluginu AdMob (Node wstrzykiwany przez plugin Godota)
		// Nazwa węzła może się różnić w zależności od wersji pluginu
		private Node _admobNode;

		// ── INIT ──────────────────────────────────────────────────────────

		public override void _Ready()
		{
			Instance = this;

			// Sprawdź czy plugin jest dostępny (na PC nie będzie — tylko Android/iOS)
			_admobNode = GetNodeOrNull("/root/AdMob");
			_pluginAvailable = _admobNode != null;

			if (_pluginAvailable)
			{
				// Podłącz sygnały AdMob pluginu
				// Nazwy sygnałów zależą od wersji pluginu — sprawdź dokumentację
				_admobNode.Connect("rewarded_ad_loaded",
					Callable.From<string>(OnRewardedAdLoaded));
				_admobNode.Connect("rewarded_ad_failed_to_show",
					Callable.From<string, int>(OnRewardedAdFailed));
				_admobNode.Connect("user_earned_reward",
					Callable.From<string, string, int>(OnUserEarnedReward));
				_admobNode.Connect("rewarded_ad_dismissed",
					Callable.From<string>(OnRewardedAdDismissed));

				LoadRewardedAd();
			}
			else
			{
				GD.Print("[AdManager] Plugin AdMob niedostępny — tryb edytora/PC.");
				// W edytorze symulujemy gotową reklamę testową
				_adReady = OS.IsDebugBuild();
			}
		}

		// ── PUBLICZNE API ─────────────────────────────────────────────────

		/// <summary>
		/// Czy reklama nagradzana jest gotowa do wyświetlenia?
		/// </summary>
		public bool IsRewardedAdReady() => _adReady;

		/// <summary>
		/// Pokaż reklamę nagradzaną. onRewarded wywoływane gdy gracz obejrzy do końca.
		/// </summary>
		public void ShowRewardedAd(Action onRewarded)
		{
			if (!_adReady)
			{
				GD.PrintErr("[AdManager] Reklama nie jest gotowa.");
				LoadRewardedAd(); // Spróbuj załadować na kolejny raz
				return;
			}

			_pendingRewardCallback = onRewarded;

			if (_pluginAvailable)
			{
				_admobNode.Call("show_rewarded", ActiveAdUnitId);
				_adReady = false;
			}
			else if (OS.IsDebugBuild())
			{
				// W edytorze symuluj nagrodę po 2 sekundach
				GD.Print("[AdManager] [DEBUG] Symulacja reklamy nagradzanej...");
				GetTree().CreateTimer(2.0).Timeout += SimulateReward;
				_adReady = false;
			}
		}

		// ── PRYWATNE — CALLBACKI PLUGINU ──────────────────────────────────

		private void LoadRewardedAd()
		{
			if (!_pluginAvailable) return;
			_admobNode.Call("load_rewarded", ActiveAdUnitId);
			GD.Print("[AdManager] Ładowanie reklamy nagradzanej...");
		}

		private void OnRewardedAdLoaded(string adUnitId)
		{
			_adReady = true;
			GD.Print("[AdManager] Reklama gotowa.");
		}

		private void OnRewardedAdFailed(string adUnitId, int errorCode)
		{
			_adReady = false;
			GD.PrintErr($"[AdManager] Błąd ładowania reklamy: {errorCode}");
			// Ponów za 30 sekund
			GetTree().CreateTimer(30.0).Timeout += LoadRewardedAd;
		}

		private void OnUserEarnedReward(string adUnitId, string rewardType, int rewardAmount)
		{
			GD.Print($"[AdManager] Nagroda: {rewardAmount} {rewardType}");
			_pendingRewardCallback?.Invoke();
			_pendingRewardCallback = null;
			// Załaduj następną reklamę
			LoadRewardedAd();
		}

		private void OnRewardedAdDismissed(string adUnitId)
		{
			// Gracz zamknął bez obejrzenia — nie dawaj nagrody
			GD.Print("[AdManager] Reklama zamknięta bez nagrody.");
			_pendingRewardCallback = null;
			LoadRewardedAd();
		}

		private void SimulateReward()
		{
			GD.Print("[AdManager] [DEBUG] Nagroda symulowana.");
			_pendingRewardCallback?.Invoke();
			_pendingRewardCallback = null;
			_adReady = true; // W debug zawsze gotowe
		}
	}
}
