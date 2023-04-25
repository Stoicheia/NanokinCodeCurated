using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Components
{
	public class HealthbarUI : StaticBoyUnity<HealthbarUI>
	{
		public GameObject BarRoot;
		public GameObject BarPrefab;
		public float      DisplayDuration = 2.5f;

		[CanBeNull]
		private Battle _battle;

		private List<FighterHealthbar> _bars;

		protected override void Awake()
		{
			base.Awake();
			_bars = new List<FighterHealthbar>();
		}

		public void SetBattle(Battle battle)
		{
			if (_battle != null)
			{
				// Despawn bars
				foreach (FighterHealthbar bar in _bars)
				{
					Destroy(bar.gameObject);
				}

				_bars.Clear();
				_battle.FighterAdded -= OnFighterAdded;
			}

			_battle = battle;

			if (_battle != null)
			{
				_battle.FighterAdded += OnFighterAdded;
			}
		}

		private void OnFighterAdded([NotNull] Fighter fter)
		{
			// Spawn bar
			GameObject       go  = Instantiate(BarPrefab, BarRoot.transform);
			FighterHealthbar bar = go.GetComponent<FighterHealthbar>();

			bar.fighter = fter;
			bar.SyncFighterValues();
			bar.SyncFighterPos();

			_bars.Add(bar);
		}

		public static void EnableFlag(Fighter fighter, string name, bool flag)
		{
			FighterHealthbar bar = Live.GetBar(fighter);
			if (bar != null)
			{
				if (flag)
				{
					bar.enables.Add(name);
					bar.SyncFighterValues();
				}
				else
				{
					bar.enables.Remove(name);
				}
			}
		}

		public static void EnableTimed(Fighter fighter, float? seconds = null)
		{
			seconds = seconds ?? Live.DisplayDuration;

			FighterHealthbar bar = Live.GetBar(fighter);
			if (bar != null)
			{
				bar.displayTime = seconds.Value;
				bar.SyncFighterValues();
			}
		}

		public static void FlashFlag(Fighter fighter, string name, bool flag)
		{
			EnableFlag(fighter, name, flag);
			FighterHealthbar bar = Live.GetBar(fighter);
			if (bar != null)
			{
				bar.Bar.Flash();
				bar.SyncFighterValues();
			}
		}

		public static void FlashTimed(Fighter fighter, float? seconds = null)
		{
			seconds = seconds ?? Live.DisplayDuration;

			EnableTimed(fighter, seconds);
			FighterHealthbar bar = Live.GetBar(fighter);
			if (bar != null)
			{
				bar.Bar.Flash();
				bar.SyncFighterValues();
			}
		}


		private void Update()
		{
			if (_battle != null)
			{
				// Update the HP values
				foreach (FighterHealthbar bar in _bars)
				{
					bar.displayTime -= Time.deltaTime;
					if (bar.displayTime <= 0)
						bar.displayTime = 0;

					bar.SetVisible(bar.displayTime > 0 || bar.enables.Count > 0);
					bar.SyncFighterValues();
					bar.SyncFighterPos();
				}
			}
		}

		[CanBeNull]
		private FighterHealthbar GetBar(Fighter fighter)
		{
			FighterHealthbar bar = null;
			for (var i = 0; i < _bars.Count; i++)
			{
				FighterHealthbar spawnedBar = _bars[i];
				if (spawnedBar.fighter == fighter)
				{
					bar = spawnedBar;
					break;
				}
			}

			return bar;
		}
	}
}