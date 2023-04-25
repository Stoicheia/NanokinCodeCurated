using Anjin.Scripting;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.SceneLoading {

	public struct SceneLoadHandle
	{
		public int ID;

		[Inline, ShowInInspector, HideInEditorMode]
		public InstructionStatus Status {
			get {
				if (GameSceneLoader.Exists && GameSceneLoader.statuses.TryGetValue(ID, out var status))
					return status;
				else return InstructionStatus.NullStatus;
			}
		}

		[ShowInInspector, HideInEditorMode]
		public SceneGroup LoadedGroup {
			get {
				if (!GameSceneLoader.Exists) return null;
				GameSceneLoader.ids_to_groups.TryGetValue(ID, out var group);
				return group;
			}
		}

		[Button, HideInEditorMode]
		public void Unload() {
			if(Status.state == InstructionStatus.State.Done)
				GameSceneLoader.UnloadGroups(LoadedGroup);
		}

		public bool IsValid => Status.state != InstructionStatus.State.Invalid;
		public bool IsDone => Status.state == InstructionStatus.State.Done;
	}

	[LuaUserdata]
	public struct SceneUnloadHandle
	{
		public int ID;

		[Inline, ShowInInspector, HideInEditorMode]
		public InstructionStatus Status {
			get {
				if (GameSceneLoader.Exists && GameSceneLoader.statuses.TryGetValue(ID, out var status))
					return status;
				else return InstructionStatus.NullStatus;
			}
		}

		public bool IsValid => Status.state != InstructionStatus.State.Invalid;
		public bool IsDone  => Status.state == InstructionStatus.State.Done;
	}

}