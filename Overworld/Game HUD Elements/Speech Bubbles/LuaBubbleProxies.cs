using Anjin.Scripting;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.UI
{
	public class HUDBubbleProxy<T> : MonoLuaProxy<T> where T:HUDBubble
	{
		public HUDElement 		hud_element 	{ get => proxy.hudElement; 		}
		public Transform 		bubble_display 	{ get => proxy.BubbleDisplay; 	}
		public RectTransform 	bubble_box 		{ get => proxy.BubbleBox; 		}

		public float? width            { get => proxy.Width;          set => proxy.Width   	= value; }
		public float? height           { get => proxy.Height;         set => proxy.Height   	= value; }
		public float  normal_max_width { get => proxy.NormalMaxWidth; set => proxy.NormalMaxWidth   = value; }


		public void apply_settings(Table settings) => proxy.ApplySettings(settings);
		public void reset_settings() => proxy.ResetSettings();


	}


}