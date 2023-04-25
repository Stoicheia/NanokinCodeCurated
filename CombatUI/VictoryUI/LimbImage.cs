using System;
using Anjin.Util;
using API.Spritesheet.Indexing;
using API.Spritesheet.Indexing.Runtime;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEditor.AI.Anjin;
using UnityEngine;
using UnityEngine.UI;
using Util.Addressable;
using Util.Animation;
using Vexe.Runtime.Extensions;

namespace Combat.Components.VictoryScreen.Menu
{
	public class LimbImage : MonoBehaviour
	{
		private Image[]          _images;
		private SpriteAnim[]     _anims;
		private NanokinLimbAsset _limb;
		private Vector2[]        _limbOffsets;

		[NonSerialized]
		public SpriteAnim rootAnim;

		[ShowInInspector]
		public NanokinLimbAsset Limb
		{
			get => _limb;
			set
			{
				#if UNITY_EDITOR
				if (!Application.IsPlaying(gameObject))
				{
					if (value == null)
						return;

					Awake();

					IndexedSpritesheetAsset asset  = value.Spritesheet.TryLoadInEditMode<IndexedSpritesheetAsset>();
					Sprite                  sprite = asset.spritesheet.Spritesheet[0].Sprite;

					MoveImagesToBounds(sprite);
					return;
				}
				#endif

				SetLimbAsync(value).ForgetWithErrors();
			}
		}

		private void Awake()
		{
			_images = GetComponentsInChildren<Image>();
			_anims  = GetComponentsInChildren<SpriteAnim>();

			rootAnim = gameObject.AddComponent<SpriteAnim>();

			// Set anims to proxy
			foreach (SpriteAnim anim in _anims)
			{
				anim.Proxy = this.rootAnim;
			}

			// Get the base offsets
			_limbOffsets = new Vector2[_anims.Length];

			for (var i = 0; i < _anims.Length; i++)
			{
				SpriteAnim anim = _anims[i];
				_limbOffsets[i] = anim.GetComponent<RectTransform>().anchoredPosition;
			}
		}

		public async UniTask SetLimbAsync([NotNull] NanokinLimbAsset limbAsset, bool animated = false)
		{
			_limb = limbAsset;

			IndexedSpritesheetAsset spritesheet = await Addressables2.LoadHandleAsyncSlim<IndexedSpritesheetAsset>(limbAsset.SpritesheetAddress);

			foreach (SpriteAnim anim in _anims)
			{
				anim.Spritesheets.ClearAndAdd(spritesheet);
				this.rootAnim.ApplyChanges();
			}

			rootAnim.Spritesheets.ClearAndAdd(spritesheet);
			rootAnim.ApplyChanges();

			rootAnim.Play("idle", PlayOptions.ForceReset);
			if (!animated)
			{
				rootAnim.player.Paused = true;
			}

			MoveImagesToBounds_CurrentAnimFirstFirst();
		}

		/// <summary>
		/// Move the images so the origin of this root is centered on the image.
		/// </summary>
		public void MoveImagesToBounds_CurrentAnimFirstFirst()
		{
			Sprite firstSprite = rootAnim.player.currentAnimation.frames[0].Sprite;
			if (firstSprite == null)
				return;

			for (var i = 0; i < _anims.Length; i++)
			{
				SpriteAnim    anim = _anims[i];
				RectTransform rect = anim.GetComponent<RectTransform>();
				// rect.anchoredPosition = new Vector2(-_limb.Bounds.x, _limb.Bounds.y);


				MoveImageToBounds(rect, firstSprite, _limbOffsets[i]);
			}
		}

		/// <summary>
		/// Move the images so the origin of this root is centered on the image.
		/// </summary>
		public void MoveImagesToBounds(Sprite sprite)
		{
			for (var i = 0; i < _anims.Length; i++)
			{
				SpriteAnim    anim = _anims[i];
				RectTransform rect = anim.GetComponent<RectTransform>();
				// rect.anchoredPosition = new Vector2(-_limb.Bounds.x, _limb.Bounds.y);

				MoveImageToBounds(rect, sprite, _limbOffsets[i]);
			}
		}

		private void MoveImageToBounds([NotNull] RectTransform rt, [NotNull] Sprite sprite, Vector2 offset)
		{
			Rect    bounds = SpriteTrimmingCache.GetTrimmedBounds(sprite);
			Vector2 center = bounds.center; //- sprite.size() / 2;

			rt.anchoredPosition = new Vector2(-center.x, center.y) * rt.localScale.xy() + offset;
			rt.sizeDelta        = sprite.size();
		}
	}
}