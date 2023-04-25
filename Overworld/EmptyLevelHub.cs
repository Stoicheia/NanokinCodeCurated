using Anjin.Nanokin;
using Anjin.Util;
using TMPro;
using UnityEngine;

namespace Anjin
{
	public class EmptyLevelHub : MonoBehaviour
	{
		public Level 			Level;
		public LevelManifest	Manifest;
		public TextMeshPro 		Label;

		public void Start()
		{
			if (!Level)
				Level = gameObject.scene.FindRootComponent<Level>();

			if(Level && Level.Manifest)
				Label.text = Level.Manifest.name;
			else if (Manifest) {
				Label.text = Manifest.name;
 			}
 		}

 	}
 }