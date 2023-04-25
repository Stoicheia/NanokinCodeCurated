using Anjin.Util;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Interactables
{
	[AddComponentMenu("Anjin: Events/Cutscene on Interact")]
	public class CutsceneOnInteract : SerializedMonoBehaviour
	{
		public Cutscene Cutscene;

		private void OnEnable()
		{
			Interactable interactable = gameObject.GetOrAddComponent<Interactable>();
			interactable.OnInteract.AddListener(OnInteract);
		}

		private void OnDisable()
		{
			Interactable interactable = GetComponent<Interactable>();
			interactable.OnInteract.RemoveListener(OnInteract);
		}

		private void Start()
		{
			if (Cutscene == null)
				Cutscene = GetComponent<Cutscene>();
		}

		private void OnInteract()
		{
			Cutscene.Play();
		}
	}
}