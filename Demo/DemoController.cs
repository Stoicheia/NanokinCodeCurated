using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Nanokin;
using Cysharp.Threading.Tasks;
using Overworld.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class DemoController : MonoBehaviour
{
	private const string DEMO_TUTORIAL_SCREEN = "SplashScreens/Demo_Start";
	private const string DEMO_END_SCREEN = "SplashScreens/Demo_Finish";



	[Button]
    private async UniTask<ISplashScreen> ShowTutorial()
    {
	    GameController.Live.State_WorldPause = GameController.WorldPauseState.FullPaused;
	    return await SplashScreens.ShowPrefabAsync(DEMO_TUTORIAL_SCREEN, () => GameController.Live.State_WorldPause = GameController.WorldPauseState.Running);
    }

    [Button]
    private async UniTask<ISplashScreen> ShowEndDemo()
    {
	    GameController.Live.State_WorldPause = GameController.WorldPauseState.FullPaused;
	    return await SplashScreens.ShowPrefabAsync(DEMO_END_SCREEN, () => GameController.Live.State_WorldPause = GameController.WorldPauseState.Running);
    }
}
