using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Data.Shops;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util.Addressable;

namespace Anjin.UI
{
	// public class LootNotifyHUD : StaticBoy<LootNotifyHUD>
	// {
	// 	public bool  Showing         = false;
	// 	public float DefaultDuration = 4;
	// 	public float Timer           = 0;
	// 	public float AlphaLerp       = 0.1f;
	//
	// 	public ILootAsset LootGained;
	//
	// 	public CanvasGroup     CanvasGroup;
	// 	public Image           LootSprite;
	// 	public TextMeshProUGUI LootNameText;
	// 	public TextMeshProUGUI LootDescText;
	//
	// 	private AsyncHandles _handles;
	//
	// 	protected override void OnAwake()
	// 	{
	// 		_handles = new AsyncHandles();
	// 		SetChildrenActive(false);
	// 	}
	//
	// 	void Update()
	// 	{
	// 		if (Showing) {
	// 			if (Timer < 0)
	// 				Showing = false;
	// 			else
	// 				Timer -= Time.deltaTime;
	// 		}
	//
	// 		CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, Showing ? 1 : 0, AlphaLerp);
	// 	}
	//
	// 	[Sirenix.OdinInspector.Button]
	// 	public async UniTask Show(ILootAsset loot)
	// 	{
	// 		if (Showing || loot == null) return;
	//
	// 		Showing = true;
	// 		SetChildrenActive(true);
	//
	// 		Timer = DefaultDuration;
	//
	// 		LootGained = loot;
	//
	// 		LootSprite.sprite = await loot.GetLootSprite(_handles);
	// 		//LootSprite.SetNativeSize();
	//
	// 		LootNameText.text = loot.LootName;
	// 		LootDescText.text = loot.LootDescription;
	// 	}
	// }
}