using Assets.Scripts.Utils;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Scripting
{
	[AddComponentMenu("Anjin: Game Building/Breakable")]
	public class Breakable : MonoBehaviour
	{
		public bool     Persisted;
		public LuaAsset Script;
		public string   BreakFunction;

		[Title("Effects")]
		public AudioDef SFX_Break;
		public ParticlePrefab ParticlePrefab;
		public float          ScreenShake;
		public int            FreezeFrames;

		private void Start()
		{
			if (Persisted)
			{
				// SaveManager.current.LoadPersistence(gameObject);
			}
		}


		public void Break()
		{
			if (!isActiveAndEnabled)
				return;

			GameSFX.Play(SFX_Break, transform);
			Lua.RunScriptOrGlobal(BreakFunction, Script, new object[] {this});
			ParticlePrefab.Instantiate(transform);

			gameObject.SetActive(false);
			if (Persisted)
			{
				// SaveManager.current.SavePersistence(gameObject);
			}
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		private class BreakableProxy : LuaProxy<Breakable>
		{
			public void Break()
			{
				proxy.Break();
			}
		}
	}
}