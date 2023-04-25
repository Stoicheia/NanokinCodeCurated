using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Comonents.Boys;

public interface IStaticBoy { }

/// <summary>
/// A version of StaticBoy, but using Odin.
/// </summary>
[DefaultExecutionOrder(-1)]
public abstract class StaticBoy<T> : StaticBoyBase
	where T : StaticBoy<T>
{
	private static bool _exists = false;

	/// <summary>
	/// Gets the 'live' instance of this static boy.
	/// </summary>
	//public static T Live { get; set; }
	private static T _live;
	private static T _cachedEdtiorLive;

	[ShowInInspector, ReadOnly]
	public static T Live
	{
		get
		{
			if (_live != null)
				return _live;

			if (_cachedEdtiorLive == null)
				_cachedEdtiorLive = FindObjectOfType<T>();

			return _cachedEdtiorLive;
		}
		set => _live = value;
	}

	public static async UniTask TillLiveExists() => await UniTask.WaitUntil(() => Live != null);

	/// <summary>
	/// A way to check if there's any instance of a StaticBoy
	/// </summary>
	public static bool Exists => Live != null;
	// Oxy: Checking Live for null is expensive (any unity Object)
	// CL: Reverted as the _Init function doesn't seem to ever get called.

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void _Init()
	{
		//Debug.Log("Staticboy Init " + typeof(T));

		if (Live)
			Live.ResetStatics();

		Live    = null;
		_exists = false;
	}


	protected virtual void ResetStatics() { }

	public virtual void Awake()
	{
		_exists = true;
		Live    = (T)this;
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

	public override void Reset()
	{
		// Debug.Log($"Reset static boy: {typeof(T)}");
		_live             = null;
		_cachedEdtiorLive = null;
	}
}