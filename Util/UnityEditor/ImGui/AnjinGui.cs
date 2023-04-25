using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Anjin.Util;
using Sirenix.Utilities;
using UnityEngine;
using Vexe.Runtime.Extensions;
using g = ImGuiNET.ImGui;
using TypeExtensions = Sirenix.Utilities.TypeExtensions;

namespace ImGuiNET
{
	/// <summary>
	/// Custom drawing helpers/Reflection-based drawers for dear Imgui.
	/// </summary>
	public static class AImgui {
		public static TestStruct test;

		public static void DrawDemo()
		{
			DrawStruct(ref test);
		}

		// Type Info
		//----------------------------------------------

		public class CachedTypeInfo {
			public bool IsEnum;
			public bool IsReferenceType;
			public bool IsPrimitive;
			public bool IsStruct;
			public bool IsConst;

			public Type                            Type;
			public string                          Name;
			public FieldInfo[]                     Fields;
			public MethodInfo[]                    Methods;
			public PropertyInfo[]                  Properties;
			public (bool didError, string message) Error;

			public Array					EnumVals;
			public string[]					EnumNames;
			public Dictionary<int, string>	EnumValsToNames;
		}

		public static Dictionary<Type, CachedTypeInfo> TypeInfoCache = new Dictionary<Type, CachedTypeInfo>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void Init()
		{
			TypeInfoCache?.Clear();
		}

		static CachedTypeInfo ensureTypeInfoCached(Type type, int depth)
		{
			if (TypeInfoCache.TryGetValue(type, out var existingInfo))
				return existingInfo;

			CachedTypeInfo info = new CachedTypeInfo();
			if (depth > 100) return info;

			try {
				info.Type        = type;
				info.Name        = type.FullName;
				info.IsPrimitive = IsPrimitive(type);
				info.IsEnum      = type.IsEnum;
				info.IsStruct    = type.IsStruct() && type != typeof(string);

				info.IsReferenceType = !info.IsPrimitive && !info.IsEnum && !info.IsStruct;


				if (info.IsEnum) {
					info.EnumVals        = Enum.GetValues(type);
					info.EnumNames       = Enum.GetNames(type);
					info.EnumValsToNames = new Dictionary<int, string>();

					var i = 0;
					foreach (object obj in info.EnumVals) {
						info.EnumValsToNames[Convert.ToInt32(obj)] = info.EnumNames[i];
						i++;
					}

				} else {
					info.Fields     = type.GetFields(		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
					info.Methods    = type.GetMethods(		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
					info.Properties = type.GetProperties(	BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				}

			} catch (Exception e) {
				info.Error = (true, e.ToString());
			}

			TypeInfoCache[type] = info;

			return info;
		}

		static bool IsPrimitive(Type type) => !type.IsEnum &&
											  (type.IsPrimitive || type == typeof(string));

		public static bool Debug => false;

		public static bool DrawObj(object obj, int depth = 0, CachedTypeInfo typeInfo = null)
		{
			if (depth > 50) return false;

			CachedTypeInfo baseInfo = typeInfo ?? ensureTypeInfoCached(obj.GetType(), 0);

			if (baseInfo == null) return false;

			for (int i = 0; i < baseInfo.Fields.Length; i++) {
				FieldInfo      field = baseInfo.Fields[i];
				CachedTypeInfo info  = ensureTypeInfoCached(field.FieldType, 0);

				ImGui.PushID(i);

				if (obj == null) {

					if(Debug) {
						ImGui.Text($"Field: {field.Name} ({info.Name}), null");
					} else {
						ImGui.Text($"{field.Name}: null");
						//g.SameLine();
					}

				} else {

					object value = field.GetValue(obj);

					if (field.IsLiteral) {
						DoName();
						ImGui.Text($"Literal: {info.Type.Name}: {value ?? "null"}");
					} else if (info.IsPrimitive) {

						DoName();
						g.SameLine();
						DrawPrimitive(ref value, info);
						field.SetValue(obj, value);

					} else if (info.IsEnum) {
						DoName();
						DrawEnum(obj, value, field, info);

					} else if (info.IsStruct) {

						DoName();
						DrawStruct(ref value, depth + 1, info);

					} else {

						if (value is IDictionary dict) {
							//if(!Debug) g.SameLine();

							if (g.CollapsingHeader($"{field.Name} (Dictionary, {dict.Count} items)")) {
								ImGui.Indent(12);
								DrawDictionary(dict, depth + 1);
								ImGui.Unindent(12);
							}

						} else if(value is IList list) {
							if (g.CollapsingHeader($"{field.Name} (List, {list.Count} items)")) {
								ImGui.Indent(12);
								DrawList(list, depth + 1);
								ImGui.Unindent(12);
							}
						} else {
							//if(!Debug) g.SameLine();
							if (g.CollapsingHeader($"{field.Name} {info.Name}")) {
								ImGui.Indent(12);
								DrawObj(value, depth + 1, info);
								ImGui.Unindent(12);
							}
						}


					}

					if (Debug) {
						ImGui.Unindent(12);
					}
				}

				ImGui.PopID();

				void DoName()
				{
					if (Debug) {
						ImGui.Text($"Field: {field.Name} ({info.Name})");
						ImGui.Indent(12);
						g.SameLine();
					} else {
						ImGui.Text($"{field.Name}");
					}
				}
			}

			return true;

		}


		// Drawers
		//----------------------------------------------

		public static void DrawPrimitive(ref object obj, CachedTypeInfo info) /* where T: struct */
		{
			if (!info.IsPrimitive) {
				return;
			}

			if (info.Type == typeof(string)) {
				string str = obj as string;

				if (str == null) {
					if (ImGui.Button("Initialize")) {
						str = "";
					}
				} else {
					ImGui.InputText ("##label", ref str, 500);
				}

				obj = str;
				//_change(ref obj, str);
			} else {
				switch (obj) {
					case int		val: DrawInt		(ref val); _change(ref obj, val); break;
					case float  	val: DrawFloat	(ref val); _change(ref obj, val); break;
					case double  	val: DrawDouble	(ref val); _change(ref obj, val); break;
					case bool 		val: DrawBool		(ref val); _change(ref obj, val); break;
				}
			}

			//void _change(ref object _obj, object newVal) => _obj = (T)Convert.ChangeType(newVal, typeof(T));
			void _change(ref object _obj, object newVal) => _obj = newVal;
		}


		public static void DrawEnum(object target, object value, FieldInfo field, CachedTypeInfo info)
		{
			var selected = Convert.ToInt32(value);

			bool value_is_part_of_enum = info.EnumValsToNames.TryGetValue(selected, out string name);

			string preview = value_is_part_of_enum ? name : $"({selected})";

			if (ImGui.BeginCombo("##label", preview))
			{
				for (int j = 0; j < info.EnumNames.Length; j++) {
					bool is_selected = selected == j;
					if (ImGui.Selectable(info.EnumNames[j] + $" ({((int)info.EnumVals.GetValue(j))})", is_selected))
						value = info.EnumVals.GetValue(j);

					if(is_selected) ImGui.SetItemDefaultFocus();
				}
				ImGui.EndCombo();
			}

			field.SetValue(target, value);
		}

		public static void DrawEnum(ref object value, CachedTypeInfo info)
		{
			var selected = Convert.ToInt32(value);

			bool value_is_part_of_enum = info.EnumValsToNames.TryGetValue(selected, out string name);

			string preview = value_is_part_of_enum ? name : $"({selected})";

			if (ImGui.BeginCombo("##label", preview))
			{
				for (int j = 0; j < info.EnumNames.Length; j++) {
					bool is_selected = selected == j;
					if (ImGui.Selectable(info.EnumNames[j] + $" ({((int)info.EnumVals.GetValue(j))})", is_selected))
						value = info.EnumVals.GetValue(j);

					if(is_selected) ImGui.SetItemDefaultFocus();
				}
				ImGui.EndCombo();
			}
		}

		public static bool DrawList(IList list, int depth = 0)
		{
			if (depth > 50) return false;

			if (list == null) return false;

			if(g.Button("Clear"))
				list.Clear();

			for (int i = 0; i < list.Count; i++) {

				object val = list[i];

				g.PushID(i);

				CachedTypeInfo info = ensureTypeInfoCached(val.GetType(), 0);
				//g.Text($"{i}: ");

				if (info.IsPrimitive) {
					g.SameLine();
					DrawPrimitive(ref val, info);
				} else if (info.IsEnum) {
					g.SameLine();
					//if(!Debug) g.SameLine();
					DrawEnum(ref val, info);
				} else {
					if(g.CollapsingHeader($"{i}: ")) {
						if (info.IsStruct) {
							//if(!Debug) g.SameLine();
							//DrawStruct(ref value, depth + 1, info);

						} else {

							ImGui.Indent(12);
							DrawObj(val, depth + 1);
							ImGui.Unindent(12);
						}
					}
				}

				list[i] = val;

				g.PopID();
			}

			return true;
		}

		public static bool DrawDictionary(IDictionary dictionary, int depth = 0)
		{
			if (depth > 50) return false;

			if (dictionary == null) return false;

			//g.Text("DICTIONARY");

			ICollection keys = dictionary.Keys;

			object[] array = new object[keys.Count];
			keys.CopyTo(array, 0);

			for (int i = 0; i < array.Length; i++) {
				var key = array[i];

				if (key == null) continue;

				g.PushID(i);

				object storedObj = dictionary[key];

				CachedTypeInfo _info = ensureTypeInfoCached(storedObj.GetType(), 0);

				g.Text(key + $" ({ TypeExtensions.GetNiceName(storedObj.GetType())}): ");

				g.SameLine();

				if (_info.IsPrimitive) {
					DrawPrimitive(ref storedObj, _info);
				} else {
					DrawObj(storedObj, depth + 1);
				}

				g.PopID();

				dictionary[key] = storedObj;
			}

			return true;
		}

		/*public static bool DrawObj(ref object obj, int depth = 0, CachedTypeInfo typeInfo = null)
		{
			CachedTypeInfo baseInfo = typeInfo ?? ensureTypeInfoCached(obj.GetType(), 0);

			if (baseInfo == null) return false;

			for (int i = 0; i < baseInfo.Fields.Length; i++) {
				FieldInfo      field    = baseInfo.Fields[i];
				CachedTypeInfo info = ensureTypeInfoCached(field.FieldType, 0);

				if (obj == null) {

					ImGui.Text($"Field: {info.Name}, null");
				} else {

					ImGui.Text($"Field: {info.Name}");
					object value = field.GetValue(obj);

					if (info.IsPrimitive) {
						switch (value) {
							case int 		val:
								IntDrawer		(ref val);
								break;
							case float  	val:
								FloatDrawer	(ref val);
								break;
							case double  	val:
								DoubleDrawer	(ref val);
								break;
							case string  	val:
								StringDrawer	(ref val);
								break;
							case bool 		val:
								BoolDrawer		(ref val);
								break;
						}

						field.SetValue(obj, value);

					} else if (info.IsEnum) {
						var selected = Convert.ToInt32(value);

						/*if (underlying != typeof(Int32)) { ImGui.TextColored(Color.red.ToV4(), "No drawer for underlying enum type  " + underlying.Name); } #1#
						if (ImGui.BeginCombo("##label", info.EnumNames[selected])) {
							for (int j = 0; j < info.EnumNames.Length; j++) {
								bool is_selected = selected == j;
								if (ImGui.Selectable(info.EnumNames[j] + $" ({((int) info.EnumVals.GetValue(j))})", is_selected))
									value = info.EnumVals.GetValue(j);

								if (is_selected) ImGui.SetItemDefaultFocus();
							}

							ImGui.EndCombo();
						}

						field.SetValue(obj, value);
					} else if (info.IsStruct) {
						DrawStruct(ref value, depth + 1, info);
					} else {
						DrawObj(ref value, depth + 1, info);
					}
				}
			}


			return true;
		}*/

		public static bool DrawStruct<S>(ref S val, int depth = 0, CachedTypeInfo typeInfo = null)
		{
			CachedTypeInfo info = typeInfo ?? ensureTypeInfoCached(typeof(S), 0);
			if (info == null) return false;

			ImGui.Text($"TODO: Struct {info.Name}");

			return true;
		}

		public static void DrawInt		(ref int    obj) 	=> ImGui.InputInt		("##label", ref obj);
		public static void DrawFloat	(ref float  obj) 	=> ImGui.InputFloat		("##label", ref obj);
		public static void DrawDouble	(ref double  obj) 	=> ImGui.InputDouble	("##label", ref obj);
		public static void DrawString	(ref string obj) 	=> ImGui.InputText		("##label", ref obj, (uint)obj.Length);
		public static void DrawBool		(ref bool   obj) 	=> ImGui.Checkbox		("##label", ref obj);

		/*public static bool DrawPrimitive(ref object obj)
		{
			switch (obj) {
				case int 		val: IntDrawer		(ref val); 	break;
				case float  	val: FloatDrawer	(ref val); 	break;
				case string  	val: StringDrawer	(ref val); 	break;
				case bool 		val: BoolDrawer		(ref val); 	break;
				default: 										return false;
			}

			return true;
		}*/

		public static void EnumDrawer<E>(string label, ref E val/*, CachedTypeInfo info*/) where E:Enum
		{
			//if (!info.IsEnum) return;

			//var selected = Convert.ToInt32(val);

			/*if (underlying != typeof(Int32)) {
				ImGui.TextColored(Color.red.ToV4(), "No drawer for underlying enum type  " + underlying.Name);
			}
			*/

			/*if (ImGui.BeginCombo("##label", info.EnumNames[selected])) {

				for (int i = 0; i < info.EnumNames.Length; i++) {
					bool is_selected = selected == i;
					if (ImGui.Selectable(info.EnumNames[i] + $" ({((int)info.EnumVals.GetValue(i))})", is_selected))
						val = (E)info.EnumVals.GetValue(i);

					if(is_selected) ImGui.SetItemDefaultFocus();
				}

				ImGui.EndCombo();
			}*/

			var type       = val.GetType();
			var vals       = Enum.GetValues(type);
			var names      = Enum.GetNames(type);
			var selected   = Convert.ToInt32(val);
			var underlying = type.GetEnumUnderlyingType();


			if (ImGui.BeginCombo(label, names[selected])) {
				for (int i = 0; i < names.Length; i++) {
					bool is_selected = selected == i;
					if (ImGui.Selectable(names[i] + $" ({ Convert.ToInt32(vals.GetValue(i)) })", is_selected))
						val = (E)vals.GetValue(i);

					if(is_selected) ImGui.SetItemDefaultFocus();
				}

				/*const bool is_selected = (item_current_idx == n);
                if (ImGui::Selectable(items[n], is_selected))
                    item_current_idx = n;

                // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                if (is_selected)
                    ImGui::SetItemDefaultFocus();*/
				ImGui.EndCombo();
			}

		}

		public static void AddSprite(ref ImDrawListPtr draw_list, Sprite sprite, Vector2 center, Vector2 size, float rot = 0)
		{
			var tex = (IntPtr) ImGuiUn.GetTextureId(sprite.texture);

			Vector2 uv1 = new Vector2(0.0f, 0.0f);
			Vector2 uv2 = new Vector2(1.0f, 0.0f);
			Vector2 uv3 = new Vector2(1.0f, 1.0f);
			Vector2 uv4 = new Vector2(0.0f, 1.0f);

			Vector2 p1 = center + MathUtilities.RotatePoint(new Vector2(-size.x * 0.5f, -size.y * 0.5f), rot);
			Vector2 p2 = center + MathUtilities.RotatePoint(new Vector2(+size.x * 0.5f, -size.y * 0.5f), rot);
			Vector2 p3 = center + MathUtilities.RotatePoint(new Vector2(+size.x * 0.5f, +size.y * 0.5f), rot);
			Vector2 p4 = center + MathUtilities.RotatePoint(new Vector2(-size.x * 0.5f, +size.y * 0.5f), rot);

			/*Vector2 c1 = v1;
			Vector2 c2 = new Vector2(v2.x, v1.y);
			Vector2 c3 = v2;
			Vector2 c4 = new Vector2(v1.x, v2.y);

			Vector2 p1 = c1, p2 = c2, p3 = c3, p4 = c4;

			switch (rot) {
				case 1:
					p1 = c2;
					p2 = c4;
					p3 = c1;
					p4 = c3;
					break;

				case 2:
					/*uv1 = Vector2.right;
					uv2 = Vector2.up;#1#
					break;

				case 3:

					break;
			}*/

			draw_list.AddImageQuad(tex, p1, p2, p3, p4, uv1, uv2, uv3, uv4);

		}

		/*public static void EditObjs(params object[] objs)
		{
			for (int i = 0; i < objs.Length; i++) {
				EditObj(objs);
			}
		}

		public static void EditObj<T>(T obj)
		{
			Profiler.BeginSample("AnjinGui.EditObj "+nameof(T));
			{
				var fullName = obj.GetType().FullName + ":";
				var members  = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				var methods  = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				ImGui.Text(fullName);
				ImGui.Columns(2);
				for (int i = 0; i < members.Length; i++) {
					var mem = members[i];
					var val = mem.GetValue(obj);

					EditVariable(mem.Name, ref val, i);
					mem.SetValue(obj, val);
				}
				ImGui.Columns(1);
			}
			Profiler.EndSample();
		}

		public static void EditVariable<T>(string label, ref T val, int id = 0, bool next_column = true)
		{
			ImGui.AlignTextToFramePadding();

			//ImGui.PushStyleColor(ImGuiCol.Text, Color.yellow.ToV4());
			ImGui.Text(label);
			//ImGui.PopStyleColor();

			if(next_column)
				ImGui.SameLine();

			ImGui.SetNextItemWidth(ImGui.GetWindowContentRegionWidth());
			ImGui.SetCursorPosX(0);
			ImGui.Selectable("##hide", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);

			if(next_column)
				ImGui.NextColumn();

			ImGui.PushID(id);
			ImGui.SetNextItemWidth(-1);
			switch (val) {
				case int  		 v: DrawInt("##label", ref v); 			break;
				case float  	 v: DrawFloat("##label", ref v); 		break;
				case string  	 v: DrawString("##label", ref v); break;
				case bool 		 v: DrawBool("##label", ref v); 		break;
				case Enum 		 v: DrawEnum("##label", ref v); 		break;
				case IList       v: DrawList("##label", v); 			break;
				default:            ImGui.TextColored(Color.red.ToV4(), "No drawer for type " +typeof(T).Name); break;
			}
			if(next_column)
				ImGui.NextColumn();

			ImGui.PopID();
		}*/

		/*public static void EditStruct<S>(ref S obj) where S : struct
		{
			Profiler.BeginSample("AnjinGui.EditStruct " +nameof(S));
			{
				var fullName = typeof(S).FullName + ":";
				var members  = typeof(S).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				var methods  = typeof(S).GetMethods(BindingFlags.Public | BindingFlags.NonPublic                        | BindingFlags.Instance);

				ImGui.Text(fullName);
				ImGui.Columns(2);
				for (int i = 0; i < members.Length; i++) {
					var mem = members[i];
					var val = mem.GetValue(obj);

					EditVariable(mem.Name, ref val, i);
					mem.SetValueDirect(__makeref(obj), val);
				}
				ImGui.Columns(1);
			}
			Profiler.EndSample();
		}*/

		/*public static void DrawInt(string    label, ref int    val) => ImGui.InputInt(label, ref val);
		public static void DrawFloat(string  label, ref float  val) => ImGui.InputFloat(label, ref val);
		public static void DrawString(string label, ref string val) => ImGui.InputText(label, ref val, (uint)val.Length);
		public static void DrawBool(string   label, ref bool   val) => ImGui.Checkbox(label, ref val);

		public static void DrawEnum<E>(string label, ref E val) where E:Enum
		{
			var type       = val.GetType();
			var vals       = Enum.GetValues(type);
			var names      = Enum.GetNames(type);
			var selected   = Convert.ToInt32(val);
			var underlying = type.GetEnumUnderlyingType();

			if (underlying != typeof(Int32)) {
				ImGui.TextColored(Color.red.ToV4(), "No drawer for underlying enum type  " + underlying.Name);
			}

			if (ImGui.BeginCombo(label, names[selected])) {
				for (int i = 0; i < names.Length; i++) {
					bool is_selected = selected == i;
					if (ImGui.Selectable(names[i] + $" ({((int)vals.GetValue(i))})", is_selected))
						val = (E)vals.GetValue(i);

					if(is_selected) ImGui.SetItemDefaultFocus();
				}

				const bool is_selected = (item_current_idx == n);
                if (ImGui::Selectable(items[n], is_selected))
                    item_current_idx = n;

                // Set the initial focus when opening the combo (scrolling + keyboard navigation focus)
                if (is_selected)
                    ImGui::SetItemDefaultFocus();#1#
				ImGui.EndCombo();
			}

		}

		public static void DrawList<L>(string label, L val) where L:IList
		{
			var len = val.Count;
			for (int i = 0; i < len; i++) {
				var v = val[i];
				ImGui.Text(i.ToString());
				ImGui.SameLine();
				EditVariable("", ref v, next_column:false);
				val[i] = v;
			}

		}*/

		public static void DrawDebugScreen()
		{
			if (ImGui.Begin("AnjinGui")) {

			}ImGui.End();
		}

		public static void Text(string fmt, Color? col = null)
		{
			if(col != null)
				ImGui.TextColored(col.Value.ToV4(), fmt);
			else
				ImGui.Text(fmt);
		}

		public static void TextExt(string fmt, Color? col = null, float size = 1)
		{
			ImDrawListPtr draw  = ImGui.GetWindowDrawList();
			ImGuiStylePtr style = ImGui.GetStyle();

			var cpos = ImGui.GetCursorPos();

			draw.AddText(ImGui.GetFont(), ImGui.GetFontSize() * size, cpos, (col ?? Color.white).ToUint(), fmt);
			var sz = ImGui.CalcTextSize(fmt) * size;
			ImGui.Dummy(sz);
		}

		public static void HSpacer(float w)
		{
			ImGui.SameLine();
			ImGui.Dummy(new Vector2(w, 0));
			ImGui.SameLine();
		}

		public static void VSpacer(float h)
		{
			ImGui.SameLine();
			ImGui.Dummy(new Vector2(0, h));
			ImGui.SameLine();
		}

		public static void HSpace(float w) => ImGui.Dummy(new Vector2(w, 0));
		public static void VSpace(float h) => ImGui.Dummy(new Vector2(0, h));

		public static bool ToggleButton(string text, bool enabled, Color activeColor, Color? activeTextColor = null)
		{
			bool pressed = false;

			if(enabled) {
				ImGui.PushStyleColor(ImGuiCol.Button, activeColor.ToUint());

				if(activeTextColor.HasValue)
					ImGui.PushStyleColor(ImGuiCol.Text, activeTextColor.Value.ToUint());
			}

			if (ImGui.Button(text))
				pressed = true;

			if(enabled) {
				ImGui.PopStyleColor();

				if(activeTextColor.HasValue)
					ImGui.PopStyleColor();
			}

			return pressed;
		}

		// TESTS
		//-----------------------------------------------------------
		public struct TestStruct
		{
			public enum StandardEnum { a = 0, b = 1, c = 2 }

			public int    test_int;
			public float  test_float;
			public bool   test_bool;
			public string test_string;

			public StandardEnum test_enum;
		}
	}
}