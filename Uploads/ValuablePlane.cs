using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuablePlane : Trap
{
	public enum State
	{
		Inactive,
		Idle,
		MoveForward,
		TakeOff,
		Flying
	}

	[Header("Animation")]
	public Transform planeBody;

	public Transform pilot;

	public Transform propeller;

	public Transform wheels;

	public Transform crank;

	[Space]
	public AnimationCurve planeBodyCurve;

	[Header("Flight Settings")]
	public float takeOffSpeed = 5.5f;

	public float liftPerSpeed = 3.5f;

	public float maxLiftForce = 18f;

	public float cruiseSpeed = 6.5f;

	public float flightSpeed = 4f;

	[Header("Sounds")]
	public Sound sfxPlaneStart;

	public Sound sfxPlaneStop;

	public Sound sfxPlaneLoop;

	public Sound sfxCrankLoop;

	[Header("Colliders")]
	public List<Collider> planeColliders;

	[Header("Physics Materials")]
	public PhysicsMaterial defaultPhysicMaterial;

	public PhysicsMaterial movingPhysicMaterial;

	[Header("Transforms")]
	public Transform centerTransform;

	private float planeBodyLerp;

	private float wheelsLerp;

	private float pilotLerp = 0.9f;

	private bool grabbedPrev;

	private bool loopPlaying;

	private bool loopPlayingPrevious;

	private float loopPitch;

	private bool playersNearby;

	private bool stuck;

	private float stuckTime;

	private Vector3 previousPosition;

	private PhysGrabObjectImpactDetector impactDetector;

	private Vector3 boxSize = new Vector3(0.16f, 0.1f, 0.42f);

	private bool activated;

	private State currentState;

	private bool stateImpulse;

	private float stateTimer;

	private float levelStrength = 4f;

	private float levelDamping = 1.2f;

	private float bankLimitDeg = 45f;

	protected override void Start()
	{
		base.Start();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		currentState = State.Inactive;
	}

	protected override void Update()
	{
		base.Update();
		HandleAudio();
		HandleGrabbedOrientation();
		AnimateParts();
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (SemiFunc.PerSecond(5f, (Object)(object)this))
		{
			ServerSideThink();
			StuckCheck();
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
		StateMachine(fixedUpdate: true);
	}

	private void HandleAudio()
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		loopPlaying = currentState == State.MoveForward || currentState == State.TakeOff || currentState == State.Flying;
		float num = loopPitch;
		Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
		loopPitch = Mathf.Lerp(num, 1f + linearVelocity.magnitude * 0.8f, Time.deltaTime * 5f);
		sfxPlaneLoop.PlayLoop(loopPlaying, 0.8f, 0.8f, loopPitch);
		sfxCrankLoop.PlayLoop(currentState == State.Idle && !impactDetector.inCart, 0.8f, 0.8f, loopPitch);
		if (loopPlaying != loopPlayingPrevious)
		{
			if (loopPlaying)
			{
				sfxPlaneStart.Play(((Component)this).transform.position);
			}
			else
			{
				sfxPlaneStop.Play(((Component)this).transform.position);
			}
			loopPlayingPrevious = loopPlaying;
		}
	}

	private void HandleGrabbedOrientation()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.grabbed)
		{
			if (!grabbedPrev)
			{
				grabbedPrev = true;
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					Quaternion turnX = Quaternion.Euler(-45f, 0f, 0f);
					Quaternion identity = Quaternion.identity;
					Quaternion identity2 = Quaternion.identity;
					physGrabObject.TurnXYZ(turnX, identity, identity2);
				}
			}
		}
		else
		{
			grabbedPrev = false;
		}
	}

	private void AnimateParts()
	{
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Idle && !impactDetector.inCart)
		{
			crank.Rotate(Vector3.right * 360f * 2f * (0f - Time.deltaTime), (Space)1);
			planeBodyLerp = (planeBodyLerp + Time.deltaTime * 4f) % 1f;
			float num = planeBodyCurve.Evaluate(planeBodyLerp) * 0.015f;
			planeBody.localPosition = new Vector3(planeBody.localPosition.x, num, planeBody.localPosition.z);
		}
		if (currentState == State.MoveForward || currentState == State.TakeOff || currentState == State.Flying)
		{
			planeBodyLerp = (planeBodyLerp + Time.deltaTime * 4f) % 1f;
			float num2 = planeBodyCurve.Evaluate(planeBodyLerp) * 0.015f;
			planeBody.localPosition = new Vector3(planeBody.localPosition.x, num2, planeBody.localPosition.z);
			wheels.Rotate(Vector3.right * 360f * 2f * Time.deltaTime, (Space)1);
			pilotLerp = (pilotLerp + Time.deltaTime) % 1f;
			float num3 = planeBodyCurve.Evaluate(planeBodyLerp) * 6f;
			Transform val = pilot;
			Quaternion localRotation = pilot.localRotation;
			float x = localRotation.eulerAngles.x;
			localRotation = pilot.localRotation;
			val.localRotation = Quaternion.Euler(x, localRotation.eulerAngles.y, num3);
			propeller.Rotate(Vector3.forward * 360f * 5f * Time.deltaTime, (Space)1);
			crank.Rotate(Vector3.right * 360f * 2f * Time.deltaTime, (Space)1);
		}
		else if (stateImpulse)
		{
			planeBody.localPosition = new Vector3(planeBody.localPosition.x, 0f, planeBody.localPosition.z);
			wheels.localRotation = Quaternion.identity;
		}
	}

	private void ServerSideThink()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Inactive)
		{
			if (impactDetector.inCart || physGrabObject.playerGrabbing.Count > 0)
			{
				UpdateState(State.Idle);
				return;
			}
			bool flag = Physics.OverlapBox(centerTransform.position, boxSize / 2f, ((Component)this).transform.rotation, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct())).Length != 0;
			if (flag && currentState == State.Idle)
			{
				UpdateState(State.MoveForward);
			}
			else if (!flag && currentState == State.MoveForward)
			{
				UpdateState(State.Idle);
			}
		}
		CheckForNearbyPlayers();
		if (!playersNearby)
		{
			UpdateState(State.Inactive);
		}
		StateMachine(fixedUpdate: false);
	}

	private void StateMachine(bool fixedUpdate)
	{
		switch (currentState)
		{
		case State.Inactive:
			StateInactive(fixedUpdate);
			break;
		case State.Idle:
			StateIdle(fixedUpdate);
			break;
		case State.MoveForward:
			StateMoveForward(fixedUpdate);
			break;
		case State.TakeOff:
			StateTakeOff(fixedUpdate);
			break;
		case State.Flying:
			StateFlying(fixedUpdate);
			break;
		}
	}

	private void StateInactive(bool fixedUpdate)
	{
		if (!fixedUpdate && physGrabObject.grabbed)
		{
			UpdateState(State.Idle);
		}
	}

	private void StateIdle(bool fixedUpdate)
	{
		if (fixedUpdate || !stateImpulse)
		{
			return;
		}
		foreach (Collider planeCollider in planeColliders)
		{
			if (Object.op_Implicit((Object)(object)planeCollider))
			{
				planeCollider.material = defaultPhysicMaterial;
			}
		}
		stateImpulse = false;
	}

	private void StateMoveForward(bool fixedUpdate)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (!fixedUpdate || !playersNearby)
		{
			return;
		}
		if (stateImpulse)
		{
			stateTimer = 2f;
			foreach (Collider planeCollider in planeColliders)
			{
				if (Object.op_Implicit((Object)(object)planeCollider))
				{
					planeCollider.material = movingPhysicMaterial;
				}
			}
			stateImpulse = false;
		}
		RandomWiggle();
		ForwardPropulsion();
		Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
		if (linearVelocity.magnitude >= takeOffSpeed && Mathf.Abs(Vector3.Dot(((Component)this).transform.up, Vector3.up)) > 0.9f)
		{
			UpdateState(State.TakeOff);
		}
	}

	private void RandomWiggle()
	{
		stateTimer -= Time.fixedDeltaTime;
		if (stateTimer <= 0f)
		{
			stateTimer = 1f;
			if (Random.value > 0.5f)
			{
				TurnPlane();
			}
		}
	}

	private void ForwardPropulsion()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		float num = cruiseSpeed;
		Vector3 linearVelocity = physGrabObject.rb.linearVelocity;
		float num2 = num - linearVelocity.magnitude;
		physGrabObject.rb.AddForce(((Component)this).transform.forward * num2 * Time.fixedDeltaTime * 2f, (ForceMode)1);
	}

	private void StateTakeOff(bool fixedUpdate)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		if (!fixedUpdate || !playersNearby)
		{
			return;
		}
		if (stateImpulse)
		{
			foreach (Collider planeCollider in planeColliders)
			{
				if (Object.op_Implicit((Object)(object)planeCollider))
				{
					planeCollider.material = movingPhysicMaterial;
				}
			}
			stateImpulse = false;
		}
		PushForwardAndLift();
		PitchNoseUp();
		RaycastHit val = default;
		if (!Physics.Raycast(((Component)this).transform.position, Vector3.down, ref val, 1.5f))
		{
			UpdateState(State.Flying);
		}
	}

	private void PushForwardAndLift()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Clamp(Vector3.Dot(physGrabObject.rb.linearVelocity, ((Component)this).transform.forward) * liftPerSpeed, 0f, maxLiftForce);
		physGrabObject.rb.AddForce(((Component)this).transform.forward * 1.5f, (ForceMode)0);
		physGrabObject.rb.AddForce(Vector3.up * num, (ForceMode)0);
	}

	private void PitchNoseUp()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject.rb.AddTorque(Vector3.left * 0.2f, (ForceMode)0);
	}

	private void StateFlying(bool fixedUpdate)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		if (fixedUpdate && playersNearby)
		{
			RaycastHit val = default;
			if (!Physics.Raycast(((Component)this).transform.position, Vector3.down, ref val, 1.5f))
			{
				physGrabObject.OverrideZeroGravity();
			}
			float num = Vector3.Dot(physGrabObject.rb.linearVelocity, ((Component)this).transform.forward);
			ManageSpeedAndLift(num);
			Stabilise(levelStrength, levelDamping);
			if (num > takeOffSpeed)
			{
				stateTimer = 2f;
			}
			stateTimer -= Time.fixedDeltaTime;
			if ((num < takeOffSpeed * 0.8f && Physics.Raycast(((Component)this).transform.position, Vector3.down, 2f)) || stateTimer <= 0f)
			{
				UpdateState(State.Inactive);
			}
		}
	}

	private void ManageSpeedAndLift(float forwardSpeed)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (forwardSpeed < flightSpeed)
		{
			physGrabObject.rb.AddForce(((Component)this).transform.forward * (flightSpeed - forwardSpeed) * Time.fixedDeltaTime, (ForceMode)1);
		}
		RaycastHit val = default;
		if (Physics.Raycast(((Component)this).transform.position, Vector3.down, ref val, 1.5f))
		{
			float num = Mathf.Clamp(forwardSpeed * liftPerSpeed, 0f, maxLiftForce);
			physGrabObject.rb.AddForce(Vector3.up * num, (ForceMode)0);
		}
	}

	private void Stabilise(float kp, float kd)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		Rigidbody rb = physGrabObject.rb;
		Vector3 up = ((Component)this).transform.up;
		Vector3 up2 = Vector3.up;
		Vector3 val = Vector3.Cross(up, up2);
		float num = Vector3.SignedAngle(up, up2, val);
		num = Mathf.Clamp(num, 0f - bankLimitDeg, bankLimitDeg);
		Vector3 val2 = val.normalized * num * (MathF.PI / 180f) * kp - rb.angularVelocity * kd;
		rb.AddTorque(val2, (ForceMode)5);
	}

	private void UpdateState(State newState)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && newState != currentState)
		{
			stuckTime = 0f;
			currentState = newState;
			stateImpulse = true;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("UpdateStateRPC", (RpcTarget)0, new object[1] { (int)currentState });
			}
			else
			{
				UpdateStateRPC((int)currentState);
			}
		}
	}

	[PunRPC]
	private void UpdateStateRPC(int stateIndex, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			currentState = (State)stateIndex;
		}
	}

	private void TurnPlane()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject.rb.AddTorque(new Vector3(0f, Random.Range(-1f, 1f) * 0.1f, 0f), (ForceMode)1);
	}

	private void CheckForNearbyPlayers()
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		playersNearby = false;
		foreach (PlayerAvatar item in SemiFunc.PlayerGetList())
		{
			if (Vector3.Distance(((Component)item).transform.position, ((Component)this).transform.position) < 20f)
			{
				playersNearby = true;
				break;
			}
		}
	}

	private void StuckCheck()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Inactive && currentState != State.Idle)
		{
			Vector3 val = physGrabObject.rb.position - previousPosition;
			float magnitude = val.magnitude;
			previousPosition = physGrabObject.rb.position;
			if (magnitude >= 0.02f)
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

	public ValuablePlane()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
	}
}
