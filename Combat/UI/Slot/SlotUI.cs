using System.Collections.Generic;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Odin.Attributes;

namespace Combat.UI
{
	public class SlotUI : StaticBoy<SlotUI>
	{
		[SerializeField, AddressFilter] private string  TileAddress;
		[SerializeField]                private Vector3 TileOffset;
		[SerializeField, Range01]       private float   DefaultOpacity = 0.75f;
		[SerializeField, Range01]
		public float HighlightOpacity = 0.5f;

		private static Dictionary<Slot, SlotTile> _tiles = new Dictionary<Slot, SlotTile>();

		public static Transform GetSlotPhysical(Slot slot)
		{
			if (_tiles[slot] == null) return null;
			return _tiles[slot].transform;
		}

		public static void Clear()
		{
			foreach (SlotTile slotTile in _tiles.Values)
			{
				// Addressables.ReleaseInstance(slotTile.gameObject);
				if (slotTile != null)
					Destroy(slotTile.gameObject);
			}

			_tiles.Clear();
		}

		public static void ResetOpacity()
		{
			SetOpacity(Live.DefaultOpacity);
		}

		/// <summary>
		/// Set the target opacity for all slots.
		/// </summary>
		public static void SetOpacity(float targetValue)
		{
			foreach (SlotTile tile in _tiles.Values)
			{
				tile.targetOpacity = targetValue;
			}
		}

		/// <summary>
		/// Set the target opacity for a slot.
		/// </summary>
		public static void SetOpacity([NotNull] Slot slot, float targetValue)
		{
			if (_tiles.TryGetValue(slot, out SlotTile tile))
			{
				tile.targetOpacity = targetValue;
			}
		}

		/// <summary>
		/// Enable highlighting for a slot.
		/// </summary>
		public static void SetHighlight([NotNull] Slot slot, bool enable = true, bool overridePersistentHighlight = false)
		{
			if (_tiles.TryGetValue(slot, out SlotTile tile) && (!slot.persistentHighlight || overridePersistentHighlight))
			{
				tile.SetHighlight(enable);
			}
		}

		public static void ChangeMaterial([NotNull] Slot slot, bool keepHighlighting = false)
		{
			if (_tiles.TryGetValue(slot, out SlotTile tile))
			{
				tile.ChangeMaterial(keepHighlighting);
			}
		}

		public static async UniTaskVoid AddSlot([NotNull] Slot slot)
		{
			GameObject @object = await Addressables.InstantiateAsync(Live.TileAddress,
				slot.actor.position + Live.TileOffset,
				slot.actor.transform.rotation, // Quaternion.Euler(0, battle.Arena.VCam.GetComponent<CinemachineOrbit>().BaseCoordinates.azimuth, 0);
				Live.gameObject.transform);

			// Lift off the ground by half bounds height
			// tile.position += Vector3.up * tile.GetComponent<MeshRenderer>().bounds.size.y / 2f + Live.TileOffset;

			// tile component
			SlotTile tile = @object.GetOrAddComponent<SlotTile>();

			tile.HighlightOpacity = Live.HighlightOpacity;

			tile.coord         = slot.coord;
			tile.targetOpacity = Live.DefaultOpacity;
			tile.opacity       = Live.DefaultOpacity;

			// register the tile
			_tiles[slot] = tile;

		}
	}
}