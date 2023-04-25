using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Combat.UI
{
	public class SlotTile : SerializedMonoBehaviour
	{
		public float HighlightOpacity = 1;

						public Material   mainSelectionMaterial;
						public Material   persistentSelectionMaterial;
		public float      targetOpacity = 1;
		public float      opacity       = 1;
		public bool       highlighted;
		[NonSerialized] public Vector2Int coord;

						private Material  selectionMaterial;

		private void Awake()
		{
			gameObject.GetComponent<MeshRenderer>().material = mainSelectionMaterial;
			selectionMaterial = gameObject.GetComponent<MeshRenderer>().material;
		}

		public void SetHighlight(bool enable)
		{
			highlighted = enable;
			selectionMaterial.SetFloat(SP_HighlightToggle, enable ? 1 : 0);
		}

		public void ChangeMaterial(bool keepSelection)
		{
			gameObject.GetComponent<MeshRenderer>().material = (!keepSelection ? mainSelectionMaterial : persistentSelectionMaterial);
			selectionMaterial = gameObject.GetComponent<MeshRenderer>().material;
		}

		private void Start()
		{
			selectionMaterial.SetFloat(SP_RandomizeThis01, RNG.Float);
			selectionMaterial.SetFloat(SP_TileOpacity, 1);
		}

		private void Update()
		{
			opacity = Mathf.Lerp(opacity, highlighted ? Mathf.Max(targetOpacity, HighlightOpacity) : targetOpacity, 0.3f);
			selectionMaterial.SetFloat(SP_TileOpacity, highlighted ? 1 : opacity);
		}

		public static readonly int SP_RandomizeThis01 = Shader.PropertyToID("_Randomizethis01");
		public static readonly int SP_HighlightToggle = Shader.PropertyToID("_HighlightToggle");
		public static readonly int SP_TileOpacity     = Shader.PropertyToID("_TileOpacityMultiplier");
	}
}