using System;
using Anjin.Util;
using JetBrains.Annotations;
using Overworld.Controllers;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Menu.Sticker
{
	public class StickerList : SerializedMonoBehaviour
	{
		[FormerlySerializedAs("PickerEntryPrefab"), SerializeField]
		private ListSticker EntryPrefab;
		[SerializeField] public RectTransform ListRoot;
		[SerializeField] public RectTransform ContentRoot;

		public ListSticker Selected { get; private set; }

		void Awake()
		{
			Selected = null;
		}

		[NotNull]
		public ListSticker Add()
		{
			ListSticker sticker = PrefabPool.Rent(EntryPrefab, ContentRoot);
			Sort();

			sticker.interactable = true;

			return sticker;
		}

		public void Sort()
		{
			ContentRoot.SortChildrenByName();
		}

		public void Remove([NotNull] ListSticker item)
		{
			PrefabPool.DestroyOrReturn(item);
			item = null;
		}

		public void Select(int index)
		{
			if (index < ContentRoot.childCount)
			{
				Selected = ContentRoot.GetChild(index).GetComponent<ListSticker>();
				Selected.Select();
			}
			else
			{
				Selected = null;
			}
		}

		public void Clear()
		{
			while (ContentRoot.childCount > 0)
			{
				PrefabPool.DestroyOrReturn(ContentRoot.GetChild(0).gameObject);
			}

			Selected = null;
		}
	}
}