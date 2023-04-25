using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using API.PropertySheet.Elements;
using API.PropertySheet.Runtime;
using API.PropertySheet.Runtime.Extensions;
using Data.Nanokin;
using JetBrains.Annotations;
using Overworld;
using Puppets;
using Puppets.Render;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using UnityUtilities;
using Util;
using Util.Odin.Attributes;
using Object = UnityEngine.Object;

namespace Combat.Entities
{
	public class MultiSpritePuppet : WorldPuppet, IRenderStrategy
	{
		public const string LIMB_OBJECT_NAME            = "limb.sprite"; // Name of the game objects with the SpriteRenderer on it.
		public const float  UNIVERSAL_WALL_PLANE_OFFSET = 1.1f;

		[DisableInPlayMode]
		[SerializeField]
		[Optional]
		private GameObject LimbPrefab;

		[DisableInPlayMode]
		[SerializeField]
		[Optional]
		public Material LimbMaterial;

		[DisableInPlayMode]
		[SerializeField]
		[Optional]
		public Material TransparencyMaterial;

		[SerializeField]
		private Material SilhouetteMaterial;

		[SerializeField]
		private Color SilhouetteTint;

		[DisableInPlayMode]
		[SerializeField] public Transform VisualTransform;

		[DisableInPlayMode]
		[SerializeField]
		private PuppetBillboardModes BillboardMode;

		[SerializeField]
		private float LimbZDistanceTransform;

		[SerializeField]
		private float LimbZDistance;

		private readonly List<Limb>                    _activeList       = new List<Limb>();
		private readonly List<Limb>                    _inactiveList     = new List<Limb>(8);
		private readonly Dictionary<LimbComp, Limb>    _limbDic          = new Dictionary<LimbComp, Limb>();
		private readonly Dictionary<string, Transform> _nodeTransformDic = new Dictionary<string, Transform>(8);

		private Vector3    _visualCenterOffset;
		private bool       _xflip;
		private PuppetComp _composition;

		private static readonly int _wallPlane = Shader.PropertyToID("_WallPlane");

		public override bool IsSilhouette
		{
			get => isSilhouette;
			set
			{
				isSilhouette = value;
				_inactiveList.Clear();
				_activeList.Clear();
			}
		}


		public event Action Rendered;

		protected override IRenderStrategy RenderStrategy => this;

		protected override void Awake()
		{
			base.Awake();

			// TODO shouldn't this be elsewhere? Doesn't seem specific to MultiSpritePuppet
			puppetPlayer.Initialized += () =>
			{
				puppetPlayer.linker.LinkPuppet(this, puppetState);

				puppetPlayer.ElementEntered += element =>
				{
					if (element is SpriteElement) puppetPlayer.linker.Link(element, GenericSpriteExtension.Create());
					if (element is SpritesheetElement) puppetPlayer.linker.Link(element, GenericSpritesheetExtension.Create());
				};
			};

			actor.visualTransform = VisualTransform;
		}

		public bool FlipX
		{
			get => _xflip;
			set
			{
				_xflip = value;
				UpdateFlip();
			}
		}

		protected override Vector3 PuppetCenter
		{
			get
			{
				// TODO figure out a way to do this once at the start (the positioning is off for some reason if I do that)
				if (!HasPuppet)
					return transform.position;

				var centroid = new Centroid();
				foreach (Limb limb in _activeList)
				{
					centroid.add(limb.tfpivot.position);
				}

				_visualCenterOffset = centroid.get() - transform.position;

				return _visualCenterOffset + transform.position;
			}
		}

		public void SetBillboardMode(PuppetBillboardModes value)
		{
			BillboardMode = value;

			if (value == PuppetBillboardModes.Puppet)
			{
				VisualTransform.AddComponent<Billboard>();
			}
		}

		public override void OnPuppetAnimControled(ISceneSpace sceneSpace)
		{
			base.OnPuppetAnimControled(sceneSpace);
			sceneSpace.Flips = new SpriteFlips(FlipX, false);
		}

		// public override void OnPuppetAnimFrame([NotNull] ISceneSpace sceneSpace, int idxFrame)
		// {
		//		sceneSpace.Flips = new SpriteFlips(FlipX, false);
		// }

		protected override void LateUpdate()
		{
			base.LateUpdate();

			if (HasPuppet)
			{
				Profiler.BeginSample("MultiSpritePuppet get azimuth");
				float theta = MathUtil.ToWorldAzimuthBlendable(actor.facing);
				FlipX = theta < 0.5f;
				Profiler.EndSample();

				// Apply VFX state
				// ----------------------------------------
				foreach (Limb limb in _activeList)
				{
					Profiler.BeginSample("MultiSpritePuppet apply limb VFX");
					Color tint = IsSilhouette ? SilhouetteTint : limb.comp.visual.tint;
					limb.SetMaterial(
						tint * vfx.state.tint.Alpha(vfx.state.opacity),
						vfx.state.fill,
						vfx.state.emissionPower);

					VisualTransform.localPosition = vfx.state.offset;
					VisualTransform.localScale    = GetLocalScale();
					Profiler.EndSample();
				}
			}

			// Scale down on the Y the further up we are, gives a neat perspective effect on the billboard sprites.
			// float yDist = CameraController.Live.transform.position.y - 15 - VisualTransform.position.y;
			// if (yDist > 0)
			// 	VisualTransform.localScale = Vector3.one - Vector3.up * (yDist / 160f);
		}

		public override void ClearView()
		{
			foreach (Transform v in VisualTransform)
			{
				Destroy(v.gameObject);
			}

			_nodeTransformDic.Clear();
		}

		private void UpdateFlip()
		{
			VisualTransform.localScale = GetLocalScale();
		}

		private Vector3 GetLocalScale()
		{
			Vector3 scale          = Vector3.one; // vfx?.state.scale ?? Vector3.one;
			if (vfx != null) scale = vfx.state.scale;
			if (_xflip) scale.x    = -scale.x;

			return scale;
		}

		public void ReplaceSpritesheet(string limbName, API.Spritesheet.Indexing.IndexedSpritesheetAsset asset)
		{
			bool success = System.Enum.TryParse(limbName, out LimbType type);

			if (success)
			{
				for (int i = 0; i < _activeList.Count; i++)
				{
					LimbState limbState = _activeList[i].state;

					if (limbState != null)
					{
						Assets.Nanokins.NanokinLimbAsset limbAsset = limbState.asset as Assets.Nanokins.NanokinLimbAsset;

						if ((limbAsset != null) && (limbAsset.Kind == type))
						{
							limbState.Spritesheet = asset.spritesheet;

							break;
						}
					}
				}
			}
		}

		public override Transform GetNodeTransform(string id)
		{
			if (_nodeTransformDic.TryGetValue(id, out Transform tf))
				return tf;

			return null;
		}

		public override Vector3 GetNodePosition(string id)
		{
			return _nodeTransformDic.TryGetValue(id, out Transform tf)
				? tf.position
				: Vector3.zero; // TODO maybe return something better
		}

		public void Render([NotNull] PuppetComp composition)
		{
			_composition = composition;
			UpdateFlip();

			// Add as many views as we need to ActiveLimbs.
			// ----------------------------------------
			while (_activeList.Count < composition.limbs.Count)
			{
				bool hasPooledLimbs = _inactiveList.Count > 0;
				if (hasPooledLimbs)
				{
					Limb limb = _inactiveList[0];
					_inactiveList.RemoveAt(0);
					_activeList.Add(limb);
					limb.Reset();
				}
				else
				{
					Limb limb;

					// TODO remove this (terrible separation of concern and code duplication)
					if (!IsSilhouette)
					{
						limb = new Limb(
							this,
							VisualTransform,
							LimbPrefab,
							LimbMaterial.InstantiateNew(),
							TransparencyMaterial.InstantiateNew());
					}
					else
					{
						limb = new Limb(
							this,
							VisualTransform,
							LimbPrefab,
							SilhouetteMaterial.InstantiateNew(),
							SilhouetteMaterial.InstantiateNew());
					}

					if (BillboardMode == PuppetBillboardModes.Limb)
					{
						limb.spriteRenderer.gameObject.AddComponent<Billboard>();
					}

					_activeList.Add(limb);
				}
			}

			var i = 0;

			_limbDic.Clear();

			while (i < composition.limbs.Count)
			{
				// Update the views we need according to the composition.
				Limb     limb  = _activeList[i];
				LimbComp lcomp = composition.limbs[i];

				_limbDic[lcomp] = limb;
				limb.Inactive   = false;

				UpdateLimb(limb, lcomp);

				foreach (NodeInfo node in limb.state.data.Nodes)
				{
					string id = node.ID;
					if (node.Type == NodeTypes.Plug)
					{
						NodeData data = limb.comp.cell.ReadData(id);
						UpdateNode(limb, id, data);
					}
				}

				i++;
			}

			// Parent each view to its parent
			// ----------------------------------------
			for (int j = 0; j < composition.limbs.Count; j++)
			{
				// Build the hierarchy.
				LimbComp cself = composition.limbs[j];
				if (cself.isRoot)
					continue;

				LimbComp cparent = composition.GetParentLimb(cself);

				Limb lself   = _limbDic[cself];
				Limb lparent = _limbDic[cparent];

				lself.tfpivot.transform.SetParent(lparent.tfpivot, true);
			}

			// Discard the rest of the active limbs we don't need.
			// ----------------------------------------
			while (i < _activeList.Count)
			{
				Limb l = _activeList[i++];

				l.Inactive = true;
				l.tfpivot.transform.SetParent(VisualTransform, true);

				// Cleanup nodes
				foreach (NodeInfo node in l.state.data.Nodes)
				{
					string id = node.ID;
					if (node.Type == NodeTypes.Plug)
					{
						NodeData data = l.comp.cell.ReadData(id);
						UpdateNode(l, id, data);
					}
				}

				_activeList.RemoveAt(i--);
				_inactiveList.Add(l);
			}

			Rendered?.Invoke();
		}


		private void UpdateNode(Limb limb, string id, NodeData nodeData)
		{
			if (!_nodeTransformDic.TryGetValue(id, out Transform tfnode))
			{
				tfnode        = new GameObject($"node-{id}").transform;
				tfnode.parent = limb.tfpivot;


				_nodeTransformDic[id] = tfnode;
			}

			Vector2 pos = nodeData.position - limb.comp.plugpos;
			float   rot = limb.comp.layout.rotation;
			tfnode.localPosition = pos * MathUtil.YDOWN_TO_UP * MathUtil.PIXEL_TO_WORLD;
			tfnode.localRotation = Quaternion.Euler(0, 0, rot);
		}

		public void UpdateLimb([NotNull] Limb limb, LimbComp comp)
		{
			limb.comp = comp;

			if (limb.state != comp.state)
			{
				// Different limb --> update the name.
				limb.tfpivot.name = $"Limb.{comp.link.idsock ?? "root"}";
				limb.state        = comp.state;
			}

			Sprite sprite = comp.sprite;

			if (sprite == null)
			{
				limb.Inactive = true;
				return;
			}

			// Update layout of pivot.
			Vector2 pivotpos = GetPivot(comp);

			// + value.selfLayout.position                 // Apply the source layout.
			// + value.parentLayout.position;    // Use the compounded parent's parameters.

			PuppetLayout selfParameters = comp.layoutSelf;
			limb.tfpivot.localPosition    = pivotpos * MathUtil.YDOWN_TO_UP * MathUtil.PIXEL_TO_WORLD;
			limb.tfpivot.localEulerAngles = new Vector3(0, 0, -selfParameters.rotation);
			limb.tfpivot.localScale       = new Vector3(selfParameters.scale.x, selfParameters.scale.y, 1);

			// Update layout of limb view transform.
			limb.tfimage.localPosition = (comp.sprite.size() / 2f - comp.plugpos) // Adjust SpriteRenderer's center origin to top-left then center on the plug.
			                             * MathUtil.DOWN_RIGHT
			                             * MathUtil.PIXEL_TO_WORLD
			                             + MathUtil.HALF_PIXEL; // Align to center of pixel.

			if (limb.zsprite == null || !limb.zsprite.enabled)
				// Simple depth sorting with slight distance variations between the limbs.
				limb.tfimage.localPosition += LimbZDistanceTransform * comp.layer * -Vector3.forward;
			else
				// Automatic SpriteRenderer sorting based on distance from camera near-plane.
				// This is required instead of setting the SpriteRenderer sorting order ourselves,
				// since otherwise 2 different puppets could have weird sorting when they overlap one another.
				limb.zsprite.offset = comp.layer;

			limb.layer = comp.layer;

			// Update visuals.
			PuppetVisual visual = comp.visual;

			Color cColor = visual.tint;

			limb.spriteRenderer.color  = cColor;
			limb.spriteRenderer.sprite = sprite;
			limb.spriteRenderer.SetFlips(visual.flips);
		}

		private Vector2 GetPivot(LimbComp value)
		{
			Vector2 pivotpos = value.sockpos; // Move to the socket.
			pivotpos += value.layoutSelf.position;

			if (!value.isRoot)
			{
				LimbComp parent = _composition.GetParentLimb(value);
				pivotpos += parent.sockpos;
				pivotpos -= parent.plugpos;
			}

			return pivotpos;
		}

		private void OnDestroy()
		{
			// Cleanup
			foreach (Limb limb in _inactiveList)
				limb.Destroy();

			foreach (Limb limb in _activeList)
				limb.Destroy();
		}

		public class Limb
		{
			public readonly Transform      tfpivot;
			public readonly Transform      tfimage;
			public readonly SpriteRenderer spriteRenderer;

			public bool      inactive;
			public LimbState state;
			public LimbComp  comp;
			public ZSprite   zsprite;

			private readonly Material          _material;
			private readonly Material          _materialTransparency;
			private readonly MultiSpritePuppet puppet;

			public int layer;

			public Limb(MultiSpritePuppet puppet, Transform parentTransform, GameObject pfbSprite = null, Material material = null, Material materialTransparency = null)
			{
				this.puppet = puppet;

				tfpivot = new GameObject().transform;
				tfpivot.SetParent(parentTransform, false);

				_material             = material;
				_materialTransparency = materialTransparency;

				GameObject goSprite = pfbSprite != null
					? Instantiate(pfbSprite)
					: new GameObject
					{
						name = LIMB_OBJECT_NAME
					};


				var obj = goSprite.gameObject;
				tfimage = goSprite.transform;
				tfimage.SetParent(tfpivot, false);

				spriteRenderer          = goSprite.GetOrAddComponent<SpriteRenderer>();
				zsprite                 = goSprite.GetComponent<ZSprite>();
				spriteRenderer.material = material;
				if (puppet.IsSilhouette)
				{
					obj.layer = Layers.AboveEnv;
				}
			}

			public bool Inactive
			{
				get => inactive;
				set
				{
					bool hasChanged = inactive != value;
					inactive = value;

					if (hasChanged)
						tfpivot.gameObject.SetActive(inactive);
				}
			}

			public void Destroy()
			{
				tfpivot.gameObject.Destroy();
				tfimage.gameObject.Destroy();
			}

			public void Reset()
			{
				// TODO
			}

			public void SetMaterial(Color tint, Color fill, float emission)
			{
				Material mat = tint.a < 1 - Mathf.Epsilon
					? _materialTransparency
					: _material;

				spriteRenderer.material = mat;

				spriteRenderer.ColorFill(fill);
				spriteRenderer.EmissionPower(emission);
				spriteRenderer.ColorTint(tint);

				float wallPlaneReverseCorrection = puppet.IsSilhouette ? 1 : 0;

				mat.SetFloat(_wallPlane, wallPlaneReverseCorrection - UNIVERSAL_WALL_PLANE_OFFSET + layer * puppet.LimbZDistance);
			}
		}
	}
}


// public override Vector3 VisualForward
// {
// get
// {
// if (_strategy.FlipX)
// return -CameraController.Live.UnityCam.transform.right;
// return CameraController.Live.UnityCam.transform.right;
// }
// }