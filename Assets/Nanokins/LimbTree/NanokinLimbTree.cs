using System.Collections.Generic;
using API.Spritesheet.Indexing;
using Data.Nanokin;
using JetBrains.Annotations;
using Puppets;
using Puppets.Assets;
using Util.Addressable;

namespace Assets.Nanokins
{
	/// <summary>
	/// A tree accessor which facilitates nanokin specific tools.
	/// This reduces code flexibility, so use with caution.
	/// Generally it should only be used to quickly and easily create a nanokin style puppet. (from nanokin monsters or limbs)
	/// </summary>
	public class NanokinLimbTree : LimbTree
	{
		private bool _editor;

		private Dictionary<LimbType, Branch> _typeMap;

		public const string PLUG_BODY      = "feet";
		public const string SOCK_BODY_HEAD = "head";
		public const string SOCK_BODY_ARM1 = "arm1";
		public const string SOCK_BODY_ARM2 = "arm2";
		public const string PLUG_HEAD      = SOCK_BODY_HEAD;
		public const string PLUG_ARM1      = SOCK_BODY_ARM1;
		public const string PLUG_ARM2      = SOCK_BODY_ARM2;

		public NanokinLimbTree()
		{
			BodyBranch = new Branch(PLUG_BODY);
			HeadBranch = new Branch(PLUG_HEAD);
			Arm1Branch = new Branch(PLUG_ARM1);
			Arm2Branch = new Branch(PLUG_ARM2);

			BodyBranch.Plug(HeadBranch, SOCK_BODY_HEAD);
			BodyBranch.Plug(Arm1Branch, SOCK_BODY_ARM1);
			BodyBranch.Plug(Arm2Branch, SOCK_BODY_ARM2);

			_typeMap = new Dictionary<LimbType, Branch>
			{
				[LimbType.Body] = BodyBranch,
				[LimbType.Head] = HeadBranch,
				[LimbType.Arm1] = Arm1Branch,
				[LimbType.Arm2] = Arm2Branch,
			};

			Root = BodyBranch;
		}

		public static NanokinLimbTree WithAddressable(
			ScriptableLimb body,
			ScriptableLimb head,
			ScriptableLimb arm1,
			ScriptableLimb arm2,
			AsyncHandles   handles)
		{
			var tree = new NanokinLimbTree();
			tree.SetBody(body, handles);
			tree.SetHead(head, handles);
			tree.SetArm1(arm1, handles);
			tree.SetArm2(arm2, handles);
			return tree;
		}

		public static NanokinLimbTree WithAddressable(NanokinInstance instance, AsyncHandles handles)
		{
			NanokinLimbTree tree = new NanokinLimbTree();

			tree.SetBody(instance.Body.Asset, handles);
			tree.SetHead(instance.Head.Asset, handles);
			tree.SetArm1(instance.Arm1.Asset, handles);
			tree.SetArm2(instance.Arm2.Asset, handles);

			return tree;
		}

		public static NanokinLimbTree WithEditor(NanokinLimbAsset body, NanokinLimbAsset head, NanokinLimbAsset arm1, NanokinLimbAsset arm2)
		{
			NanokinLimbTree tree = new NanokinLimbTree();
			tree._editor = true;
			tree.SetBodyEditor(body);
			tree.SetHeadEditor(head);
			tree.SetArm1Editor(arm1);
			tree.SetArm2Editor(arm2);
			return tree;
		}

		public Branch BodyBranch { get; }
		public Branch HeadBranch { get; }
		public Branch Arm1Branch { get; }
		public Branch Arm2Branch { get; }

		public void Set([NotNull] NanokinInstance nano, AsyncHandles handles)
		{
			SetBody(nano[LimbType.Body].Asset, handles);
			SetHead(nano[LimbType.Head].Asset, handles);
			SetArm1(nano[LimbType.Arm1].Asset, handles);
			SetArm2(nano[LimbType.Arm2].Asset, handles);
		}

		public void SetBody(ScriptableLimb value, AsyncHandles handles) => BodyBranch.State = new LimbState(value, handles);
		public void SetHead(ScriptableLimb value, AsyncHandles handles) => HeadBranch.State = new LimbState(value, handles);
		public void SetArm1(ScriptableLimb value, AsyncHandles handles) => Arm1Branch.State = new LimbState(value, handles);
		public void SetArm2(ScriptableLimb value, AsyncHandles handles) => Arm2Branch.State = new LimbState(value, handles);

	#region UNITY_EDITOR

		public void SetBodyEditor(ScriptableLimb value) => BodyBranch.State = new LimbState(value, value.Spritesheet.Asset as IndexedSpritesheetAsset);
		public void SetHeadEditor(ScriptableLimb value) => HeadBranch.State = new LimbState(value, value.Spritesheet.Asset as IndexedSpritesheetAsset);
		public void SetArm1Editor(ScriptableLimb value) => Arm1Branch.State = new LimbState(value, value.Spritesheet.Asset as IndexedSpritesheetAsset);
		public void SetArm2Editor(ScriptableLimb value) => Arm2Branch.State = new LimbState(value, value.Spritesheet.Asset as IndexedSpritesheetAsset);

	#endregion

		// public void SetBody(ScriptableLimb value) => BodyBranch.State = new LimbState(value, _handles);
		// public void SetHead(ScriptableLimb value) => HeadBranch.State = new LimbState(value, _handles);
		// public void SetArm1(ScriptableLimb value) => Arm1Branch.State = new LimbState(value, _handles);
		// public void SetArm2(ScriptableLimb value) => Arm2Branch.State = new LimbState(value, _handles);


		public Branch this[LimbType types] => _typeMap[types];
	}
}