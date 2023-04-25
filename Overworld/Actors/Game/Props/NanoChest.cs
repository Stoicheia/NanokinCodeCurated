using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anjin.UI;
using Anjin.Util;
using Anjin.Utils;
using Cysharp.Threading.Tasks;
using Data.Shops;
using Overworld.Cutscenes;
using Overworld.Cutscenes.Implementations;
using Overworld.QuestCompass;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;


namespace Anjin.Actors
{
	[RequireComponent(typeof(Interactable), typeof(MapObjective))]
	public class NanoChest : Actor
	{
		private static readonly int OpenAnimHash = Animator.StringToHash("Open");

		Animator Animator;

		[SerializeField] public  LootEntry       Loot;
		[SerializeField] public  Cutscene        OpenCutscene;
		[SerializeField] private List<Transform> Particles;
		[SerializeField] private bool            ForceDisableOnInteract = true;

		private Collider _col;

		private bool _opened;
		private bool _subscribed;
		private Interactable _interactable;
		private Cutscene _cutscene;

		private TalkNPC _talkNPC;
		private MapObjective _objective;

		public bool Opened
		{
			get => _opened;
			private set
			{
				_opened = value;

				Animator.SetBool(OpenAnimHash, Opened);

				for (int i = 0; i < Particles.Count; i++)
				{
					Particles[i].SetActive(!Opened);
				}

				ToggleInteract(!value || !ForceDisableOnInteract);
				_objective.Toggle(!value);
			}
		}

		protected virtual void Awake()
		{
			_subscribed = false;
			Animator = GetComponent<Animator>();
			_col = GetComponent<Collider>();
			_objective = GetComponent<MapObjective>();

			_interactable    = GetComponent<Interactable>();
			_talkNPC         = GetComponent<TalkNPC>();

			if(_talkNPC != null)
				_talkNPC.enabled = Opened;

			_cutscene        = _interactable.Cutscene;
			ToggleInteract(true);
		}

		private void ToggleInteract(bool b)
		{
			if (b == _subscribed) return;
			if (b)
			{
			   _interactable.OnInteract.AddListener(InteractAction);
			   _interactable.Cutscene = _cutscene;
			   if (_talkNPC != null)
				   _talkNPC.enabled = false;
			   _subscribed = true;
			   return;
			}
			_interactable.OnInteract.RemoveListener(InteractAction);
			_interactable.Cutscene = null;
			if (_talkNPC != null)
				_talkNPC.enabled = true;
			_subscribed = false;
		}

		private void InteractAction()
		{
			OnInteract();
		}

		private async UniTask OnInteract()
		{
			if (Opened)
			{
				return;
			}
			Opened = true;

			Loot.AwardOne();
			//await SceneLoader.GetOrLoadAsync("ItemGetCutscene");
			ItemGetCutscene.SetLoot(Loot);
			OpenCutscene.Play();
		}
	}
}