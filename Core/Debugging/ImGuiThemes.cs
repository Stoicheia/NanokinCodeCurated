using ImGuiNET;
using UnityEngine;
using UnityUtilities;

namespace Core.Debug
{
	public static class ImGuiThemes
	{
		public static void Cherry()
		{
			// cherry colors, 3 intensities
			Color high   = new Color(0.502f, 0.075f, 0.256f);
			Color medium = new Color(0.455f, 0.198f, 0.301f);
			Color low    = new Color(0.232f, 0.201f, 0.271f);
			Color bg     = new Color(0.200f, 0.220f, 0.270f);
			Color text   = new Color(0.860f, 0.930f, 0.890f);

			ImGuiStylePtr style = ImGui.GetStyle();
			style.Colors[(int) ImGuiCol.Text]                 = text.Alpha(0.78f);
			style.Colors[(int) ImGuiCol.TextDisabled]         = text.Alpha(0.28f);
			style.Colors[(int) ImGuiCol.WindowBg]             = new Vector4(0.13f, 0.14f, 0.17f, 0.965f);
			style.Colors[(int) ImGuiCol.ChildBg]              = bg.Alpha(0.58f);
			style.Colors[(int) ImGuiCol.PopupBg]              = bg.Alpha(0.9f);
			style.Colors[(int) ImGuiCol.Border]               = new Vector4(0.31f, 0.31f, 1.00f, 0.00f);
			style.Colors[(int) ImGuiCol.BorderShadow]         = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
			style.Colors[(int) ImGuiCol.FrameBg]              = bg.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.FrameBgHovered]       = medium.Alpha(0.78f);
			style.Colors[(int) ImGuiCol.FrameBgActive]        = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.TitleBg]              = low.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.TitleBgActive]        = high.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.TitleBgCollapsed]     = bg.Alpha(0.75f);
			style.Colors[(int) ImGuiCol.MenuBarBg]            = bg.Alpha(0.47f);
			style.Colors[(int) ImGuiCol.ScrollbarBg]          = bg.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.ScrollbarGrab]        = new Vector4(0.09f, 0.15f, 0.16f, 1.00f);
			style.Colors[(int) ImGuiCol.ScrollbarGrabHovered] = medium.Alpha(0.78f);
			style.Colors[(int) ImGuiCol.ScrollbarGrabActive]  = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.CheckMark]            = new Vector4(0.71f, 0.22f, 0.27f, 1.00f);
			style.Colors[(int) ImGuiCol.SliderGrab]           = new Vector4(0.47f, 0.77f, 0.83f, 0.14f);
			style.Colors[(int) ImGuiCol.SliderGrabActive]     = new Vector4(0.71f, 0.22f, 0.27f, 1.00f);
			style.Colors[(int) ImGuiCol.Button]               = new Vector4(0.47f, 0.77f, 0.83f, 0.14f);
			style.Colors[(int) ImGuiCol.ButtonHovered]        = medium.Alpha(0.86f);
			style.Colors[(int) ImGuiCol.ButtonActive]         = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.Header]               = medium.Alpha(0.76f);
			style.Colors[(int) ImGuiCol.HeaderHovered]        = medium.Alpha(0.86f);
			style.Colors[(int) ImGuiCol.HeaderActive]         = high.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.Separator]            = new Vector4(0.07f, 0.11f, 0.06f, 1.00f);
			style.Colors[(int) ImGuiCol.SeparatorHovered]     = medium.Alpha(0.78f);
			style.Colors[(int) ImGuiCol.SeparatorActive]      = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.ResizeGrip]           = new Vector4(0.47f, 0.77f, 0.83f, 0.04f);
			style.Colors[(int) ImGuiCol.ResizeGripHovered]    = medium.Alpha(0.78f);
			style.Colors[(int) ImGuiCol.ResizeGripActive]     = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.PlotLines]            = text.Alpha(0.63f);
			style.Colors[(int) ImGuiCol.PlotLinesHovered]     = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.PlotHistogram]        = text.Alpha(0.63f);
			style.Colors[(int) ImGuiCol.PlotHistogramHovered] = medium.Alpha(1.00f);
			style.Colors[(int) ImGuiCol.TextSelectedBg]       = medium.Alpha(0.43f);
			style.Colors[(int) ImGuiCol.ModalWindowDimBg]     = bg.Alpha(0.73f);

			style.WindowPadding     = new Vector2(6, 4);
			style.WindowRounding    = 0.0f;
			style.FramePadding      = new Vector2(5, 2);
			style.FrameRounding     = 3.0f;
			style.ItemSpacing       = new Vector2(7, 1);
			style.ItemInnerSpacing  = new Vector2(1, 1);
			style.TouchExtraPadding = new Vector2(0, 0);
			style.IndentSpacing     = 6.0f;
			style.ScrollbarSize     = 12.0f;
			style.ScrollbarRounding = 16.0f;
			style.GrabMinSize       = 20.0f;
			style.GrabRounding      = 2.0f;

			style.WindowTitleAlign.x = 0.50f;

			style.Colors[(int) ImGuiCol.Border] = new Vector4(0.539f, 0.479f, 0.255f, 0.162f);
			style.FrameBorderSize               = 0.0f;
			style.WindowBorderSize              = 1.0f;
		}
	}
}