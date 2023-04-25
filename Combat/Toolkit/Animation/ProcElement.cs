using API.PropertySheet.Elements;

namespace Combat.Data
{
	/// <summary>
	/// A proc element for use in puppet animation.
	/// The proc element can then be mapped to procs
	/// inside of skill scripts and such.
	/// </summary>
	public class ProcElement : Element
	{
		public override string TypeName { get; } = "Proc";

		public string ProcID { get; set; }

		public ProcElement(int id) : base(id) { }
	}
}