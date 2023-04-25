using Anjin.Core.Flags;
using Cysharp.Threading.Tasks;
using Overworld.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplashScreenTrigger : MonoBehaviour
{
	[SerializeField] private string flagKey;
	[SerializeField] private string splashAddress;

	private bool triggered;

    // Start is called before the first frame update
    void Start()
    {
		gameObject.SetActive(!Flags.GetBool(flagKey));

		triggered = !gameObject.activeSelf;
    }

	private void OnTriggerEnter(Collider other)
	{
		if (!triggered && (other.gameObject.layer == LayerMask.NameToLayer("Player")))
		{
			triggered = true;

			Flags.SetBool(flagKey, true);
			ShowSplashScreen().Forget();

			gameObject.SetActive(false);
		}
	}

	private async UniTask ShowSplashScreen()
	{
		await UniTask2.Seconds(1f);
		await SplashScreens.ShowPrefabAsync(splashAddress);
		await UniTask.WaitUntil(() => !SplashScreens.IsActive);
		await UniTask2.Seconds(0.75f);
	}
}
