using System.Collections.Generic;
using System.Linq;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Puppets.Render;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Combat.UI.Info
{
	public class VirtualHomeUI : StaticBoy<VirtualHomeUI>
	{
		[SerializeField] private bool _useIndicatorsOverride;
		[SerializeField] [ShowIf("_useIndicatorsOverride")]
		private GameObject _overrideAllIndicators; //for testing purposes
		[SerializeField] private Transform                              _root;
		private static           Dictionary<Fighter, GameObject>        _getGraphics;
		private static           Dictionary<Fighter, (Transform, bool)> _getVirtualHome;
		private static           HashSet<Fighter>                       _doNotRender;
		private static           IRenderStrategy                        _renderer;
		private static           bool                                   _initialised;
		public static            Battle                                 battle;


		public override void Awake()
		{
			base.Awake();
			_getGraphics    = new Dictionary<Fighter, GameObject>();
			_getVirtualHome = new Dictionary<Fighter, (Transform, bool)>();
			_doNotRender    = new HashSet<Fighter>();
			_initialised    = false;
		}

		private void Update()
		{
			if (_initialised)
			{
				UpdateFighterPositions();
				UpdateDisplays();
			}
		}

		public static void Initialize([NotNull] Battle b)
		{
			battle       = b;
			_initialised = true;
			_getVirtualHome.Clear();
			foreach (KeyValuePair<Fighter, GameObject> g in _getGraphics)
			{
				Destroy(g.Value);
			}

			_getGraphics.Clear();
		}

		public async UniTask InitialiseFighterGraphics(bool instantiate = false)
		{
			List<Fighter> fighters = battle.fighters;
			foreach (Fighter f in fighters)
			{
				await InitialiseGraphicsFor(f, instantiate);
			}
		}

		private async UniTask InitialiseGraphicsFor(Fighter f, bool instantiate = false)
		{
			GameObject indicator;
			if (_useIndicatorsOverride)
				indicator = _overrideAllIndicators;
			else
				indicator = await f.ShadowPrefab;

			GameObject physicalIndicator = instantiate
				? Instantiate(indicator, _root)
				: indicator;

			if (physicalIndicator == null)
			{
				Debug.LogWarning($"This fighter {f.Name} does not have a silhouette. It will never be rendered.");
				_doNotRender.Add(f);
				return;
			}

			physicalIndicator.SetActive(false);
			_getGraphics.Add(f, physicalIndicator);
		}

		private void UpdateFighterPositions()
		{
			_getVirtualHome.Clear();

			foreach (Fighter f in battle.fighters)
			{
				if (_doNotRender.Contains(f)) continue;
				if (f.existence == Existence.Dead) continue;

				Slot fighterSlot = f.HomeTargeting;
				bool displayHome = !f.VirtuallyAtHome;

				Transform slot = SlotUI.GetSlotPhysical(fighterSlot);
				if (slot != null)
					_getVirtualHome.Add(f, (SlotUI.GetSlotPhysical(fighterSlot), displayHome));
			}
		}

		private void UpdateDisplays()
		{
			foreach (Fighter f in battle.fighters)
			{
				if (_doNotRender.Contains(f)) continue;
				if (!_getVirtualHome.ContainsKey(f))
					InitialiseGraphicsFor(f).Forget();

				(Transform home, bool show) = _getVirtualHome[f];

				if (_getGraphics.ContainsKey(f))
				{
					GameObject graphic = _getGraphics[f];
					if (show)
					{
						graphic.transform.position = home.position;
						graphic.SetActive(true);

						FighterActor actor = graphic.GetComponent<FighterActor>();
						if (actor != null)
							actor.facing = f.facing;
					}
					else
					{
						graphic.SetActive(false);
					}
				}
			}
		}

		public static void Kill()
		{
			_initialised = false;
			battle       = null;
			foreach (KeyValuePair<Fighter, GameObject> g in _getGraphics)
			{
				Destroy(g.Value);
			}

			_getGraphics.Clear();
			_getVirtualHome.Clear();
		}

		public static FighterActor GetFighterCaster(Fighter f)
		{
			Live.UpdateFighterPositions();
			Live.UpdateDisplays();

			GameObject graphics = _getGraphics[f];
			bool       show     = _getVirtualHome[f].Item2;

			return graphics == null || !show
				? null
				: graphics.GetComponent<FighterActor>();
		}
	}
}