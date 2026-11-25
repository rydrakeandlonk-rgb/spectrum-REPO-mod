using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableCar : Trap
{
	public enum State
	{
		Inactive,
		Idle,
		MoveForward
	}

	[Header("Animation")]
	public Transform carBody;

	public Transform wheelsFront;

	public Transform wheelsBack;

	public Transform driverBody;

	public Transform driverArms;

	public AnimationCurve carBodyCurve;

	private float carBodyLerp;

	private float wheelsLerp;

	private float driverBodyLerp = 0.9f;

	public AnimationCurve driverArmsCurve;

	private float driverArmsLerp;

	[Header("Sounds")]
	public Sound sfxCarHorn;

	public Sound sfxCarStart;

	public Sound sfxCarStop;

	public Sound sfxCarLoop;

	private bool grabbedPrev;

	private bool loopPlaying;

	private bool loopPlayingPrevious;

	private float loopPitch;

	[Header("Colliders")]
	public List<Collider> carColliders;

	public PhysicsMaterial defaultPhysicMaterial;

	public PhysicsMaterial movingPhysicMaterial;

	public Transform centerTransform;

	private PhysGrabObjectImpactDetector impactDetector;

	private Vector3 boxSize = new Vector3(0.16f, 0.1f, 0.42f);

	private bool stuck;

	private float stuckTime;

	private Vector3 previousPosition;

	private State currentState;

	private bool stateImpulse;

	private float stateTimer;

	private float playerNearTimer;

	protected override void Start()
	{
		base.Start();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		currentState = State.Inactive;
	}

	protected override void Update()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0438: Unknown result type (might be due to invalid IL or missing references)
		//IL_043d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0464: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_0493: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_050c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0510: Unknown result type (might be due to invalid IL or missing references)
		//IL_051a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_057f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_058f: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		base.Update();
		loopPlaying = currentState == State.MoveForward;
		float num = loopPitch;
		Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
		loopPitch = Mathf.Lerp(num, 1f + linearVelocity.magnitude * 1f, Time.deltaTime * 5f);
		sfxCarLoop.PlayLoop(loopPlaying, 0.8f, 0.8f, loopPitch);
		if (loopPlaying != loopPlayingPrevious)
		{
			if (loopPlaying)
			{
				sfxCarStart.Play(((Component)this).transform.position);
			}
			else
			{
				sfxCarStop.Play(((Component)this).transform.position);
			}
			loopPlayingPrevious = loopPlaying;
		}
		if (physGrabObject.grabbed)
		{
			if (!grabbedPrev)
			{
				grabbedPrev = true;
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					PlayHonk();
					Quaternion turnX = Quaternion.Euler(-45f, 0f, 0f);
					Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
					Quaternion identity = Quaternion.identity;
					physGrabObject.TurnXYZ(turnX, turnY, identity);
				}
				if (!physGrabObject.grabbedLocal)
				{
				}
			}
		}
		else
		{
			grabbedPrev = false;
		}
		float num2 = 0.102f;
		Quaternion localRotation;
		if (currentState == State.MoveForward)
		{
			if (carBodyLerp < 1f)
			{
				carBodyLerp += Time.deltaTime * 4f;
			}
			if (carBodyLerp >= 1f)
			{
				carBodyLerp = 0f;
			}
			float num3 = carBodyCurve.Evaluate(carBodyLerp) * 0.015f;
			carBody.localPosition = new Vector3(carBody.localPosition.x, num3 + num2, carBody.localPosition.z);
			if (wheelsLerp < 1f)
			{
				wheelsLerp += Time.deltaTime * 4f;
			}
			if (wheelsLerp >= 1f)
			{
				wheelsLerp = 0f;
			}
			float num4 = 720f;
			wheelsFront.Rotate(new Vector3(num4, 0f, 0f) * Time.deltaTime, (Space)1);
			wheelsBack.Rotate(new Vector3(num4, 0f, 0f) * Time.deltaTime, (Space)1);
			if (driverBodyLerp < 1f)
			{
				driverBodyLerp += Time.deltaTime * 1f;
			}
			if (driverBodyLerp >= 1f)
			{
				driverBodyLerp = 0f;
			}
			float num5 = carBodyCurve.Evaluate(carBodyLerp) * 6f;
			Transform val = driverBody;
			localRotation = driverBody.localRotation;
			float x = localRotation.eulerAngles.x;
			localRotation = driverBody.localRotation;
			val.localRotation = Quaternion.Euler(x, localRotation.eulerAngles.y, num5);
			if (driverArmsLerp < 1f)
			{
				driverArmsLerp += Time.deltaTime * 1f;
			}
			if (driverArmsLerp >= 1f)
			{
				driverArmsLerp = 0f;
			}
			Transform val2 = driverArms;
			localRotation = driverArms.localRotation;
			float x2 = localRotation.eulerAngles.x;
			localRotation = driverArms.localRotation;
			val2.localRotation = Quaternion.Euler(x2, localRotation.eulerAngles.y, driverArmsCurve.Evaluate(driverArmsLerp) * 20f);
		}
		else if (stateImpulse)
		{
			carBody.localPosition = new Vector3(carBody.localPosition.x, num2, carBody.localPosition.z);
			Transform val3 = wheelsFront;
			localRotation = wheelsFront.localRotation;
			float x3 = localRotation.eulerAngles.x;
			localRotation = wheelsFront.localRotation;
			float y = localRotation.eulerAngles.y;
			localRotation = wheelsFront.localRotation;
			val3.localRotation = Quaternion.Euler(x3, y, localRotation.eulerAngles.z);
			Transform val4 = wheelsBack;
			localRotation = wheelsBack.localRotation;
			float x4 = localRotation.eulerAngles.x;
			localRotation = wheelsBack.localRotation;
			float y2 = localRotation.eulerAngles.y;
			localRotation = wheelsBack.localRotation;
			val4.localRotation = Quaternion.Euler(x4, y2, localRotation.eulerAngles.y);
			Transform val5 = driverArms;
			localRotation = driverArms.localRotation;
			float x5 = localRotation.eulerAngles.x;
			localRotation = driverArms.localRotation;
			float y3 = localRotation.eulerAngles.y;
			localRotation = driverArms.localRotation;
			val5.localRotation = Quaternion.Euler(x5, y3, localRotation.eulerAngles.z);
		}
		StateMachine(_fixedUpdate: false);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			if (currentState == State.Inactive)
			{
				return;
			}
			if (impactDetector.inCart || physGrabObject.playerGrabbing.Count > 0)
			{
				UpdateState(State.Idle);
				return;
			}
			bool flag = false;
			Collider[] array = Physics.OverlapBox(centerTransform.position, boxSize / 2f, ((Component)this).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()));
			for (int i = 0; i < array.Length; i++)
			{
				if (!Object.op_Implicit((Object)(object)((Component)array[i]).GetComponentInParent<ValuableCar>()))
				{
					flag = true;
					if (currentState != State.MoveForward && currentState != State.Inactive)
					{
						UpdateState(State.MoveForward);
					}
					break;
				}
			}
			if (!flag)
			{
				UpdateState(State.Idle);
			}
			else
			{
				StuckCheck();
			}
		}
		if (stuck)
		{
			stuckTime += Time.deltaTime;
			if (stuckTime >= 15f)
			{
				UpdateState(State.Inactive);
			}
		}
	}

	public void FixedUpdate()
	{
		StateMachine(_fixedUpdate: true);
	}

	private void StateInactive(bool _fixedUpdate)
	{
		if (!_fixedUpdate && physGrabObject.grabbed)
		{
			UpdateState(State.Idle);
		}
	}

	private void StateIdle(bool _fixedUpdate)
	{
		if (_fixedUpdate || !stateImpulse)
		{
			return;
		}
		foreach (Collider carCollider in carColliders)
		{
			if (!((Object)(object)carCollider == (Object)null))
			{
				carCollider.material = defaultPhysicMaterial;
			}
		}
		stateImpulse = false;
	}

	private void StateMoveForward(bool _fixedUpdate)
	{
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		if (!_fixedUpdate)
		{
			return;
		}
		if (stateImpulse)
		{
			stateTimer = 2f;
			foreach (Collider carCollider in carColliders)
			{
				if (!((Object)(object)carCollider == (Object)null))
				{
					carCollider.material = movingPhysicMaterial;
				}
			}
			stateImpulse = false;
		}
		stateTimer -= Time.fixedDeltaTime;
		if (stateTimer <= 0f)
		{
			stateTimer = 1f;
			if (Random.Range(0f, 1f) > 0.5f)
			{
				TurnCar();
			}
		}
		Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
		float num = 4f - linearVelocity.magnitude;
		physGrabObject.rb.AddForce(((Component)this).transform.forward * num * Time.fixedDeltaTime, (ForceMode)1);
		Vector3 val = new Vector3(0f, Mathf.Sin(Time.time * 10f) * 0.006f, 0f);
		physGrabObject.rb.AddTorque(val, (ForceMode)1);
		if (playerNearTimer >= 0f)
		{
			playerNearTimer -= Time.deltaTime;
			return;
		}
		playerNearTimer = 1f;
		bool flag = false;
		foreach (PlayerAvatar item in SemiFunc.PlayerGetList())
		{
			if (Vector3.Distance(((Component)item).transform.position, ((Component)this).transform.position) < 25f)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			UpdateState(State.Inactive);
		}
	}

	private void UpdateState(State _state)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && _state != currentState)
		{
			stuck = false;
			stuckTime = 0f;
			currentState = _state;
			stateImpulse = true;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("UpdateStateRPC", (RpcTarget)0, new object[1] { currentState });
			}
			else
			{
				UpdateStateRPC(currentState);
			}
		}
	}

	[PunRPC]
	private void UpdateStateRPC(State _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			currentState = _state;
		}
	}

	private void PlayHonk()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, 15f);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("PlayHonkRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			PlayHonkRPC();
		}
	}

	[PunRPC]
	private void PlayHonkRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			sfxCarHorn.Play(((Component)this).transform.position);
		}
	}

	private void StateMachine(bool _fixedUpdate)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			switch (currentState)
			{
			case State.Inactive:
				StateInactive(_fixedUpdate);
				break;
			case State.Idle:
				StateIdle(_fixedUpdate);
				break;
			case State.MoveForward:
				StateMoveForward(_fixedUpdate);
				break;
			}
		}
	}

	private void TurnCar()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = new Vector3(0f, Random.Range(-1f, 1f) * 0.1f, 0f);
		physGrabObject.rb.AddTorque(val, (ForceMode)1);
		PlayHonk();
	}

	private void StuckCheck()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Inactive && currentState != State.Idle)
		{
			Vector3 val = wheelsFront.position - previousPosition;
			float magnitude = val.magnitude;
			previousPosition = wheelsFront.position;
			if (magnitude >= 0.2f)
			{
				stuck = false;
				stuckTime = 0f;
			}
			else
			{
				stuck = true;
			}
		}
	}

	public ValuableCar()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
	}
}
