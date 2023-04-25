namespace Combat.Data.Entities.Attachments
{
	// public class AttachmentComposerFan : IAttachmentComposer
	// {
	// 	public void ComposeEntities(Vector3 centerPosition, Entity[] entities)
	// 	{
	// 		float ANGLE_RANGE = 5 + entities.Length * 1.2f;
	// 		float POS_RANGE_X = 0.03f + entities.Length * 0.01f;
	// 		var   POS_RANGE_Y = 0.15f;
	//
	// 		for (var idx = 0; idx < entities.Length; idx++)
	// 		{
	// 			float t     = (idx + 1) / (float) entities.Length;
	// 			float x     = -POS_RANGE_X + POS_RANGE_X * 2 * t;
	// 			float y     = Mathf.Sin(t * Mathf.PI) * POS_RANGE_Y;
	// 			float theta = -ANGLE_RANGE - ANGLE_RANGE * 2 * t;
	//
	// 			Entity attachment = entities[idx];
	//
	// 			attachment.transform.localPosition    = centerPosition + x * Vector3.right + y * Vector3.up;
	// 			attachment.transform.localEulerAngles = new Vector3(0, 0, theta);
	// 		}
	// 	}
	// }
}