using System;
using System.Collections.Generic;
using Combat.Launch;
using JetBrains.Annotations;
using Pathfinding.Util;
using UnityEngine;
using Util.Extensions;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Park
{
	[AddComponentMenu("Anjin: Game Building/Encounter Layer")]
	public class EncounterLayer : MonoBehaviour, IComparable<EncounterLayer>
	{
		private static List<EncounterLayer> _volumes = new List<EncounterLayer>();
		private static List<EncounterLayer> _all     = new List<EncounterLayer>();

		public bool Volume = false;

		public int Priority = 0;

		[Space]
		[Inline]
		public EncounterSettings Settings;

		private Collider _collider;
		private bool     _registered;

		[SerializeField] private List<Combat.Arena> arenas;

		private Dictionary<string, Combat.Arena> arenaLookup;

		private void Awake()
		{
			if (Volume)
				_collider = GetComponent<Collider>();

			if ((arenas != null) && (arenas.Count > 0))
			{
				arenaLookup = new Dictionary<string, Combat.Arena>();

				foreach (var arena in arenas)
				{
					string ID = arena.ID;

					if (!string.IsNullOrEmpty(ID) && !arenaLookup.ContainsKey(ID))
					{
						arenaLookup.Add(ID, arena);
					}
				}
			}
		}

		private void OnEnable()
		{
			UpdateRegistration(true);
		}

		private void OnDisable()
		{
			UpdateRegistration(false);
		}

		public void UpdateRegistration(bool register)
		{
			if (_registered && register)
			{
				UpdateRegistration(false);
			}

			if (register)
			{
				if (!Volume)
					_all.Add(this);
				else
					_volumes.Add(this);

				_registered = true;
			}
			else
			{
				_all.Remove(this);
				_volumes.Remove(this);

				_registered = false;
			}
		}

		public static void Refresh(ref EncounterSettings s, EncounterSettings @override, Vector3 pos)
		{
			s.Recipe        = @override.Recipe ? @override.Recipe : GetRecipe(pos);
			s.MonsterPrefab = @override.MonsterPrefab ? @override.MonsterPrefab : GetMonster(pos);

			if (@override.ArenaAddress.IsValid) s.ArenaAddress = @override.ArenaAddress;
			else if (@override.Arena) s.Arena                  = @override.Arena;
			else
			{
				EncounterLayer layer = GetVolume(pos, vol => vol.Settings.ArenaAddress.IsValid || vol.Settings.Arena != null);
				s.Arena        = (!string.IsNullOrEmpty(@override.ArenaID) ? ((layer.arenaLookup != null) && layer.arenaLookup.ContainsKey(@override.ArenaID) ? layer.arenaLookup[@override.ArenaID] : layer.Settings.Arena) : layer.Settings.Arena);
				s.ArenaAddress = layer.Settings.ArenaAddress;
			}
		}


		[CanBeNull]
		public static BattleRecipeAsset GetRecipe(Vector3 pos)
		{
			EncounterLayer pick = GetVolume(pos, vol => vol.Settings.Recipe != null);
			return pick != null ? pick.Settings.Recipe : null;
		}

		[CanBeNull]
		public static GameObject GetMonster(Vector3 pos)
		{
			EncounterLayer pick = GetVolume(pos, vol => vol.Settings.MonsterPrefab != null);
			return pick != null ? pick.Settings.MonsterPrefab : null;
		}

		public static SceneReference GetArena(Vector3 pos)
		{
			EncounterLayer pick = GetVolume(pos, vol => vol.Settings.ArenaAddress.IsValid);
			return pick != null ? pick.Settings.ArenaAddress : null;
		}

		[CanBeNull]
		private static EncounterLayer GetVolume(Vector3 pos, Func<EncounterLayer, bool> criterion)
		{
			List<EncounterLayer> matches = ListPool<EncounterLayer>.Claim();

			try
			{
				foreach (EncounterLayer vol in _all)
				{
					if (criterion(vol))
					{
						matches.Add(vol);
					}
				}

				foreach (EncounterLayer vol in _volumes)
				{
					if (criterion(vol) && vol._collider.ContainsPoint(pos))
						matches.Add(vol);
				}

				if (matches.Count <= 0)
					return null;

				matches.Sort();
				int priority = matches[0].Priority;

				int max = 0;
				for (int i = 0; i < matches.Count; i++)
				{
					EncounterLayer layer = matches[i];
					if (layer.Priority != priority)
					{
						max = i - 1;
						break;
					}
				}

				int            idx = RNG.Range(0, max);
				EncounterLayer ret = matches[idx];


				return ret;
			}
			finally
			{
				ListPool<EncounterLayer>.Release(matches);
			}
		}

		public int CompareTo(EncounterLayer other)
		{
			if (ReferenceEquals(this, other)) return 0;
			if (ReferenceEquals(null, other)) return 1;

			return -Priority.CompareTo(other.Priority);
		}
	}
}