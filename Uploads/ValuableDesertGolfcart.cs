using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class ValuableDesertGolfcart : MonoBehaviour
{
	public enum State
	{
		Idle,
		Active
	}

	[HideInInspector]
	public State currentState;

	[HideInInspector]
	public float stateTimer;

	[HideInInspector]
	public float stateTimerMax;

	private bool stateStart;

	internal PhotonView photonView;

	internal PhysGrabObject physGrabObject;

	public Rigidbody rb;

	public SemiMounting mounting;

	public PhysGrabObjectImpactDetector impactDetector;

	public HurtCollider hurtCollider;

	public Transform seatTransform;

	public Transform steeringTransform;

	public Transform steeringWheelTransform;

	public float steeringWheelRotationMultiplier = 30f;

	public float steeringWheelAngleMax = 120f;

	public float steeringWheelSmoothSpeed = 5f;

	public Transform[] wheels;

	public Vector3 wheelSpinAxis = Vector3.right;

	public float wheelRadius = 0.3f;

	public float wheelRotationMultiplier = 1f;

	public float wheelSpeedSmoothSpeed = 8f;

	public float sidewaysGrip = 60f;

	public MineCartGrounded groundedTrigger;

	public float wheelGroundCheckDistance = 0.6f;

	public Sound audioEngineLoop;

	public Sound audioGallopLoop;

	public Sound audioNeigh;

	public float gallopSpeedMax = 15f;

	private float cartForwardSpeed = 10f;

	private float cartReverseSpeed = 6f;

	private float speedOverTime;

	private int throttleInput;

	private float currentSteeringAngle;

	private float targetSteeringAngle;

	private Quaternion steeringOriginalRotation;

	private SpringQuaternion steeringSpring;

	private Quaternion steeringWheelOriginalRotation;

	private float wheelForwardSpeedSmoothed;

	private Vector3 previousForward;

	private float fakeAngularVelocity;

	private float fakeAngularVelocitySmoothed;

	private float steeringWheelAngle;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		if (!Object.op_Implicit((Object)(object)rb))
		{
			rb = ((Component)this).GetComponent<Rigidbody>();
		}
		rb.maxLinearVelocity = 13.333333f;
		if (!Object.op_Implicit((Object)(object)mounting))
		{
			mounting = ((Component)this).GetComponent<SemiMounting>();
		}
		mounting.allowInputWhileMounting = true;
		if (!Object.op_Implicit((Object)(object)impactDetector))
		{
			impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		}
	}

	private void Start()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected Obj, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		SetState(State.Idle);
		impactDetector.onAllBreaks.AddListener((UnityAction)NeighOnBreak);
		if (Object.op_Implicit((Object)(object)steeringTransform))
		{
			steeringOriginalRotation = steeringTransform.localRotation;
			steeringSpring = new SpringQuaternion();
			steeringSpring.speed = 10f;
			steeringSpring.damping = 0.6f;
			steeringSpring.maxAngle = 80f;
			steeringSpring.clamp = true;
		}
		if (Object.op_Implicit((Object)(object)steeringWheelTransform))
		{
			steeringWheelOriginalRotation = steeringWheelTransform.localRotation;
		}
		previousForward = seatTransform.forward;
		previousForward.y = 0f;
	}

	private void Update()
	{
		StateMachine();
		ThrottleInputLogic();
		HurtColliderLogic();
		EngineAudioLogic();
		UpdateFakeAngularVelocity();
		UpdateSteering();
		WheelRotationLogic();
		GallopAudioLogic();
	}

	private void FixedUpdate()
	{
		WheelGripLogic();
		if (currentState == State.Active)
		{
			FixedUpdateActive();
		}
	}

	private void StateMachine()
	{
		if (Object.op_Implicit((Object)(object)Driver()))
		{
			SetState(State.Active);
		}
		else
		{
			SetState(State.Idle);
		}
		switch (currentState)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Active:
			StateActive();
			break;
		}
	}

	private void StateIdle()
	{
		if (stateStart)
		{
			stateTimerMax = 999f;
			stateTimer = 0f;
			throttleInput = 0;
			speedOverTime = 0f;
			stateStart = false;
		}
	}

	private void StateActive()
	{
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateTimerMax = 999f;
			stateTimer = 0f;
			stateStart = false;
		}
		if (stateTimer <= stateTimerMax)
		{
			stateTimer += Time.deltaTime;
		}
		if (throttleInput > 0)
		{
			speedOverTime += Time.deltaTime * 0.5f;
			speedOverTime = Mathf.Clamp(speedOverTime, 0f, 10f);
		}
		else
		{
			speedOverTime = 0f;
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude <= 1f)
		{
			speedOverTime = 0f;
		}
	}

	private void FixedUpdateActive()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !Object.op_Implicit((Object)(object)rb))
		{
			return;
		}
		PlayerAvatar playerAvatar = Driver();
		if (Object.op_Implicit((Object)(object)playerAvatar))
		{
			Vector3 forward = playerAvatar.localCamera.GetOverrideTransform().forward;
			if (throttleInput > 0)
			{
				Vector3 val = forward * cartForwardSpeed;
				rb.AddForce(val * (1f + speedOverTime), (ForceMode)5);
			}
			if (throttleInput < 0)
			{
				Vector3 val2 = -forward * cartReverseSpeed;
				rb.AddForce(val2, (ForceMode)5);
			}
			Quaternion val3 = Quaternion.Euler(0f, -90f, 0f);
			Quaternion val4 = playerAvatar.localCamera.GetOverrideTransform().rotation * val3;
			Quaternion targetRotation = Quaternion.Euler(0f, val4.eulerAngles.y, 0f);
			Vector3 val5 = SemiFunc.PhysFollowRotation(((Component)rb).transform, targetRotation, rb, 20f);
			rb.AddTorque(val5 * 5f, (ForceMode)0);
			Vector3 val6 = rb.angularVelocity * -5f;
			rb.AddTorque(val6, (ForceMode)0);
		}
	}

	private void WheelGripLogic()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb) && !rb.IsSleeping() && WheelsGrounded())
		{
			Vector3 val = seatTransform.right;
			val.y = 0f;
			val = val.normalized;
			float num = Vector3.Dot(rb.linearVelocity, val);
			if (!(Mathf.Abs(num) <= 0.01f))
			{
				float num2 = 1f - Mathf.Exp((0f - sidewaysGrip) * Time.fixedDeltaTime);
				rb.AddForce(val * (0f - num) * num2, (ForceMode)2);
			}
		}
	}

	private bool WheelsGrounded()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)groundedTrigger))
		{
			return groundedTrigger.Grounded;
		}
		return SemiFunc.OnGroundCheck(((Component)this).transform.position, wheelGroundCheckDistance, physGrabObject);
	}

	private PlayerAvatar Driver()
	{
		if (!Object.op_Implicit((Object)(object)mounting))
		{
			return null;
		}
		PlayerAvatar playerAvatar = mounting.Driver();
		if (!Object.op_Implicit((Object)(object)playerAvatar) || !playerAvatar.physGrabber.grabbed || (Object)(object)playerAvatar.physGrabber.grabbedPhysGrabObject != (Object)(object)physGrabObject)
		{
			return null;
		}
		return playerAvatar;
	}

	private void ThrottleInputLogic()
	{
		if (currentState != State.Active)
		{
			return;
		}
		PlayerAvatar playerAvatar = Driver();
		if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.isLocal)
		{
			float num = 0f;
			if (InputManager.instance.VehicleInputAllowed())
			{
				num = SemiFunc.InputMovementY();
			}
			int throttle = 0;
			if (num > 0.1f)
			{
				throttle = 1;
			}
			if (num < -0.1f)
			{
				throttle = -1;
			}
			SetThrottle(throttle);
		}
	}

	private void HurtColliderLogic()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)hurtCollider))
		{
			return;
		}
		if (currentState != State.Active)
		{
			if (((Component)hurtCollider).gameObject.activeSelf)
			{
				((Component)hurtCollider).gameObject.SetActive(false);
			}
			return;
		}
		bool flag = Vector3.Dot(rb.linearVelocity, seatTransform.forward) > 5f;
		if (((Component)hurtCollider).gameObject.activeSelf != flag)
		{
			((Component)hurtCollider).gameObject.SetActive(flag);
		}
	}

	private void EngineAudioLogic()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = mounting.VelocityGet();
		val.y = 0f;
		float num = val.magnitude * 0.5f;
		num = Mathf.Clamp(num, 1.5f, 5f);
		audioEngineLoop.PlayLoop(currentState == State.Active && mounting.MotorSoundCanPlay(), 2f, 2f, num);
	}

	private void GallopAudioLogic()
	{
		float num = Mathf.Clamp01(Mathf.Abs(wheelForwardSpeedSmoothed) / gallopSpeedMax);
		float pitchMultiplier = Mathf.Lerp(0.8f, 1.4f, num);
		audioGallopLoop.PlayLoop(currentState == State.Active && mounting.MotorSoundCanPlay(), 2f, 2f, pitchMultiplier, num);
	}

	private void NeighOnBreak()
	{
		audioNeigh.Play(((Component)rb).transform);
	}

	private void UpdateSteering()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = Driver();
		if (currentState == State.Active && Object.op_Implicit((Object)(object)playerAvatar))
		{
			Vector3 forward = playerAvatar.localCamera.GetOverrideTransform().forward;
			float num = Vector3.SignedAngle(seatTransform.forward, forward, Vector3.up) * 5f;
			targetSteeringAngle = Mathf.Clamp(num, -65f, 65f);
		}
		else
		{
			targetSteeringAngle = 0f;
		}
		currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, Time.deltaTime * 8f);
		UpdateSteeringColumn();
		UpdateSteeringWheel();
	}

	private void UpdateSteeringColumn()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)steeringTransform))
		{
			Quaternion targetRotation = Quaternion.Euler(0f, currentSteeringAngle, 0f);
			Quaternion val = SemiFunc.SpringQuaternionGet(steeringSpring, targetRotation);
			steeringTransform.localRotation = steeringOriginalRotation * val;
		}
	}

	private void UpdateFakeAngularVelocity()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.PerSecond(30f, (Object)(object)this))
		{
			Vector3 forward = seatTransform.forward;
			forward.y = 0f;
			fakeAngularVelocity = Vector3.SignedAngle(previousForward, forward, Vector3.up);
			previousForward = forward;
		}
	}

	private void UpdateSteeringWheel()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)steeringWheelTransform))
		{
			fakeAngularVelocitySmoothed = Mathf.Lerp(fakeAngularVelocitySmoothed, fakeAngularVelocity, Time.deltaTime * steeringWheelSmoothSpeed);
			steeringWheelAngle = Mathf.Clamp(fakeAngularVelocitySmoothed * steeringWheelRotationMultiplier, 0f - steeringWheelAngleMax, steeringWheelAngleMax);
			Quaternion val = Quaternion.Euler(0f, 0f, steeringWheelAngle);
			steeringWheelTransform.localRotation = steeringWheelOriginalRotation * val;
		}
	}

	private void WheelRotationLogic()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (wheels == null || wheels.Length == 0)
		{
			return;
		}
		float num = MathF.PI * 2f * wheelRadius;
		if (!(num <= 0f))
		{
			float num2 = Vector3.Dot(mounting.VelocityGet(), seatTransform.forward);
			float num3 = 1f - Mathf.Exp((0f - wheelSpeedSmoothSpeed) * Time.deltaTime);
			wheelForwardSpeedSmoothed = Mathf.Lerp(wheelForwardSpeedSmoothed, num2, num3);
			float num4 = (0f - wheelForwardSpeedSmoothed) / num * 360f * wheelRotationMultiplier * Time.deltaTime;
			Transform[] array = wheels;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Rotate(wheelSpinAxis, num4, (Space)1);
			}
		}
	}

	private void SetThrottle(int newThrottle)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (newThrottle != throttleInput)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetThrottleRPC", (RpcTarget)0, new object[1] { newThrottle });
			}
			else
			{
				SetThrottleRPC(newThrottle);
			}
		}
	}

	[PunRPC]
	private void SetThrottleRPC(int newThrottle, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = Driver();
		if (Object.op_Implicit((Object)(object)playerAvatar) && SemiFunc.OwnerOnlyRPC(info, playerAvatar.photonView) && newThrottle >= -1 && newThrottle <= 1)
		{
			throttleInput = newThrottle;
		}
	}

	private void SetState(State newState)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (newState != currentState && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetStateRPC", (RpcTarget)0, new object[1] { (int)newState });
			}
			else
			{
				SetStateRPC((int)newState);
			}
		}
	}

	[PunRPC]
	private void SetStateRPC(int newStateInt, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			stateStart = true;
			currentState = (State)newStateInt;
		}
	}

	public ValuableDesertGolfcart()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
	}
}
