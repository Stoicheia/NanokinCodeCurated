namespace Anjin.Nanokin.ParkAI {
	public struct GraphPath {
		public Node[] array;
		public int    index;
		public bool   reverse;
		public int    length      => array.Length;
		public bool   valid       => array != null && index_valid;
		public bool   index_valid => index >= 0    && index < array.Length;
		public Node   this[int i] => array[i];
		public int direction => !reverse ? 1 : -1;

		public bool IsLooping()
		{
			if (!valid || length == 0) return false;

			Node first = array[0];
			Node last  = array[array.Length - 1];

			if (first == last) return true;

			if (first.type == NodeType.Portal && last.type == NodeType.Portal) {
				if (last.portal_destination.node.id == first.id)
					return true;
			}

			return false;
		}
	}
}