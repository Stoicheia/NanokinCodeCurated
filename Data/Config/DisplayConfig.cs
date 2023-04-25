using Sirenix.OdinInspector;
using UnityEngine;

public class DisplayConfig : SerializedScriptableObject
{
	public int      targetFramerate = 60;
	public Shader   spriteCanvasDefaultShader;
	public Material basicSpriteExtensionDefaultMaterial;
}