using System;
using Photon.Pun;
using UnityEngine;

public class ValuableDesertBirdCage : MonoBehaviour
{
	public enum State
	{
		Idle,
		Dead
	}

	private State stateCurrent;

	private bool stateImpulse;

	private float stateTimer;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private float deathTimer;

	private float chirpTimer;

	private float chirpCooldownTimer;

	private float chirpCooldownTime;

	private float enemyNearestDistance;

	private float enemyNearestDistanceMin = 3f;

	private float enemyNearestDistanceMax = 25f;

	public Transform swingTransform;

	public Transform shakeBirdTransform;

	public SpringQuaternionSystem springSwing;

	public SpringQuaternionSystem springBird;

	public SpringQuaternionSystem springBirdHead;

	public SpringQuaternionSystem springBirdBeakBot;

	public SpringQuaternionSystem springBirdBeakTop;

	public SpringQuaternionSystem springBirdTail;

	public SpringQuaternionSystem springBirdWingLeft;

	public SpringQuaternionSystem springBirdWingRight;

	private float poseAnimationMultiplierTimer;

	public ShakeAnimation shakeBirdX;

	public ShakeAnimation shakeBirdY;

	public ShakeAnimation shakeBirdRotation;

	public Sound soundChirp;

	public Sound soundDie;

	public Sound soundRevive;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		springSwing.Initialize();
		springBird.Initialize();
		springBirdHead.Initialize();
		springBirdBeakBot.Initialize();
		springBirdBeakTop.Initialize();
		springBirdTail.Initialize();
		springBirdWingLeft.Initialize();
		springBirdWingRight.Initialize();
	}

	private void Update()
	{
		StateMachine();
		SpringLogic();
		SwingLogic();
		ShakeLogic();
		PoseAnimationMultiplierLogic();
		EnemyDetectionLogic();
	}

	private void StateIdle()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			soundRevive.Play(physGrabObject.centerPoint);
			ShakeImpulse(2f);
			PoseReset();
			deathTimer = 0f;
			chirpTimer = -1f;
			chirpCooldownTimer = 2f;
			stateImpulse = false;
			return;
		}
		if (chirpTimer > 0f)
		{
			springBird.target.localEulerAngles = new Vector3(-10f, 0f, 0f);
			springBirdHead.target.localEulerAngles = new Vector3(-20f, 0f, 0f);
			springBirdWingRight.target.localEulerAngles = new Vector3(0f, 0f, -20f);
			springBirdWingLeft.target.localEulerAngles = new Vector3(0f, 0f, -20f);
			springBirdBeakBot.target.localEulerAngles = new Vector3(20f, 0f, 0f);
			springBirdBeakTop.target.localEulerAngles = new Vector3(-30f, 0f, 0f);
			chirpTimer -= Time.deltaTime;
			if (chirpTimer <= 0f)
			{
				PoseReset();
				chirpTimer = -1f;
			}
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		float num = Mathf.Lerp(0f, 8f, enemyNearestDistance / enemyNearestDistanceMax);
		if (chirpCooldownTimer > 0f)
		{
			if (chirpCooldownTime >= num * 1.5f)
			{
				chirpCooldownTimer = 0f;
			}
			chirpCooldownTimer -= Time.deltaTime;
		}
		if (enemyNearestDistance < enemyNearestDistanceMin)
		{
			deathTimer += Time.deltaTime;
			if (deathTimer > 2f && chirpCooldownTimer <= 0f)
			{
				StateSet(State.Dead);
			}
		}
		if (enemyNearestDistance < enemyNearestDistanceMax && chirpCooldownTimer <= 0f && chirpTimer <= 0f)
		{
			ChirpSet();
			if (deathTimer > 0f)
			{
				chirpCooldownTime = num / 2f;
			}
			else
			{
				chirpCooldownTime = num;
			}
			chirpCooldownTimer = chirpCooldownTime;
		}
	}

	private void StateDead()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (stateImpulse)
		{
			stateTimer = 2f;
			ShakeImpulse(2f);
			PoseReset();
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				physGrabObject.impactDetector.BreakMedium(physGrabObject.centerPoint, _forceBreak: true);
			}
			soundDie.Play(physGrabObject.centerPoint);
			springBird.target.localEulerAngles = new Vector3(180f, 0f, 0f);
			springBirdWingRight.target.localEulerAngles = new Vector3(130f, 0f, -30f);
			springBirdWingLeft.target.localEulerAngles = new Vector3(130f, 0f, -30f);
			springBirdBeakBot.target.localEulerAngles = new Vector3(20f, 0f, 0f);
			springBirdBeakTop.target.localEulerAngles = new Vector3(-60f, 0f, 0f);
			stateImpulse = false;
		}
		else
		{
			stateTimer -= Time.deltaTime;
			if (SemiFunc.IsMasterClientOrSingleplayer() && stateTimer <= 0f && enemyNearestDistance > enemyNearestDistanceMin * 2f)
			{
				StateSet(State.Idle);
			}
		}
	}

	private void StateMachine()
	{
		switch (stateCurrent)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Dead:
			StateDead();
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

	private void SpringLogic()
	{
		springSwing.UpdateWorldSpace();
		springBird.UpdateWorldSpace();
		springBirdHead.UpdateWorldSpace();
		springBirdBeakBot.UpdateLocalSpace();
		springBirdBeakTop.UpdateLocalSpace();
		springBirdTail.UpdateWorldSpace();
		springBirdWingLeft.UpdateWorldSpace();
		springBirdWingRight.UpdateWorldSpace();
	}

	private void SwingLogic()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		float num = Vector3.Dot(((Component)this).transform.forward, Vector3.up);
		if (stateCurrent == State.Idle)
		{
			swingTransform.localEulerAngles = new Vector3(num * 30f, 0f, 0f);
		}
		else
		{
			swingTransform.localEulerAngles = new Vector3(num * 15f, 0f, 0f);
		}
	}

	private void ShakeImpulse(float _multiplier)
	{
		shakeBirdX.Set(0.02f * _multiplier, 0.0025f * _multiplier);
		shakeBirdY.Set(0.02f * _multiplier, 0.0025f * _multiplier);
		shakeBirdRotation.Set(1f * _multiplier, 0.3f * _multiplier);
	}

	private void ShakeLogic()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		shakeBirdX.UpdateShake();
		shakeBirdY.UpdateShake();
		shakeBirdRotation.UpdateShake();
		shakeBirdTransform.localPosition = Vector2.op_Implicit(new Vector2(shakeBirdX.Get(), shakeBirdY.Get()));
		shakeBirdTransform.localRotation = Quaternion.Euler(shakeBirdRotation.Get(), 0f, 0f);
	}

	private void PoseReset()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		springSwing.target.localRotation = Quaternion.identity;
		springBird.target.localRotation = Quaternion.identity;
		springBirdHead.target.localRotation = Quaternion.identity;
		springBirdBeakBot.target.localRotation = Quaternion.identity;
		springBirdBeakTop.target.localRotation = Quaternion.identity;
		springBirdTail.target.localRotation = Quaternion.identity;
		springBirdWingLeft.target.localRotation = Quaternion.identity;
		springBirdWingRight.target.localRotation = Quaternion.identity;
	}

	private void PoseAnimationMultiplier(float _multiplier, float _time)
	{
		poseAnimationMultiplierTimer = _time;
		springSwing.spring.speed = springSwing.speedDefault * _multiplier;
		springBird.spring.speed = springBird.speedDefault * _multiplier;
		springBirdHead.spring.speed = springBirdHead.speedDefault * _multiplier;
		springBirdBeakBot.spring.speed = springBirdBeakBot.speedDefault * _multiplier;
		springBirdBeakTop.spring.speed = springBirdBeakTop.speedDefault * _multiplier;
		springBirdTail.spring.speed = springBirdTail.speedDefault * _multiplier;
		springBirdWingLeft.spring.speed = springBirdWingLeft.speedDefault * _multiplier;
		springBirdWingRight.spring.speed = springBirdWingRight.speedDefault * _multiplier;
	}

	private void PoseAnimationMultiplierLogic()
	{
		if (poseAnimationMultiplierTimer > 0f)
		{
			poseAnimationMultiplierTimer -= Time.deltaTime;
			if (poseAnimationMultiplierTimer <= 0f)
			{
				PoseAnimationMultiplier(1f, 0f);
			}
		}
	}

	private void ChirpSet()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (GameManager.Multiplayer())
		{
			photonView.RPC("ChirpRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			ChirpRPC();
		}
	}

	private void EnemyDetectionLogic()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.PerSecond(3f, (Object)(object)this))
		{
			return;
		}
		enemyNearestDistance = 999f;
		if (SemiFunc.PlayerNearestDistance(physGrabObject.centerPoint) < enemyNearestDistanceMax + 5f)
		{
			Enemy enemy = SemiFunc.EnemyGetNearest(physGrabObject.centerPoint, enemyNearestDistanceMax + 5f, _raycast: false);
			if (Object.op_Implicit((Object)(object)enemy))
			{
				enemyNearestDistance = Vector3.Distance(physGrabObject.centerPoint, enemy.CenterTransform.position);
			}
		}
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

	[PunRPC]
	private void ChirpRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			soundChirp.Play(physGrabObject.centerPoint);
			ShakeImpulse(1f);
			PoseAnimationMultiplier(2f, 0.5f);
			chirpTimer = 0.25f;
		}
	}
}
