using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.Scripting
{
	[LuaUserdata]
	public class LuaCollisionDetector : MonoBehaviour
	{
		public Closure on_collision_enter;
		public Closure on_collision_exit;

		public Closure on_trigger_enter;
		public Closure on_trigger_exit;

		void OnCollisionEnter(Collision other) 	=> on_collision_enter?.Call(other);
		void OnCollisionExit(Collision other) 	=> on_collision_exit?.Call(other);
		void OnTriggerEnter(Collider other) => on_trigger_enter?.Call(other);
		void OnTriggerExit(Collider other) => on_trigger_exit?.Call(other);

	}
}