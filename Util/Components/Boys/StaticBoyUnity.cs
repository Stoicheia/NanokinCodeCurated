using UnityEngine;

[DefaultExecutionOrder(-1)]
public abstract class StaticBoyUnity<T> : MonoBehaviour, IStaticBoy
	where T : StaticBoyUnity<T>
{
	private static bool _exists;

	/// <summary>
	/// Gets the 'live' instance of this static boy.
	/// </summary>
	public static T Live { get; set; }

	/// <summary>
	/// A way to check if there's any instance of a StaticBoy
	/// </summary>
	public static bool Exists => _exists; // Checking Live for null is expensive (any unity Object)

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void _Init()
	{
		if (Live)
			Live.ResetStatics();

		Live    = null;
		_exists = false;
	}

	protected virtual void ResetStatics() { }

	protected virtual void Awake()
	{
		_exists = true;
		Live    = (T) this;
		OnAwake();
	}

	protected virtual void OnAwake() { }

	public void SetChildrenActive(bool active)
	{
		var children = GetComponentsInChildren<Transform>(true);

		for (var i = 0; i < children.Length; i++)
		{
			var child = children[i];
			if (child != transform)
			{
				child.gameObject.SetActive(active);
			}
		}
	}
}