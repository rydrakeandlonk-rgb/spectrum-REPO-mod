using Photon.Pun;
using UnityEngine;

public class ValuableArcticSnowBike : MonoBehaviour
{
	public enum State
	{
		Idle,
		Active
	}

	public State currentState;

	public float stateTimer;

	public float stateTimerMax;

	private bool stateStart;

	private PhysGrabObject physGrabObject;

	internal PhotonView photonView;

	public Rigidbody rb;

	public SemiMounting mounting;

	public Transform seatTransform;

	public Transform steeringTransform;

	public Sound audioEngineLoop;

	[Space]
	public float bikeForwardSpeed = 6f;

	public float bikeReverseSpeed = 4f;

	public float bikeSpeedMax = 8f;

	public float bikeReverseSpeedMax = 3f;

	public float bikeDeceleration = 3f;

	public float bikeBrakeStrength = 12f;

	public float bikeGripStrength = 8f;

	public ItemVehicle.VehicleGamepad gamepad = new ItemVehicle.VehicleGamepad();

	public float bikeBalanceStrength = 80f;

	public float bikeBalanceDamping = 5f;

	[Range(0f, 0.75f)]
	public float bikeLiftAmount = 0.25f;

	private PlayerAvatar previousDriver;

	private int throttleInput;

	private int steeringInput;

	private int sentThrottleInput;

	private int sentSteeringInput;

	private float driveYaw;

	private float gamepadYaw;

	private float driveInputSendTimer;

	private float currentSteeringAngle;

	private float targetSteeringAngle;

	private Quaternion steeringOriginalRotation;

	private SpringQuaternion steeringSpring;

	public HurtCollider hurtCollider;

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		stateStart = true;
		if (!Object.op_Implicit((Object)(object)rb))
		{
			rb = ((Component)this).GetComponent<Rigidbody>();
		}
		if (!Object.op_Implicit((Object)(object)mounting))
		{
			mounting = ((Component)this).GetComponent<SemiMounting>();
		}
	}

	private void Start()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		SetState(State.Idle);
		if (Object.op_Implicit((Object)(object)steeringTransform))
		{
			steeringOriginalRotation = steeringTransform.localRotation;
			steeringSpring = new SpringQuaternion();
			steeringSpring.speed = 10f;
			steeringSpring.damping = 0.6f;
			steeringSpring.maxAngle = 80f;
			steeringSpring.clamp = true;
		}
	}

	private void Update()
	{
		if (stateTimer <= stateTimerMax)
		{
			stateTimer += Time.deltaTime;
		}
		StateMachine();
		DriveInputLogic();
		DriveHeadingUpdate();
		HurtColliderLogic();
		EngineAudioLogic();
		UpdateSteering();
	}

	private void OnDisable()
	{
		currentState = State.Idle;
		stateStart = true;
		DriveReset();
		previousDriver = null;
		if (audioEngineLoop != null && Object.op_Implicit((Object)(object)audioEngineLoop.Source))
		{
			audioEngineLoop.Stop();
		}
		if (Object.op_Implicit((Object)(object)hurtCollider))
		{
			((Component)hurtCollider).gameObject.SetActive(false);
		}
	}

	private void FixedUpdate()
	{
		if (currentState == State.Active)
		{
			FixedUpdateActive();
		}
	}

	private void StateMachine()
	{
		PlayerAvatar playerAvatar = Driver();
		DriverRefresh(playerAvatar);
		if (Object.op_Implicit((Object)(object)playerAvatar))
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
			stateStart = false;
		}
	}

	private void StateActive()
	{
		if (stateStart)
		{
			stateTimerMax = 999f;
			stateTimer = 0f;
			stateStart = false;
		}
		if (Object.op_Implicit((Object)(object)Driver()))
		{
			physGrabObject.OverrideMaterial(SemiFunc.PhysicMaterialSlipperyPlus());
		}
	}

	private void FixedUpdateActive()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb) && Object.op_Implicit((Object)(object)Driver()))
		{
			BalancePhysics();
			if (mounting.SeatFullyMounted(0))
			{
				DrivePhysics();
				Quaternion targetRotation = Quaternion.Euler(0f, driveYaw - 90f, 0f);
				Vector3 val = SemiFunc.PhysFollowRotation(((Component)rb).transform, targetRotation, rb, 20f);
				rb.AddTorque(Vector3.up * Vector3.Dot(val, Vector3.up) * 5f, (ForceMode)0);
				Vector3 val2 = -Physics.gravity * rb.mass * Mathf.Clamp(bikeLiftAmount, 0f, 0.75f);
				rb.AddForce(val2, (ForceMode)0);
			}
		}
	}

	private void BalancePhysics()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.Cross(((Component)this).transform.up, Vector3.up);
		rb.AddTorque(val * bikeBalanceStrength, (ForceMode)0);
		rb.AddTorque(-rb.angularVelocity * bikeBalanceDamping, (ForceMode)0);
	}

	private void DrivePhysics()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.ProjectOnPlane(seatTransform.forward, Vector3.up);
		Vector3 normalized = val.normalized;
		Vector3 val2 = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
		float num = Vector3.Dot(val2, normalized);
		float num2 = (float)throttleInput * 0.01f;
		float num3 = num2 * ((num2 > 0f) ? bikeSpeedMax : bikeReverseSpeedMax);
		float num4 = ((num2 > 0f) ? bikeForwardSpeed : bikeReverseSpeed);
		if (Mathf.Abs(num2) < 0.01f)
		{
			num4 = bikeDeceleration;
		}
		else if (num * num2 < 0f)
		{
			num4 = bikeBrakeStrength;
		}
		float num5 = Mathf.MoveTowards(num, num3, Mathf.Max(0f, num4) * Time.fixedDeltaTime);
		rb.AddForce(normalized * ((num5 - num) / Time.fixedDeltaTime), (ForceMode)5);
		Vector3 val3 = val2 - normalized * num;
		float num6 = 1f - Mathf.Exp((0f - Mathf.Max(0f, bikeGripStrength)) * Time.fixedDeltaTime);
		rb.AddForce(-val3 * (num6 / Time.fixedDeltaTime), (ForceMode)5);
	}

	private void UpdateSteering()
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)steeringTransform))
		{
			PlayerAvatar playerAvatar = Driver();
			if (currentState == State.Active && Object.op_Implicit((Object)(object)playerAvatar))
			{
				Vector3 val = Quaternion.Euler(0f, driveYaw, 0f) * Vector3.forward;
				float num = Vector3.SignedAngle(seatTransform.forward, val, Vector3.up) * 5f;
				targetSteeringAngle = Mathf.Clamp(num, -65f, 65f);
			}
			else
			{
				targetSteeringAngle = 0f;
			}
			currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, Time.deltaTime * 8f);
			Quaternion targetRotation = Quaternion.Euler(0f, currentSteeringAngle, 0f);
			Quaternion val2 = SemiFunc.SpringQuaternionGet(steeringSpring, targetRotation);
			steeringTransform.localRotation = steeringOriginalRotation * val2;
		}
	}

	private PlayerAvatar Driver()
	{
		if (!Object.op_Implicit((Object)(object)mounting) || !((Behaviour)mounting).isActiveAndEnabled)
		{
			return null;
		}
		PlayerAvatar playerAvatar = mounting.Driver();
		if (!Object.op_Implicit((Object)(object)playerAvatar) || playerAvatar.isDisabled)
		{
			return null;
		}
		return playerAvatar;
	}

	private void DriverRefresh(PlayerAvatar driver)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)driver == (Object)(object)previousDriver))
		{
			DriveReset();
			previousDriver = driver;
			if (Object.op_Implicit((Object)(object)driver))
			{
				driveYaw = driver.localCamera.GetOverrideTransform().eulerAngles.y;
				gamepadYaw = YawGet(seatTransform.forward);
			}
		}
	}

	private void DriveReset()
	{
		throttleInput = 0;
		steeringInput = 0;
		sentThrottleInput = 0;
		sentSteeringInput = 0;
		driveInputSendTimer = 0f;
		gamepad.InputReset();
	}

	private void DriveInputLogic()
	{
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = Driver();
		if (!Object.op_Implicit((Object)(object)playerAvatar) || !playerAvatar.isLocal || !Object.op_Implicit((Object)(object)InputManager.instance))
		{
			return;
		}
		InputManager instance = InputManager.instance;
		float num = 0f;
		float num2 = 0f;
		if (instance.CurrentMode == InputModes.Gamepad)
		{
			instance.OverrideVehicleGamepad();
			instance.VehicleGamepadInputUpdate(gamepad, allowDrifting: false);
			num = gamepad.throttle;
		}
		else
		{
			gamepad.InputReset();
			num = instance.VehicleThrottleGet();
			num2 = instance.VehicleSteeringGet();
		}
		if (!mounting.SeatFullyMounted(0))
		{
			num = 0f;
			num2 = 0f;
		}
		int num3 = Mathf.RoundToInt(num * 100f);
		int num4 = Mathf.RoundToInt(num2 * 100f);
		driveInputSendTimer = Mathf.Max(0f, driveInputSendTimer - Time.deltaTime);
		if ((num3 != sentThrottleInput || num4 != sentSteeringInput) && (!(driveInputSendTimer > 0f) || (num3 == 0 && num4 == 0)))
		{
			driveInputSendTimer = 0.05f;
			sentThrottleInput = num3;
			sentSteeringInput = num4;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("RequestDriveInputRPC", (RpcTarget)2, new object[3]
				{
					playerAvatar.photonView.ViewID,
					num3,
					num4
				});
			}
			else
			{
				RequestDriveInputRPC(playerAvatar.photonView.ViewID, num3, num4);
			}
		}
	}

	private void DriveHeadingUpdate()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = Driver();
		if (Object.op_Implicit((Object)(object)playerAvatar))
		{
			float y = playerAvatar.localCamera.GetOverrideTransform().eulerAngles.y;
			if (playerAvatar.isLocal && gamepad.currentState != ItemVehicle.VehicleGamepad.State.Keyboard)
			{
				GamepadHeadingUpdate(playerAvatar);
				return;
			}
			driveYaw = y + (float)steeringInput * 0.45f;
			gamepadYaw = driveYaw;
		}
	}

	private void GamepadHeadingUpdate(PlayerAvatar driver)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		if (mounting.SeatFullyMounted(0) && InputManager.instance.VehicleGamepadLookAllowed())
		{
			float vehicleYaw = YawGet(seatTransform.forward);
			Vector3 val = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
			float magnitude = val.magnitude;
			gamepadYaw = gamepad.SteeringYawGet(gamepadYaw, vehicleYaw, magnitude / Mathf.Max(bikeSpeedMax, 1f), Time.deltaTime);
			driveYaw = gamepadYaw;
			if (Object.op_Implicit((Object)(object)CameraAim.Instance))
			{
				Vector3 val2 = Quaternion.Euler(gamepad.CameraPitchGet(InputManager.instance.VehicleGamepadLookGet(), GameplayManager.instance.aimInvertVertical), gamepadYaw, 0f) * Vector3.forward;
				Vector3 position = driver.localCamera.GetOverrideTransform().position;
				CameraAim.Instance.AimTargetSoftSet(position + val2 * 10f, 0.1f, 12f, 12f, ((Component)this).gameObject, 50);
			}
		}
	}

	private float YawGet(Vector3 direction)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return Mathf.Atan2(direction.x, direction.z) * 57.29578f;
	}

	private void HurtColliderLogic()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)hurtCollider))
		{
			bool flag = currentState == State.Active && Object.op_Implicit((Object)(object)Driver()) && Vector3.Dot(rb.linearVelocity, seatTransform.forward) > 5f;
			if (((Component)hurtCollider).gameObject.activeSelf != flag)
			{
				((Component)hurtCollider).gameObject.SetActive(flag);
			}
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
		audioEngineLoop.PlayLoop(currentState == State.Active && Object.op_Implicit((Object)(object)Driver()) && mounting.MotorSoundCanPlay(), 2f, 2f, num);
	}

	[PunRPC]
	private void RequestDriveInputRPC(int playerPhotonID, int throttle, int steering, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		PlayerAvatar playerAvatar = Driver();
		if (!Object.op_Implicit((Object)(object)playerAvatar) || playerAvatar.photonView.ViewID != playerPhotonID || !SemiFunc.OwnerOnlyRPC(info, playerAvatar.photonView) || throttle < -100 || throttle > 100 || steering < -100 || steering > 100)
		{
			return;
		}
		DriverRefresh(playerAvatar);
		if (throttle != throttleInput || steering != steeringInput)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetDriveInputRPC", (RpcTarget)0, new object[3] { playerPhotonID, throttle, steering });
			}
			else
			{
				SetDriveInputRPC(playerPhotonID, throttle, steering);
			}
		}
	}

	[PunRPC]
	private void SetDriveInputRPC(int playerPhotonID, int throttle, int steering, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info))
		{
			PlayerAvatar playerAvatar = Driver();
			if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.photonView.ViewID == playerPhotonID && throttle >= -100 && throttle <= 100 && steering >= -100 && steering <= 100)
			{
				DriverRefresh(playerAvatar);
				throttleInput = throttle;
				steeringInput = steering;
			}
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
}
