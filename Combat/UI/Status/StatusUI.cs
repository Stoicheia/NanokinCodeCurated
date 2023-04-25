using System.Collections.Generic;
using Anjin.Util;
using Combat.Data;
using Combat.UI;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Addressable;

namespace Combat.Components
{
	public class StatusUI : StaticBoyUnity<StatusUI>
	{
		[SerializeField] private Transform     Root_WorldSpace;
		[SerializeField] private SelectUIStyle SelectStyle = SelectUIStyle.Default;

		public static  SelectUIManager                  selection;
		private static Dictionary<Fighter, StatusPanel> _huds = new Dictionary<Fighter, StatusPanel>();
		private static List<StatusPanel>                _all  = new List<StatusPanel>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			selection = new SelectUIManager();
			_huds.Clear();
			_all.Clear();
		}


		public static async UniTask AddFighter([NotNull] Fighter fighter)
		{
			// FighterSource source = fighter.source;
			GameObject obj = await Addressables.InstantiateAsync("Combat/UI/Monster Status", Live.Root_WorldSpace);

			StatusPanel panel = obj.GetComponent<StatusPanel>();
			panel.transform.parent       = Live.Root_WorldSpace;
			panel.selectState.brightness = Live.SelectStyle.NormalBrightness;

			await panel.ChangeFighter(fighter);
			panel.SnapToEnd();

			_huds[fighter] = panel;
			_all.Add(panel);
		}

		public static void SetVisible(bool state = true)
		{
			Live.Root_WorldSpace.gameObject.SetActive(state);
		}

		[CanBeNull]
		public static StatusPanel GetUI(Fighter fighter)
		{
			return _huds.SafeGet(fighter);
		}

		public static bool TryGetUI([NotNull] Fighter fighter, out StatusPanel panel)
		{
			return _huds.TryGetValue(fighter, out panel);
		}

		public static void Clear()
		{
			foreach (StatusPanel hud in _huds.Values)
			{
				Addressables2.ReleaseInstanceSafe(hud.gameObject);
			}

			_huds.Clear();
			_all.Clear();
		}

		public void SnapToEnd()
		{
			foreach (StatusPanel ui in _all)
			{
				ui.SnapToEnd();
			}
		}

		public static void SetHighlight(bool b)
		{
			for (var i = 0; i < _all.Count; i++)
			{
				StatusPanel panel = _all[i];
				selection.Set(ref panel.selectState, b);
			}
		}

		public static void SetHighlight([NotNull] Fighter fighter, bool b)
		{
			if (_huds.TryGetValue(fighter, out StatusPanel ui))
			{
				selection.Set(ref ui.selectState, b);
			}
		}

		public static void SetHighlight([NotNull] Target target, bool b)
		{
			foreach (Fighter fighter in target.fighters)
			{
				SetHighlight(fighter, b);
			}

			foreach (Slot slot in target.slots)
			{
				if (slot.owner != null)
					SetHighlight(slot.owner, b);
			}
		}

		private void Update()
		{
			foreach (StatusPanel ui in _all)
			{
				selection.Update(ref ui.selectState, ref SelectStyle);
			}
		}


		public static void Hurt([NotNull] Fighter fter)
		{
			if (_huds.TryGetValue(fter, out StatusPanel ui))
			{
				ui.Hurt();
			}
		}
	}
}