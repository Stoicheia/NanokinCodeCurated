using UnityEngine;

namespace Util.Extensions
{
	public partial class Extensions
	{
		public static bool IsUsed(this Event ev)
		{
			return ev.type == EventType.Used;
		}

		public static bool IsRawMouse(this Event ev)
		{
			EventType type = ev.rawType;

			int num;
			switch (type)
			{
				case EventType.MouseDown:
				case EventType.MouseUp:
				case EventType.MouseMove:
				case EventType.MouseDrag:
				case EventType.ContextClick:
				case EventType.MouseEnterWindow:
					num = 1;
					break;
				default:
					num = type == EventType.MouseLeaveWindow ? 1 : 0;
					break;
			}

			return num != 0;
		}

		public static bool IsRawKey(this Event ev)
		{
			EventType type = ev.rawType;
			return type == EventType.KeyDown || type == EventType.KeyUp;
		}

		public static bool IsRawScrollWheel(this Event ev)
		{
			EventType type = ev.rawType;
			return type == EventType.ScrollWheel || type == EventType.ScrollWheel;
		}
	}
}