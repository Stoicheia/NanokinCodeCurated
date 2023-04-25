#define COLOR_REPLACEMENT_SAFTEYCHECKS
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Rendering
{
	[DefaultExecutionOrder(1)]
	public class ColorReplacementSetter : MonoBehaviour
	{
		public const int    MAX_REPLACEMENT_COLORS = 24;
		public const string KEYWORD_NAME           = "_COLOR_REPLACE";

		public bool Enabled = true;

		public List<ColorReplacementProfile> Profiles = new List<ColorReplacementProfile>();

		[Range(0f, 1f)]
		public float ReplaceRange = 0.05f;

		private SpriteRenderer _rend;

		private Material _originalMat;
		private Material _colorReplacementEnabledMat;

		private bool _init;

		// MANAGER
		// ----------------------------------------
		public static List<ColorReplacementSetter> all;
		public static bool                         managerRegistered;

		public static Dictionary<int, CachedBlock> CachedPropertyBlocks;

		[ShowInInspector]
		public static Dictionary<Material, Material> KeywordEnabledMaterials;

		private static List<Vector4> _scratchFromColors;
		private static List<Vector4> _scratchToColors;

		private static StringBuilder _sb;

		public struct CachedBlock
		{
			public MaterialPropertyBlock         block;
			public List<ColorReplacementProfile> profiles;
			public int                           hash;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void OnReg()
		{
			managerRegistered       = false;
			all                     = new List<ColorReplacementSetter>();
			CachedPropertyBlocks    = new Dictionary<int, CachedBlock>();
			KeywordEnabledMaterials = new Dictionary<Material, Material>();

			_scratchFromColors = new List<Vector4>();
			_scratchToColors   = new List<Vector4>();
			_sb                = new StringBuilder();
		}

		private void OnEnable()  => all.Add(this);
		private void OnDisable() => all.Remove(this);

		public static void OnOptionChange(bool val)
		{
			for (var i = 0; i < all.Count; i++)
			{
				if (all[i])
					all[i].UpdateMaterialProperties(val);
			}
		}

		// INSTANCE
		// ----------------------------------------

		public void Awake()
		{
			_rend = GetComponent<SpriteRenderer>();
		}

		private void Start()
		{
			UpdateMaterialProperties();

			if (!managerRegistered)
			{
				managerRegistered = true;
				GameOptions.current.sprite_color_replacement.AddHandler(OnOptionChange);
			}
		}

		public void InsureReady()
		{
			if (_init) return;
			_init = true;

			Init();
		}

		[Button]
		public void Init()
		{
			_rend        = GetComponent<SpriteRenderer>();
			_originalMat = _rend.sharedMaterial;

			if (KeywordEnabledMaterials.TryGetValue(_rend.sharedMaterial, out _colorReplacementEnabledMat))
			{
				_rend.sharedMaterial = _colorReplacementEnabledMat;
			}
			else
			{
				var newMat = new Material(_rend.sharedMaterial);
				newMat.name      = _rend.sharedMaterial.name + " (color replacement enabled)";
				newMat.hideFlags = HideFlags.None;
				// newMat.hideFlags = HideFlags.HideAndDontSave;
				newMat.EnableKeyword(KEYWORD_NAME);
				KeywordEnabledMaterials[_rend.sharedMaterial] = newMat;
				_rend.sharedMaterial                          = newMat;
				_colorReplacementEnabledMat                   = newMat;
			}
		}

		public void UpdateMaterialProperties()
		{
			UpdateMaterialProperties(GameOptions.current.sprite_color_replacement);
		}

		// TODO: This REALLY does not need to break!
		[ShowInInspector]
		public bool HashOfProfiles(List<ColorReplacementProfile> profiles, out int hash)
		{
			hash = 459;
			if (profiles == null || profiles.Count == 0) return false;

			for (int i = 0; i < profiles.Count; i++)
			{
				if (profiles[i] == null) continue;
				hash += profiles[i].ID.GetHashCode();
			}

			return true;
		}

		[Button, HideInEditorMode]
		public void UpdateMaterialProperties(bool replacementEnabled)
		{
			if (!Enabled) return;

			InsureReady();

			if (!_rend)
			{
				_rend = GetComponent<SpriteRenderer>();
			}

			if (_rend)
			{
				if (replacementEnabled && Profiles != null && Profiles.Count > 0)
				{
					_rend.sharedMaterial = _colorReplacementEnabledMat;

					if (!HashOfProfiles(Profiles, out int hash))
						return;

					CachedBlock block;

					if (!CachedPropertyBlocks.TryGetValue(hash, out block))
					{
						_scratchFromColors.Clear();
						_scratchToColors.Clear();

						int totalCount = 0;

						for (var i = 0; i < Profiles.Count; i++)
						{
							for (var j = 0; j < Profiles[i].colArray.Count; j++)
							{
								ColReplace col = Profiles[i].colArray[j];
								if (QualitySettings.activeColorSpace == ColorSpace.Linear)
								{
									_scratchFromColors.Add(col.from.linear);
									_scratchToColors.Add(col.to.linear);
								}
								else
								{
									_scratchFromColors.Add(col.from);
									_scratchToColors.Add(col.to);
								}

								totalCount++;
							}
						}

						// NOTE: Apparently you can't change the size of an array once you set it... FOR SOME FUCKING REASON.
						while (_scratchFromColors.Count < MAX_REPLACEMENT_COLORS)
						{
							_scratchFromColors.Add(Color.black);
						}

						while (_scratchToColors.Count < MAX_REPLACEMENT_COLORS)
						{
							_scratchToColors.Add(Color.black);
						}

						block.hash     = hash;
						block.profiles = Profiles;

						block.block = new MaterialPropertyBlock();
						block.block.SetFloat("_ColorReplaceRange", ReplaceRange);
						block.block.SetInt("_ColorsCounts", totalCount);
						block.block.SetVectorArray("_FromColors", _scratchFromColors);
						block.block.SetVectorArray("_ToColors", _scratchToColors);

						CachedPropertyBlocks[hash] = block;
					}
					else
					{
#if COLOR_REPLACEMENT_SAFTEYCHECKS
						// Safetycheck in case of hash collisions
						bool passed = true;

						if (block.profiles.Count != Profiles.Count)
							passed = false;

						for (int i = 0; i < Profiles.Count; i++)
						{
							if (block.profiles[i] != Profiles[i])
							{
								passed = false;
								break;
							}
						}

						if (!passed)
						{
							_sb.Clear();
							_sb.AppendLine("ColorReplacementSetter: MISMATCHED BLOCKS!");
							_sb.AppendLine($"Profiles hash: {hash}, Block hash: {hash}");
							_sb.Append("\n\nProfiles:");
							for (int i = 0; i < Profiles.Count; i++)
							{
								_sb.AppendLine($"\t {Profiles[i].name}");
							}

							_sb.Append($"\nBlock:");
							for (int i = 0; i < block.profiles.Count; i++)
							{
								_sb.AppendLine($"\t {block.profiles[i].name}");
							}

							Debug.LogError(_sb, this);
						}
#endif
					}

					if (block.block != null)
						_rend.SetPropertyBlock(block.block);
				}
				else
				{
					_rend.sharedMaterial = _originalMat;
				}

				// NOTE: I don't know why I have to do this, but otherwise the material doesn't have its texture retained. Very odd.
				_rend.gameObject.SetActive(false);
				_rend.gameObject.SetActive(true);
			}
		}
	}
}