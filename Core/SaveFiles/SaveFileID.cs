using Drawing;
using ImGuiNET;
using Sirenix.Utilities;

namespace SaveFiles {
	public struct SaveFileID {
		public bool   isNamed;
		public int    index;
		public string name;

		public bool IsValid()
		{
			if (!isNamed && index < 0) return false;
			if (isNamed && name.IsNullOrWhitespace()) return false;

			return true;
		}

		public static SaveFileID DefaultIndexed = new SaveFileID {
			index = -1,
		};

		public static implicit operator SaveFileID(int index) => new SaveFileID {
			isNamed = false,
			index   = index,
		};

		public static implicit operator SaveFileID(string name) => new SaveFileID {
			isNamed = true,
			name    = name,
		};

		public override string ToString() => isNamed ? (name ?? "(null name)") : index.ToString();

		public static void OnImgui(ref SaveFileID id)
		{
			ImGui.Checkbox("Is Named", ref id.isNamed);

			ImGui.SameLine();
			ImGui.SetNextItemWidth(86);

			if (id.isNamed) {
				if (id.name == null)
					id.name = "";

				ImGui.InputText("", ref id.name, 64);
			} else
				ImGui.InputInt("", ref id.index);
		}
	}
}