using Anjin.Util;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Data.Shops;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.UniTween.Value;

namespace Overworld.Shopping
{
	/// <summary>
	/// Joins the shop UI to the overworld.
	/// This is what we use in the overworld code to use the Shop UI.
	/// </summary>
	public class ShopNPC : MonoBehaviour
	{
// @formatter:off
		[FormerlySerializedAs("_vcam"),SerializeField]
		public CinemachineVirtualCamera VCam;

		[FormerlySerializedAs("_menuNpcOffset"), LabelText("Offset")]
		public Vector3 NPCMenuOffset;
// @formatter:on

		private void Awake()
		{
			VCam.Priority = -1;
		}

		private void Update() { }
	}
}