using System;
using System.Collections.Generic;
using Anjin.Util;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.Components.VictoryScreen.Menu
{
	/// <summary>
	/// Shows a list of notifications vertically.
	/// </summary>
	public class NotificationRectStack : MonoBehaviour
	{
		public RectTransform Root;
		public float         Padding         = 1;
		public Vector2       StartingOffset  = new Vector2(0, -60);
		public float         DefaultDuration = 4f;
		[FormerlySerializedAs("Damping")]
		public float PositionDamping = 1;
		public float AlphaDamping = 1;
		public float ScaleDamping = 1;

		[NonSerialized]
		public List<Notif> activeNotifs;
		private RectTransform _rt;

		public RectTransform GetRoot() => Root != null ? Root : _rt;

		private void Awake()
		{
			activeNotifs = new List<Notif>();
			_rt          = GetComponent<RectTransform>();
		}

		[Button]
		public Notif PushPrefab([NotNull] Notif notif)
		{
			notif.go = Instantiate(notif.prefab, GetRoot());
			return Push(notif);
		}

		[CanBeNull]
		private Notif Push([NotNull] Notif notif)
		{
			if (!notif.go.TryGetComponent(out RectTransform rt))
			{
				this.LogError($"Invalid object '{notif}' given to ShowNotification, no RectTransform on it.");
				return null;
			}

			// Replace existing (soft)
			foreach (Notif n in activeNotifs)
			{
				if (n.id == notif.id)
					n.remainingSeconds = 0;
			}

			// Add new notif
			notif.Init();
			notif.remainingSeconds = notif.duration ?? DefaultDuration;

			rt.anchoredPosition = StartingOffset;
			rt.parent           = GetRoot();

			activeNotifs.Insert(0, notif);

			return notif;
		}

		private void LateUpdate()
		{
			float y = 0;

			for (var i = 0; i < activeNotifs.Count; i++)
			{
				Notif notif = activeNotifs[i];

				RectTransform rt   = notif.rt;
				Vector2       apos = rt.anchoredPosition;

				// Set layout
				rt.anchoredPosition = new Vector2(
					apos.x.LerpDamp(0, PositionDamping),
					apos.y.LerpDamp(y, PositionDamping)
				);

				// Add to spent layout
				y += rt.sizeDelta.y;
				y += Padding;

				// Lerp scale
				rt.localScale = rt.localScale.LerpDamp(rt.localScale, ScaleDamping);

				// Timer to death
				notif.remainingSeconds -= Time.deltaTime;
				if (notif.remainingSeconds <= 0)
				{
					notif.targetAlpha = 0;
					if (notif.cgroup.alpha <= 0)
					{
						// Die!!
						activeNotifs.RemoveAt(i--);
						Destroy(notif.go);
					}
				}

				// Lerp alpha
				notif.cgroup.alpha = notif.cgroup.alpha.LerpDamp(notif.targetAlpha, AlphaDamping);
			}
		}

		public class Notif
		{
			public GameObject prefab;
			public GameObject go;
			public float?     duration = null;

			public CanvasGroup   cgroup;
			public RectTransform rt;
			public float         remainingSeconds;
			public float         targetAlpha;
			public float         targetScale;

			public object id = null;

			public Notif()
			{
				targetAlpha = 1f;
				targetScale = 1f;
			}

			public void Init()
			{
				cgroup = go.GetOrAddComponent<CanvasGroup>();
				rt     = go.GetOrAddComponent<RectTransform>();
			}
		}
	}
}