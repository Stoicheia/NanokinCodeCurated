using System.Collections.Generic;
using UnityEngine;

namespace Anjin.Nanokin.Map {

	// A piece of data that can be attached to a specific point on an AnjinPath.
	// Note(C.L. 6-14-22): This may be overkill for now but I'd like to think we'll think of more things to add to paths than just instructions for moving platforms
	public class AnjinPathMetadataPoint {

		// What segment or point of the path this instruction lies on
		public int   Segment    = 0;

		// Is the segment a single point, or a range?
		public bool Range = false;

		// How far along the segment does the instruction lie on (0 is treated as being on the point)
		public float SegmentT    = 0;
		public float SegmentTEnd = 0;

		// If the segment is considered to be on a specific point, or on the segment
		public bool IsOnPoint => SegmentT <= Mathf.Epsilon;

		public List<IAnjinPathMetadata> Data = new List<IAnjinPathMetadata>();
	}

	public interface IAnjinPathMetadata {

	}

	// An instruction for a PathMover
	public class PathMoverInstruction : IAnjinPathMetadata {
		// TODO
	}
}