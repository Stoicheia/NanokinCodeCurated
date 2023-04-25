using Anjin.Scripting;
using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSTextureSwapper : MonoBehaviour
{
	[SerializeField] private ParticleSystemRenderer ps;
	[SerializeField] private Texture texture;

	public void Configure(Table config)
	{
		//if (config.TryGet("texture", out Texture texture))
		//{
		//	ps.material.SetTexture("_MainTex", texture);
		//}

		ps.material.SetTexture("_MainTex", texture);
	}
}
