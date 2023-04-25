namespace Combat.Data.Entities.Attachments
{
	// public class AttachmentComposerVertical : IAttachmentComposer
	// {
	// 	private readonly Configuration _configuration;
	// 	private          Vector3       _previousCenter;
	//
	// 	public AttachmentComposerVertical(Configuration configuration)
	// 	{
	// 		_configuration = configuration;
	// 	}
	//
	// 	public void ComposeEntities(Vector3 centerPosition, Entity[] entities)
	// 	{
	// 		Vector3 dist = _previousCenter.Towards(centerPosition);
	// 		dist.y = 0;
	//
	// 		Vector3 offset = Vector3.zero;
	//
	// 		float stackTheta = 0;
	//
	// 		for (var i = 0; i < entities.Length; i++)
	// 		{
	// 			Entity view = entities[i];
	// 			offset += Vector3.up * view.Height / 2f; // Move the origin to the bottom of the entity.
	//
	// 			// Calculate the final position.
	// 			Vector3 localPosition = centerPosition
	// 								  + offset                               // Vertical stacking.
	// 								  + dist * i * _configuration.swayForce; // Swaying
	//
	// 			stackTheta += Mathf.Atan2(
	// 							  localPosition.y - centerPosition.y,
	// 							  localPosition.x - centerPosition.x
	// 						  )
	// 						* Mathf.Rad2Deg
	// 						* _configuration.swayAngleForce;
	//
	// 			view.transform.localPosition    = localPosition;
	// 			view.transform.localEulerAngles = new Vector3(0, 0, stackTheta);
	//
	// 			offset += Vector3.up * view.Height / 2f; // Move the cursor to the top of this entity.
	// 		}
	//
	// 		_previousCenter = centerPosition;
	// 	}
	//
	// 	[Serializable]
	// 	public class Configuration
	// 	{
	// 		public float swayForce, swayAngleForce;
	// 	}
	// }
}