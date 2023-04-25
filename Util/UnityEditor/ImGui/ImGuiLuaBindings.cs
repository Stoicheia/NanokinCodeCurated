using Anjin.Scripting;
using ImGuiNET;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin
{
	[LuaUserdata(StaticName = "ImGui")]
	public class ImGuiLuaBindings
	{
		public static bool begin(string name, DynValue flags)
		{
			ImGuiWindowFlags _flags = ImGuiWindowFlags.None;
			if (flags.Type == DataType.UserData)
				flags.UserData.TryGet(out _flags);
			else if (flags.Type == DataType.Table) {
				if (flags.Table.Length > 0) {
					for (int i = 0; i < flags.Table.Length; i++) {
						DynValue val = flags.Table.Get(i + 1);
						if (val.UserData.TryGet(out ImGuiWindowFlags flag))
							_flags |= flag;
					}
				}
			}


			return ImGui.Begin(name ?? "", _flags);
		}

		public static void _end() => ImGui.End();

		public static void set_next_window_size(Vector2 size) 					=> ImGui.SetNextWindowSize(size);
		public static void set_next_window_size(Vector2 size, ImGuiCond cond) 	=> ImGui.SetNextWindowSize(size, cond);

		public static void set_next_window_pos(Vector2 pos) 								=> ImGui.SetNextWindowPos(pos);
		public static void set_next_window_pos(Vector2 pos, ImGuiCond cond) 				=> ImGui.SetNextWindowPos(pos, cond);
		public static void set_next_window_pos(Vector2 pos, ImGuiCond cond, Vector2 pivot) 	=> ImGui.SetNextWindowPos(pos, cond, pivot);

		public static void text(string fmt) 						=> ImGui.Text(fmt ?? "");
		public static void text_colored(Vector4 color, string fmt) 	=> ImGui.TextColored(color, fmt ?? "");

		public static bool button(string label, Vector2 size = new Vector2()) => ImGui.Button(label ?? "", size);

		public static void same_line(float offset_from_start_x = 0, float spacing = -1) => ImGui.SameLine(offset_from_start_x, spacing);

		public static void push_id(int id) 		=> ImGui.PushID(id);
		public static void push_id(string id) 	{ if (id != null) ImGui.PushID(id); }
		public static void pop_id() 			=> ImGui.PopID();
	}
}