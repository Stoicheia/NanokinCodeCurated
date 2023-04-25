using System;
using System.Collections.Generic;
using Anjin.Util;
using API.Spritesheet.Indexing;
using API.Spritesheet.Indexing.Runtime;
using JetBrains.Annotations;
using Overworld.Rendering;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class CharacterRig : MonoBehaviour
	{
		/// <summary>
		/// Parts to this rig.
		/// This is both used for the serialized and runtime data.
		/// </summary>
		[OnValueChanged("OnPartsChanged", true)]
		[DisableInPlayMode]
		[ListDrawerSettings(CustomRemoveIndexFunction = "OnRemovePart", CustomAddFunction = "OnAddPart")]
		public List<Part> Parts = new List<Part>();

		/// <summary>
		/// Root transform where the spawned/baked objects will be added.
		/// This root can have a SpriteRenderer as a placeholder
		/// which gets disabled on baking.
		/// It must have an AnimableSpritesheet, or else it will be
		/// added automatically on startup.
		/// </summary>
		[DisableInPlayMode]
		[Optional]
		public Transform Root = null;

		/// <summary>
		/// Base material for the parts.
		/// </summary>
		[DisableInPlayMode]
		[Required]
		public Material Material = null;

		/// <summary>
		/// Indicates that the rig is baked ahead of time in
		/// edit mode. When this is set, the baked objects
		/// are updated as values are changed in the inspector.
		/// Do not set this from code.
		/// Use Bake() instead.
		/// </summary>
		[Space]
		[SerializeField]
		[HideInInspector]
		public bool Baked = false;

		/// <summary>
		/// Indicates that the rig will spawn itself on startup.
		/// Otherwise, it must be spawned manually through code.
		/// </summary>
		[ShowIf("@!Baked")]
		[DisableInPlayMode]
		public bool SpawnOnStart = true;

		[NonSerialized]
		public bool spawned;

		private SpriteAnim    _animableProxy;
		private ActorRenderer _renderer;

		[Serializable]
		public struct Part
		{
			public string Name;

			// For reference
			/// <summary>
			/// Indicates that the part was baked ahead of time.
			/// </summary>
			[HideInInspector]
			public bool Baked;

			//[HideInInspector]
			public GameObject GameObject;

			public Vector3                       BaseOffset;
			public Vector3                       Offset;
			public List<IndexedSpritesheetAsset> Sheets;

			[CanBeNull]
			public ColorReplacementProfile[] Colors;

			[NonSerialized]
			public bool spawned;

			[NonSerialized]
			public SpriteRenderer spriteRenderer;

			[NonSerialized]
			public Transform transform;

			[NonSerialized, CanBeNull]
			public ColorReplacementSetter colorSetter;

			[NonSerialized, CanBeNull]
			public SpriteAnim animable;
		}

		private void Awake()
		{
			if (Root == null)
				Root = transform;

			UpdateReferences();

			// We can put a placeholder SpriteRenderer on the View in case
			// we bake only at runtime.
			SpriteRenderer placeholder = Root.GetComponent<SpriteRenderer>();

			_renderer = GetComponentInChildren<ActorRenderer>();

			if (placeholder != null)
				placeholder.enabled = false;

			if (Baked || SpawnOnStart)
			{
				Spawn();
			}
		}

		private void OnDestroy()
		{
			CharacterRigSystem.Remove(this);
		}

		private void UpdateReferences()
		{
			if (_animableProxy == null)
			{
				if (!TryGetComponent(out _animableProxy) && !Root.TryGetComponent(out _animableProxy))
				{
					_animableProxy = Root.AddComponent<SpriteAnim>();
				}
			}
		}

		// private Material _regularMaterial;

		/// <summary>
		/// Completely clear the rig to a clean slate, removing all parts.
		/// </summary>
		public void Clear()
		{
			for (var i = 0; i < Parts.Count; i++)
			{
				Part part = Parts[i];
				part.GameObject.Destroy();
			}

			Parts.Clear();
		}

		/// <summary>
		/// Despawn the spawned parts.
		/// </summary>
		public void Despawn(bool removeBake = false)
		{
			if (spawned)
				CharacterRigSystem.Remove(this);

			for (var i = 0; i < Parts.Count; i++)
			{
				Part part = Parts[i];
				if (part.spawned || part.Baked && removeBake)
				{
					part.GameObject.Destroy();
					part.GameObject = null;
					part.spawned    = false;
				}

				Parts[i] = part;
			}

			spawned = false;
			Baked   = false;

#if UNITY_EDITOR
			if (!Application.isPlaying)
				UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		/// <summary>
		/// Spawn the parts to real gameobjects.
		/// Already spawned parts will be skipped.
		/// </summary>
		public void Spawn()
		{
			Profiler.BeginSample("CharacterRig.Bake");

			if (!spawned)
				CharacterRigSystem.Add(this);

			spawned = true;

			for (var i = 0; i < Parts.Count; i++)
			{
				Part part = Parts[i];
				if (part.Baked)
				{
					if (!part.spawned)
					{
						part.spawned = true;

						part.transform      = part.GameObject.transform;
						part.colorSetter    = part.GameObject.GetComponent<ColorReplacementSetter>();
						part.animable       = part.GameObject.GetComponent<SpriteAnim>();
						part.spriteRenderer = part.GameObject.GetComponent<SpriteRenderer>();
					}

					Parts[i] = part;
					continue;
				}

				part.spawned = true;

				if (part.GameObject == null)
				{
					var go = new GameObject($"Part.{part.Name}");
					go.transform.SetParent(Root, false);
					go.transform.localRotation = Quaternion.identity;
					go.transform.localPosition = part.BaseOffset + part.Offset;

					part.GameObject = go.gameObject;
					part.transform  = go.transform;
				}

				// Renderer
				// ----------------------------------------
				part.spriteRenderer                = part.GameObject.GetOrAddComponent<SpriteRenderer>();
				part.spriteRenderer.sharedMaterial = Material;
				part.spriteRenderer.sprite         = part.Sheets.SafeGet(0)?.spritesheet.Spritesheet[0].Sprite;

				// Animable
				// ----------------------------------------
				part.animable              = part.GameObject.GetOrAddComponent<SpriteAnim>();
				part.animable.Spritesheets = part.Sheets;
				part.animable.Proxy        = _animableProxy;

				if (_renderer)
					part.animable.onRegisteredInSystem += _renderer.OnSpriteChange;

				// Colors
				// ----------------------------------------
				ColorReplacementProfile[] profiles = part.Colors;
				if (profiles?.Length > 0)
				{
					part.colorSetter = part.GameObject.GetOrAddComponent<ColorReplacementSetter>();

					part.colorSetter.Profiles.Clear();

					for (var j = 0; j < profiles.Length; j++)
					{
						if (profiles[j] != null)
							part.colorSetter.Profiles.Add(profiles[j]);
					}

					if (Application.isPlaying)
						part.colorSetter.UpdateMaterialProperties();
				}

				Parts[i] = part;
			}

			Profiler.EndSample();
		}

		// private void LateUpdate()
		// {
		// // This comes from SpriteAnimator TODO Use VFXManager instead to do this (used to be combat only)
		// if (!ManualMaterialUpdate)
		// {
		// 	// Update the opacity of the sprite.
		// 	mainRenderer.color    = mainRenderer.color.Alpha(opacity);
		// 	mainRenderer.material = opacity < 1 ? GameAssets.Live.MatSpritesTransparent : _regularMaterial;
		//
		// 	for (var i = 0; i < additionalRenderers.Count; i++)
		// 	{
		// 		SpriteRenderer rend = additionalRenderers[i];
		// 		rend.color    = rend.color.Alpha(opacity);
		// 		rend.material = opacity < 1 ? GameAssets.Live.MatSpritesTransparent : _regularMaterial;
		// 	}
		// }
		// }


#if UNITY_EDITOR
		[ShowIf("@!Baked")]
		[Button]
		[LabelText("@!Baked ? \"Bake\" : \"Update Bake\"")]
		public void Bake()
		{
			Bake(true);
		}

		[ShowIf("@Baked")]
		[Button]
		private void UndoBake()
		{
			Bake(false);
		}

		public void Bake(bool enable)
		{
			if (enable == Baked) return;
			if (!Baked && !enable) return; // Nothing to do since we already aren't baked.

			if (Baked)
				Despawn(true);

			if (enable)
			{
				UpdateReferences();
				Spawn();
			}

			Baked = enable;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		private void setBake(bool enable)
		{
			Baked = enable;
			for (var i = 0; i < Parts.Count; i++)
			{
				Part part = Parts[i];
				part.Baked = enable;
				Parts[i]   = part;
			}

			SpriteRenderer placeholder = Root.GetComponent<SpriteRenderer>();
			if (placeholder)
				placeholder.enabled = !enable; // disable the placeholder if we're baked
		}

		[UsedImplicitly]
		public void OnPartsChanged()
		{
			Debug.Log("OnPartsChanged");
			if (Baked)
			{
				Despawn(true);
				Bake();
			}
		}

		public void OnRemovePart(int idx)
		{
			if (Parts[idx].Baked)
				Parts[idx].GameObject.Destroy();

			Parts.RemoveAt(idx);
		}

		public void OnAddPart()
		{
			Parts.Add(new Part());
		}


#endif
	}
}