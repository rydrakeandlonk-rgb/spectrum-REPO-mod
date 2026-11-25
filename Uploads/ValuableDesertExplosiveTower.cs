using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableDesertExplosiveTower : MonoBehaviour
{
	public enum State
	{
		Idle,
		Trigger,
		Reset,
		Explode,
		Destroy
	}

	[Serializable]
	public class BoxParticles
	{
		public List<ParticleSystem> particles;
	}

	private State stateCurrent;

	private bool stateImpulse;

	private float stateTimer;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private PhysGrabObjectGrabArea physGrabArea;

	private ParticleScriptExplosion particleScriptExplosion;

	private bool triggeredByGrabber;

	private float grabberTriggerTimer;

	private float grabberResetTimer;

	public BoxCollider triggerCollider;

	public BoxCollider triggerColliderFree;

	public Transform handleGrabPoint;

	[Space]
	public BoxCollider colliderHandle;

	public BoxCollider colliderHandleUpTarget;

	public BoxCollider colliderHandleDownTarget;

	[Space]
	public Transform handleAnimationTransform;

	public Transform shakeAnimationTransform;

	[Space]
	public Transform dynamitePositionParent;

	public PrefabRef dynamitePrefabRef;

	public AnimationCurve handleCurve;

	private float handleCurveLerp;

	private bool handleState;

	[Space]
	public ShakeAnimation shakeAnimationX;

	public ShakeAnimation shakeAnimationY;

	public ShakeAnimation shakeAnimationRotation;

	[Space]
	public List<BoxParticles> boxParticles;

	private int boxParticlesIndex;

	public Sound triggerPush;

	public Sound triggerPull;

	[Space]
	public List<Sound> triggerBuildup;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		physGrabArea = ((Component)this).GetComponent<PhysGrabObjectGrabArea>();
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		SetHandleCollider(_down: false);
	}

	private void Update()
	{
		if (LevelGenerator.Instance.Generated)
		{
			StateMachine();
			HandleAnimationLogic();
			HandleGrabberOverrideLogic();
			ShakeAnimationLogic();
		}
	}

	private void StateIdle()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 0.5f;
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			if (GrabberTriggerCheck())
			{
				triggeredByGrabber = true;
				StateSet(State.Trigger);
			}
			else if (CollisionTriggerCheck())
			{
				triggeredByGrabber = false;
				StateSet(State.Trigger);
			}
		}
	}

	private void StateTrigger()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			SetHandleCollider(_down: true);
			ShakeAnimationImpulse(0.5f);
			SemiFunc.CameraShakeDistance(physGrabObject.centerPoint, 1.5f, 0.5f, 3f, 8f);
			SemiFunc.CameraShakeImpactDistance(physGrabObject.centerPoint, 1.5f, 0.1f, 3f, 8f);
			triggerPush.Play(handleAnimationTransform.position);
			stateImpulse = false;
			stateTimer = 0f;
			boxParticlesIndex = 0;
		}
		stateTimer += Time.deltaTime;
		bool flag = false;
		switch (boxParticlesIndex)
		{
		case 0:
			if (!(stateTimer > 0.5f))
			{
				break;
			}
			foreach (ParticleSystem particle in boxParticles[boxParticlesIndex].particles)
			{
				particle.Play();
			}
			boxParticlesIndex++;
			flag = true;
			break;
		case 1:
			if (!(stateTimer > 1f))
			{
				break;
			}
			foreach (ParticleSystem particle2 in boxParticles[boxParticlesIndex].particles)
			{
				particle2.Play();
			}
			boxParticlesIndex++;
			flag = true;
			break;
		case 2:
			if (!(stateTimer > 1.5f))
			{
				break;
			}
			foreach (ParticleSystem particle3 in boxParticles[boxParticlesIndex].particles)
			{
				particle3.Play();
			}
			boxParticlesIndex++;
			flag = true;
			break;
		case 3:
			if (!(stateTimer > 2.5f))
			{
				break;
			}
			foreach (BoxParticles boxParticle in boxParticles)
			{
				foreach (ParticleSystem particle4 in boxParticle.particles)
				{
					particle4.Play();
				}
			}
			boxParticlesIndex++;
			flag = true;
			break;
		}
		if (flag)
		{
			triggerBuildup[boxParticlesIndex - 1].Play(physGrabObject.centerPoint);
			ShakeAnimationImpulse(1f);
			SemiFunc.CameraShakeDistance(physGrabObject.centerPoint, 1f, 0.5f, 3f, 8f);
			SemiFunc.CameraShakeImpactDistance(physGrabObject.centerPoint, 1f, 0.1f, 3f, 8f);
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (stateTimer > 0.5f && (GrabberResetCheck() || CollisionResetCheck()))
			{
				StateSet(State.Reset);
			}
			else if (stateTimer > 3f)
			{
				StateSet(State.Explode);
			}
		}
	}

	private void StateReset()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			SetHandleCollider(_down: false);
			ShakeAnimationImpulse(0.5f);
			SemiFunc.CameraShakeDistance(physGrabObject.centerPoint, 1.5f, 0.5f, 3f, 8f);
			SemiFunc.CameraShakeImpactDistance(physGrabObject.centerPoint, 1.5f, 0.1f, 3f, 8f);
			triggerPull.Play(handleAnimationTransform.position);
			stateImpulse = false;
			stateTimer = 0.2f;
		}
		stateTimer -= Time.deltaTime;
		if (SemiFunc.IsMasterClientOrSingleplayer() && stateTimer <= 0f)
		{
			StateSet(State.Idle);
		}
	}

	private void StateExplode()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected Obj, but got Unknown
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			ShakeAnimationImpulse(2f);
			particleScriptExplosion.Spawn(dynamitePositionParent.position, 1f, 20, 50);
			stateImpulse = false;
			stateTimer = 0.1f;
		}
		stateTimer -= Time.deltaTime;
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !(stateTimer <= 0f))
		{
			return;
		}
		physGrabObject.impactDetector.DestroyObject();
		foreach (Transform item in ((Component)dynamitePositionParent).transform)
		{
			Transform val = item;
			if (!SemiFunc.IsMultiplayer())
			{
				Object.Instantiate<GameObject>(dynamitePrefabRef.Prefab, val.position, Quaternion.identity);
			}
			else
			{
				PhotonNetwork.InstantiateRoomObject(dynamitePrefabRef.ResourcePath, val.position, Quaternion.identity, (byte)0, (object[])null);
			}
			StateSet(State.Destroy);
		}
	}

	private void StateMachine()
	{
		switch (stateCurrent)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Trigger:
			StateTrigger();
			break;
		case State.Reset:
			StateReset();
			break;
		case State.Explode:
			StateExplode();
			break;
		}
	}

	private void StateSet(State _state)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && _state != stateCurrent)
		{
			if (GameManager.Multiplayer())
			{
				photonView.RPC("StateSetRPC", (RpcTarget)0, new object[1] { _state });
			}
			else
			{
				StateSetRPC(_state);
			}
		}
	}

	private bool GrabberTriggerCheck()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabArea.listOfAllGrabbers.Count == 0)
		{
			grabberTriggerTimer = 0f;
			return false;
		}
		bool flag = false;
		foreach (PhysGrabber listOfAllGrabber in physGrabArea.listOfAllGrabbers)
		{
			if (Vector3.Dot(listOfAllGrabber.currentGrabForce.normalized, -((Component)triggerCollider).transform.up) > 0.5f)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			grabberTriggerTimer += Time.deltaTime;
		}
		else
		{
			grabberTriggerTimer = 0f;
		}
		return grabberTriggerTimer > 0.25f;
	}

	private bool CollisionTriggerCheck()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			return false;
		}
		Vector3 val = new Vector3(((Component)triggerCollider).transform.lossyScale.x * triggerCollider.size.x, ((Component)triggerCollider).transform.lossyScale.y * triggerCollider.size.y, ((Component)triggerCollider).transform.lossyScale.z * triggerCollider.size.z);
		Collider[] array = Physics.OverlapBox(((Component)triggerCollider).transform.TransformPoint(triggerCollider.center), val / 2f, ((Component)triggerCollider).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPlayersAndPhysObjects()) + LayerMask.GetMask(new string[1] { "Default" }) + LayerMask.GetMask(new string[1] { "PhysGrabAvoider" }));
		foreach (Collider val2 in array)
		{
			PlayerPhysPusher playerPhysPusher = null;
			PlayerController componentInParent = ((Component)val2).GetComponentInParent<PlayerController>();
			PlayerAvatar playerAvatar;
			if (Object.op_Implicit((Object)(object)componentInParent))
			{
				playerAvatar = componentInParent.playerAvatarScript;
			}
			else
			{
				playerAvatar = ((Component)val2).GetComponentInParent<PlayerAvatar>();
				if (!Object.op_Implicit((Object)(object)playerAvatar))
				{
					playerPhysPusher = ((Component)val2).GetComponentInParent<PlayerPhysPusher>();
					if (Object.op_Implicit((Object)(object)playerPhysPusher))
					{
						playerAvatar = playerPhysPusher.Player;
					}
				}
			}
			if (Object.op_Implicit((Object)(object)playerAvatar))
			{
				Vector3 normalized = playerAvatar.rbVelocityRaw.normalized;
				if (Object.op_Implicit((Object)(object)playerPhysPusher))
				{
					Vector3 linearVelocity = playerPhysPusher.Rigidbody.linearVelocity;
					normalized = linearVelocity.normalized;
				}
				if (Vector3.Dot(normalized, -((Component)triggerCollider).transform.up) > 0.5f)
				{
					return true;
				}
			}
			PhysGrabObject componentInParent2 = ((Component)val2).GetComponentInParent<PhysGrabObject>();
			if (Object.op_Implicit((Object)(object)componentInParent2))
			{
				if ((Object)(object)componentInParent2 == (Object)(object)physGrabObject)
				{
					continue;
				}
				if (Vector3.Dot(physGrabObject.rbVelocityBeforeImpact.normalized, -((Component)triggerCollider).transform.up) > 0.5f)
				{
					return true;
				}
			}
			if (((Component)val2).gameObject.layer == LayerMask.NameToLayer("Default") && Vector3.Dot(physGrabObject.rbVelocityBeforeImpact.normalized, ((Component)triggerCollider).transform.up) > 0.5f)
			{
				return true;
			}
		}
		return false;
	}

	private bool GrabberResetCheck()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabArea.listOfAllGrabbers.Count == 0)
		{
			grabberResetTimer = 0f;
			return false;
		}
		bool flag = false;
		foreach (PhysGrabber listOfAllGrabber in physGrabArea.listOfAllGrabbers)
		{
			if (Vector3.Dot(listOfAllGrabber.currentGrabForce.normalized, ((Component)triggerCollider).transform.up) > 0.5f)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			grabberResetTimer += Time.deltaTime;
		}
		else
		{
			grabberResetTimer = 0f;
		}
		if (grabberResetTimer > 0.25f && CollisionResetCheck(_forceCheck: true))
		{
			return true;
		}
		return false;
	}

	private bool CollisionResetCheck(bool _forceCheck = false)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			return false;
		}
		if (!_forceCheck && triggeredByGrabber)
		{
			return false;
		}
		Vector3 val = new Vector3(((Component)triggerColliderFree).transform.lossyScale.x * triggerColliderFree.size.x, ((Component)triggerColliderFree).transform.lossyScale.y * triggerColliderFree.size.y, ((Component)triggerColliderFree).transform.lossyScale.z * triggerColliderFree.size.z);
		Collider[] array = Physics.OverlapBox(((Component)triggerColliderFree).transform.TransformPoint(triggerColliderFree.center), val / 2f, ((Component)triggerColliderFree).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPlayersAndPhysObjects()) + LayerMask.GetMask(new string[1] { "Default" }));
		bool result = true;
		Collider[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (!((Object)(object)((Component)array2[i]).GetComponentInParent<ValuableDesertExplosiveTower>() == (Object)(object)this))
			{
				result = false;
			}
		}
		return result;
	}

	private void SetHandleCollider(bool _down)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		BoxCollider val = colliderHandleUpTarget;
		if (_down)
		{
			val = colliderHandleDownTarget;
		}
		((Component)colliderHandle).transform.localPosition = ((Component)val).transform.localPosition;
		((Component)colliderHandle).transform.localRotation = ((Component)val).transform.localRotation;
		((Component)colliderHandle).transform.localScale = ((Component)val).transform.localScale;
		colliderHandle.center = val.center;
		colliderHandle.size = val.size;
	}

	private void HandleAnimationLogic()
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		if (stateCurrent == State.Idle || stateCurrent == State.Reset)
		{
			if (!handleState)
			{
				handleState = true;
				handleCurveLerp = 0f;
			}
			if (handleCurveLerp < 1f)
			{
				handleCurveLerp += 2f * Time.deltaTime;
				handleAnimationTransform.localPosition = new Vector3(0f, Mathf.LerpUnclamped(-0.33f, 0f, handleCurve.Evaluate(handleCurveLerp)), 0f);
			}
		}
		else
		{
			if (handleState)
			{
				handleState = false;
				handleCurveLerp = 0f;
			}
			if (handleCurveLerp < 1f)
			{
				handleCurveLerp += 2f * Time.deltaTime;
				handleAnimationTransform.localPosition = new Vector3(0f, Mathf.LerpUnclamped(0f, -0.33f, handleCurve.Evaluate(handleCurveLerp)), 0f);
			}
		}
	}

	private void HandleGrabberOverrideLogic()
	{
		foreach (PhysGrabber listOfAllGrabber in physGrabArea.listOfAllGrabbers)
		{
			listOfAllGrabber.OverrideGrabPoint(((Component)handleGrabPoint).transform);
		}
	}

	private void ShakeAnimationImpulse(float _multiplier)
	{
		shakeAnimationX.Set(0.02f * _multiplier, 0.0025f * _multiplier);
		shakeAnimationY.Set(0.02f * _multiplier, 0.0025f * _multiplier);
		shakeAnimationRotation.Set(1f * _multiplier, 0.3f * _multiplier);
	}

	private void ShakeAnimationLogic()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		shakeAnimationX.UpdateShake();
		shakeAnimationY.UpdateShake();
		shakeAnimationRotation.UpdateShake();
		shakeAnimationTransform.localPosition = Vector2.op_Implicit(new Vector2(shakeAnimationX.Get(), shakeAnimationY.Get()));
		shakeAnimationTransform.localRotation = Quaternion.Euler(shakeAnimationRotation.Get(), 0f, 0f);
	}

	[PunRPC]
	private void StateSetRPC(State _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			stateCurrent = _state;
			stateImpulse = true;
			stateTimer = 0f;
			StateMachine();
		}
	}
}
