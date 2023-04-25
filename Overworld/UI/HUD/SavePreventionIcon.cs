using Anjin.Nanokin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SavePreventionIcon : MonoBehaviour
{
	[SerializeField] private GameObject noSaveIcon;

    // Update is called once per frame
    void Update()
    {
		noSaveIcon.SetActive((GameController.Live.StateGame == GameController.GameState.Overworld) && !GameController.CanSave);
    }
}
