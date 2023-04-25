using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Overworld.Controllers;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.Park;
using Util;
using Cysharp.Threading.Tasks;

namespace Anjin.Actors
{
	public class CannoneerProjectile : MonoBehaviour, IHitHandler<SwordHit>
	{
		[SerializeField] private int damage;

		//[SerializeField] private float speed;

		[SerializeField] private ManualTimer lifetime;

		[SerializeField] private SceneReference SceneHUD;

		[SerializeField] private ParticleSystem explosionVFX;

		[SerializeField] private GameObject sphere;
		[SerializeField] private Collider collider;

		[SerializeField] private GameObject P_SwordClash;

		[SerializeField] private AudioDef SFX_Explosion;
		[SerializeField] private AudioDef SFX_SwordClash;

		private bool collided;
		private bool expired;

		private Rigidbody rb;

		private float force;

		private float highestHeight;
		private float lowestHeight;
		private float verticalForce;
		private float sign;
		private float increment;

		//private FollowParabola follow;

		public void Initialize(Vector3 heading, float speed)
		{
			collided = false;
			expired = false;

			sphere.SetActive(true);

			collider.isTrigger = false;

			highestHeight = 4;
			lowestHeight = -7;
			verticalForce = 0;
			sign = 1;
			increment = 20f;

			//Vector3 direction = (cannoneerHeading * Vector3.forward);
			//direction = new Vector3(direction.x, 0, direction.z);

			rb = GetComponent<Rigidbody>();

			// Rotate the current transform to look at the enemy
			//transform.rotation = Quaternion.LookRotation(direction);
			transform.rotation = Quaternion.LookRotation(heading);

			rb.velocity = Vector3.zero;

			force = speed;

			//rb.AddForce(direction * speed);
			rb.AddForce(heading * force);

			lifetime.Restart();
		}

		private void Update()
		{
			if (!collided)
			{
				lifetime.Update(Time.deltaTime);

				if (lifetime.IsDone)
				{
					expired = true;

					PrefabPool.ReturnSafe(gameObject);
				}
			}

			//if (follow != null)
			//{
			//	follow.Update();
			//}
		}

		private void FixedUpdate()
		{
			if (!collided)
			{
				if (verticalForce >= highestHeight)
				{
					verticalForce = highestHeight;
					sign = -1;
				}

				verticalForce = Mathf.Clamp(verticalForce + (increment * Time.fixedDeltaTime * sign), lowestHeight, highestHeight);
			}

			rb.AddForce(Vector3.up * verticalForce);
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!collided && !expired)
			{
				collided = true;

				if (collision.collider.TryGetComponent(out PlayerActor actor) && (actor == ActorController.playerActor))
				{
					if (actor.TryGetComponent(out EncounterPlayer eplayer) && !eplayer.Immune)
					{
						eplayer.AddImmunity(3);

						actor.OnStun((transform.rotation * Vector3.forward));

						CollectibleSystem.Live.OnDamageDealtPercentage(0.1f).Forget();
					}
				}
				else if (collision.collider.TryGetComponent(out Stunnable enemy) && !enemy.Stunned)
				{
					enemy.Stunned = true;
				}

				rb.velocity = Vector3.zero;

				collider.isTrigger = true;

				DestroyCannonball().Forget();
			}
		}

		private async UniTask DestroyCannonball()
		{
			sphere.SetActive(false);

			explosionVFX.Play();

			GameSFX.PlayGlobal(SFX_Explosion, this);

			await UniTask.Delay(1200);

			PrefabPool.ReturnSafe(gameObject);
		}

		public void OnHit(SwordHit hit)
		{
			if (!collided && !expired)
			{
				var actor = ActorController.playerActor;

				if (actor != null)
				{
					GameObject swordClash = PrefabPool.Rent(P_SwordClash, null);
					swordClash.transform.position = transform.position;

					GameSFX.PlayGlobal(SFX_SwordClash);

					rb.velocity = Vector3.zero;

					Vector3 direction = actor.transform.rotation * Vector3.forward;
					direction.y = 0;

					rb = GetComponent<Rigidbody>();

					// Rotate the current transform to look at the enemy
					transform.rotation = Quaternion.LookRotation(direction);

					verticalForce = 0;
					sign = 1;

					rb.AddForce(direction * force * 2);

					lifetime.Restart();
				}
			}
		}

		public bool IsHittable(SwordHit hit) => !collided && !expired;
	}
}
