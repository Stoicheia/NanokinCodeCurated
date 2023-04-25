using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnjinGraphicsRaycaster : GraphicRaycaster
{
	public bool  LenseDistortionCorrection;
	public float distortion_intensity  = 0f;
	public float distortion_intensityX = 1f;
	public float distortion_intensityY = 1f;
	public float distortion_centerX    = 0f;
	public float distortion_centerY    = 0f;
	public float distortion_scale      = 1;

	private Canvas canvas;

	protected override void Start()
	{
		base.Start();

		canvas = GetComponent<Canvas>();
	}

	Vector4 _Distortion_Amount;
	Vector4 _Distortion_CenterScale;

	public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
	{
		//All copied from the post processing stack's lens distortion effect, to correct input casting on distorted canvases
		if(LenseDistortionCorrection)
		{
			float amount = 1.6f          * Mathf.Max(Mathf.Abs(distortion_intensity), 1f);
			float theta  = Mathf.Deg2Rad * Mathf.Min(160f, amount);
			float sigma  = 2f            * Mathf.Tan(theta * 0.5f);

			_Distortion_CenterScale = new Vector4(distortion_centerX, distortion_centerY, Mathf.Max(distortion_intensityX, 1e-4f), Mathf.Max(distortion_intensityY, 1e-4f));
			_Distortion_Amount      = new Vector4(distortion_intensity >= 0f ? theta : 1f / theta, sigma, 1f / distortion_scale, distortion_intensity); //

			var dimensions = new Vector2(Screen.width, Screen.height);

			Vector2 uv          = eventData.position / dimensions;
			Vector2 distortedUV = Distort(uv) * dimensions;

			//Debug.Log(eventData.position + ", " + distortedUV);

			eventData.position = distortedUV;
		}

		base.Raycast(eventData, resultAppendList);
	}

	static Vector2 half = new Vector2(0.5f,0.5f);

	Vector2 Distort(Vector2 uv)
	{
		uv = (uv - half) * _Distortion_Amount.z + half;
		Vector2 ruv = _Distortion_CenterScale.zw() * (uv - half - _Distortion_CenterScale.xy());
		float   ru  = ruv.magnitude;

		if (_Distortion_Amount.w > 0.0f)
		{
			float wu = ru * _Distortion_Amount.x;
			ru = Mathf.Tan(wu) * (1.0f / (ru * _Distortion_Amount.y));
			uv = uv + ruv * (ru - 1.0f);
		}
		else
		{
			ru = (1.0f / ru) * _Distortion_Amount.x * Mathf.Atan(ru * _Distortion_Amount.y);
			uv = uv + ruv * (ru - 1.0f);
		}

		return uv;
	}
}