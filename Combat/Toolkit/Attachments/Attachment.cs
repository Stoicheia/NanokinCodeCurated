namespace Combat.Data.Entities.Attachments
{
	// public abstract class Attachment<TEntity> : IAttachment
	// 	where TEntity : IAttachableEntity
	// {
	// 	protected Attachment(TEntity hostEntity)
	// 	{
	// 		HostEntity = hostEntity;
	// 	}
	//
	// 	public TEntity       HostEntity { get; }
	// 	public List<TEntity> Entities   { get; } = new List<TEntity>();
	//
	// 	/// <summary>
	// 	/// Updates the attachment's position to its proper position.
	// 	/// </summary>
	// 	public abstract void Update();
	//
	// 	public void Remove(TEntity entity)
	// 	{
	// 		Entities.Remove(entity);
	// 		Update();
	// 	}
	//
	// 	public void Add(TEntity attachedEntity)
	// 	{
	// 		Entities.Add(attachedEntity);
	// 		attachedEntity.RootTransform.SetParent(HostEntity.AttachmentParent, false);
	//
	// 		Update();
	// 	}
	//
	// 	public void Insert(int idx, TEntity entity)
	// 	{
	// 		Entities.Insert(idx, entity);
	// 		entity.RootTransform.SetParent(HostEntity.RootTransform, false);
	// 		Update();
	// 	}
	// }
}