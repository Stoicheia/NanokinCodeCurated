using System;
using Anjin.Util;
using Cinemachine;
using MoonSharp.Interpreter;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util.Extensions;
using Object = UnityEngine.Object;

// ReSharper disable UnusedMember.Global

namespace Anjin.Scripting
{
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public abstract class UnityObjectLuaProxy<O> : LuaProxy<O> where O : Object
	{
		public string name      { get => proxy.name; set => proxy.name = value; }
		public void   destroy() => proxy.Destroy();

		public O instantiate(DynValue dv1 = null)
		{
			if(dv1.AsUserdata(out Vector3 pos))
				return Object.Instantiate(proxy, pos, Quaternion.identity);

			return Object.Instantiate(proxy);
		}
	}

	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public abstract class ComponentLuaProxy<O> : UnityObjectLuaProxy<O> where O : Component
	{
		public int foo;

		public GameObject game_object { get => proxy.gameObject; }
		public Transform  transform   { get => proxy.transform; }

		public Component   get_component(Type              t) => t != null ? proxy.GetComponent(t) : null;
		public Component[] get_components_in_children(Type t) => t != null ? proxy.GetComponentsInChildren(t, true) : new Component[0];

	}

	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public abstract class MonoLuaProxy<O> : ComponentLuaProxy<O> where O : MonoBehaviour
	{
		public bool enabled               { get => proxy.enabled; set => proxy.enabled = value; }
		public bool is_active_and_enabled { get => proxy.isActiveAndEnabled; }
	}

	public class GameObjectLuaProxy : UnityObjectLuaProxy<GameObject>
	{
		public void        set_active(bool                 active) => proxy.SetActive(active);
		public Component   get_component(Type              t)      => proxy.GetComponent(t);
		public Component[] get_components_in_children(Type t)      => t != null ? proxy.GetComponentsInChildren(t, true) : new Component[0];
		public Transform   transform                               { get => proxy.transform; }
		public Scene       scene                                   => proxy.scene;

		public void scale(DynValue val)
		{
			if (val.AsUserdata(out Vector3 v3))
				proxy.transform.localScale = v3;
			else if (val.AsUserdata(out float f))
				proxy.transform.localScale = new Vector3(f, f, f);
		}
	}

	public class TransformLuaProxy : UnityObjectLuaProxy<Transform>
	{
		public Vector3    position    { get => proxy.position;   set => proxy.position = value; }
		public Quaternion rotation    { get => proxy.rotation;   set => proxy.rotation = value; }
		public Vector3    local_scale { get => proxy.localScale; set => proxy.localScale = value; }

		public Vector3    local_position { get => proxy.localPosition; set => proxy.localPosition = value; }
		public Quaternion local_rotation { get => proxy.localRotation; set => proxy.localRotation = value; }

		public Vector3 forward { get => proxy.forward; }
		public Vector3 right   { get => proxy.right; }
		public Vector3 up      { get => proxy.up; }

		public void reset()
		{
			proxy.position   = Vector3.zero;
			proxy.rotation   = Quaternion.identity;
			proxy.localScale = Vector3.one;
		}

		public void reset_local()
		{
			proxy.localPosition = Vector3.zero;
			proxy.localRotation = Quaternion.identity;
			proxy.localScale    = Vector3.one;
		}

		public void set_active(bool       active) => proxy.SetActive(active);

		public WorldPoint offset(float          x, float y, float horizontal = 0) => proxy.offset(x, y, horizontal);
		public WorldPoint identity_offset(float d)                                           => proxy.identity_offset(d);
		public WorldPoint polar_offset(float    rad,      float angle, float horizontal = 0) => proxy.polar_offset(rad, angle, horizontal);
		public WorldPoint ahead(float           distance, float horizontal = 0) => proxy.ahead(distance, horizontal);
		public WorldPoint behind(float          distance, float horizontal = 0) => proxy.behind(distance, horizontal);
		public WorldPoint above(float           distance = 0) => proxy.above(distance);
		public WorldPoint under(float           distance)     => proxy.under(distance);
	}

	public class RigidbodyLuaProxy : ComponentLuaProxy<Rigidbody>
	{
		public Vector3 velocity         { get => proxy.velocity;        set => proxy.velocity = value; }
		public Vector3 angular_velocity { get => proxy.angularVelocity; set => proxy.angularVelocity = value; }

		public void add_force(Vector3 vec)                 => proxy.AddForce(vec);
		public void add_force(float   x, float y, float z) => proxy.AddForce(x, y, z);
	}

	public class VCamLuaProxy : MonoLuaProxy<CinemachineVirtualCamera>
	{
		public int          priority { get => proxy.Priority; set => proxy.Priority = value; }
		public CameraState  state    { get => proxy.State; }
		public LensSettings lens     { get => proxy.m_Lens; set => proxy.m_Lens = value; }
	}

	[LuaProxyTypes(typeof(TextMeshProUGUI), typeof(TextMeshPro))]
	public class TextmeshProProxy : MonoLuaProxy<TMP_Text>
	{
		public string text { get => proxy.text; set => proxy.text = value; }
	}
}