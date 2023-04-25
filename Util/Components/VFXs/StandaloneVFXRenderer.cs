using Combat.Data.VFXs;
using Combat.Toolkit;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util.Components.VFXs {
	public class StandaloneVFXRenderer : MonoBehaviour {


		private VFXManager _vfxManager;
		private bool       _hasVFX;

		private Vector3 _basePosition;


		private void Awake()
		{
			_vfxManager = GetComponent<VFXManager>();
			_hasVFX     = _vfxManager != null;

			_basePosition = transform.position;
		}


		private void LateUpdate()
		{
			Vector3 position = _basePosition;


			if (_hasVFX) {
				VFXState state = _vfxManager.state;

				position += state.offset;
			}


			transform.position = position;
		}


		[Button, ShowInPlay]
		public void VFXTest(VFX vfx)
		{
			_vfxManager.Add(vfx);
		}

	}
}