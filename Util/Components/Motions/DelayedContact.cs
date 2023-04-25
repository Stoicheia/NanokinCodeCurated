using Anjin.Scripting;
using Combat.Data;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.Odin.Attributes;
using Util.UnityEditor.Odin;

namespace Anjin.Utils
{
	[Inline(true, true)]
	[DarkBox]
	[System.Serializable]
	[LuaUserdata]
	public struct DelayedContact
	{
		public float Delay;

		public string ProcID;

		[System.NonSerialized]
		public Proc proc;

		[System.NonSerialized]
		public Closure luaClosure;
	}
}

