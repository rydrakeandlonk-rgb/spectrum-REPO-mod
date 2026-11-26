using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ItemVehicle : MonoBehaviour
{
	[Serializable]
	public class VehicleSeat
	{
		public Transform seatTransform;

		public Transform firstMountTransform;

		public Transform grabPoint;

		public Transform grabPointVisual;

		public float followForce = 20f;

		public float downSeatForce = 0.2f;

		public float playerMass = 1f;

		public float addStabilizationForce;

		[HideInInspector]
		public PlayerAvatar seatedPlayer;

		[HideInInspector]
		public float grabbedTimer;

		[HideInInspector]
		public float takeASeatTimer;

		[HideInInspector]
		public bool reachedFirstMount;

		[NonSerialized]
		public float mountFinishTimer;

		[NonSerialized]
		public float mountSettleTimer;
	}

	[Serializable]
	public class DestructionState
	{
		public GameObject[] meshesToActivate;
	}

	[Serializable]
	public class DriftLightSettings
	{
		public Color color = Color.white;

		public float intensity = 2f;

		public float flickerAmount = 0.5f;

		public float flickerSpeed = 20f;

		public float pulseIntensity;

		public float pulseSpeed = 5f;

		public DriftLightSettings()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		}
	}

	[Serializable]
	public class DriftLevelSettings
	{
		public float rollAngle;

		public float yawOffset;

		public float meshHorizontalOffset;

		public float meshYRotationOffset;

		public float meshDownOffset;

		public float meshForwardOffset;
	}

	public enum ImpactType
	{
		None,
		Small,
		Medium,
		Big
	}

	[Serializable]
	public class VehicleGamepad
	{
		public enum State
		{
			Keyboard,
			Driving,
			DriftLeft,
			DriftRight
		}

		[Range(0f, 0.5f)]
		public float stickDeadzone;

		[Range(0f, 1f)]
		public float steeringCurve = 0.15f;

		[Min(1f)]
		public float turnSpeed = 220f;

		[Min(1f)]
		public float highSpeedTurnSpeed = 160f;

		[Min(0.01f)]
		public float driftTurnAccelerationTime = 2f;

		[Min(0.1f)]
		public float driftCounterSteerSoftAngle = 20f;

		[Range(0f, 75f)]
		public float driftCounterSteerAngle = 40f;

		private float driftTurnAccelerationTimer;

		private float previousTurnDirection;

		public State currentState { get; private set; }

		public float steer { get; private set; }

		public float throttle { get; private set; }

		public float DriftDirection
		{
			get
			{
				if (currentState != State.DriftLeft)
				{
					if (currentState != State.DriftRight)
					{
						return 0f;
					}
					return 1f;
				}
				return -1f;
			}
		}

		public void InputUpdate(Vector2 stick, float acceleration, float brake, bool leftShoulder, bool rightShoulder)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			float num = Mathf.Abs(stick.x);
			float num2 = Mathf.InverseLerp(stickDeadzone, 1f, num);
			float num3 = Mathf.Sign(stick.x);
			float num4 = num2 * num3;
			float num5 = Mathf.Clamp01(acceleration);
			float num6 = Mathf.Clamp01(brake);
			float num7 = num5 - num6;
			State state = State.Driving;
			if (leftShoulder & rightShoulder)
			{
				if (DriftDirection != 0f)
				{
					state = currentState;
				}
				else if (Mathf.Abs(num4) > 0.1f)
				{
					state = ((num4 < 0f) ? State.DriftLeft : State.DriftRight);
				}
			}
			else if (leftShoulder)
			{
				state = State.DriftLeft;
			}
			else if (rightShoulder)
			{
				state = State.DriftRight;
			}
			currentState = state;
			steer = num4;
			throttle = num7;
		}

		public void InputReset()
		{
			currentState = State.Keyboard;
			steer = 0f;
			throttle = 0f;
		}

		public float SteeringGet()
		{
			float num = steer * steer * steer;
			return Mathf.Lerp(steer, num, steeringCurve);
		}

		public void CameraTurnReset()
		{
			driftTurnAccelerationTimer = 0f;
			previousTurnDirection = 0f;
		}

		public float CameraTurnSpeedUpdate(bool drifting, float deltaTime)
		{
			if (!drifting || steer == 0f)
			{
				CameraTurnReset();
				return 1f;
			}
			float num = Mathf.Sign(steer);
			if (num != previousTurnDirection)
			{
				driftTurnAccelerationTimer = 0f;
			}
			previousTurnDirection = num;
			float num2 = Mathf.Max(driftTurnAccelerationTime, 0.01f);
			float num3 = driftTurnAccelerationTimer / num2;
			float result = Mathf.Lerp(1f, 2.2f, num3);
			float num4 = Mathf.Max(deltaTime, 0f);
			driftTurnAccelerationTimer = Mathf.Min(num2, driftTurnAccelerationTimer + num4);
			return result;
		}

		public float SteeringYawGet(float yaw, float vehicleYaw, float speedRatio, float deltaTime, float turnSpeedMultiplier = 1f)
		{
			float num = SteeringGet();
			float num2 = Mathf.Clamp01(speedRatio);
			float num3 = Mathf.Lerp(turnSpeed, highSpeedTurnSpeed, num2);
			float num4 = num * num3 * turnSpeedMultiplier * deltaTime;
			float num5 = yaw + num4;
			float num6 = Mathf.Clamp(Mathf.DeltaAngle(vehicleYaw, num5), -75f, 75f);
			return vehicleYaw + num6;
		}

		public float DriftCameraYawGet(float yaw, float targetYaw, float vehicleYaw, float yawOffset, float deltaTime)
		{
			if (Mathf.Abs(yawOffset) < 0.001f)
			{
				return targetYaw;
			}
			float num = 0f - Mathf.Sign(yawOffset);
			float num2 = Mathf.DeltaAngle(vehicleYaw, yaw) * num;
			float num3 = Mathf.DeltaAngle(vehicleYaw, targetYaw) * num;
			float num4 = Mathf.Clamp(driftCounterSteerAngle, 0f, 75f);
			if (num2 > num4)
			{
				float num5 = 1f - Mathf.Exp(-12f * deltaTime);
				float num6 = Mathf.Lerp(num2, num4, num5);
				num3 = Mathf.Min(num3, num6);
			}
			else if (num3 > num2)
			{
				float num7 = num3 - num2;
				float num8 = num4 - num2;
				float num9 = Mathf.Max(driftCounterSteerSoftAngle, 0.1f);
				float num10 = 1f - Mathf.Exp((0f - num7) / num9);
				float num11 = num8 * num10;
				num3 = num2 + Mathf.Min(num7, num11);
			}
			float num12 = num3 * num;
			return vehicleYaw + num12;
		}

		public float CameraPitchGet(float look, bool inverted)
		{
			float num = Mathf.Clamp(look, -1f, 1f);
			float num2 = (inverted ? 20f : (-20f));
			return num * num2;
		}
	}

	public enum State
	{
		Idle,
		Active,
		Tumbled
	}

	private struct CachedComponents
	{
		public ItemVehicle vehicle;

		public Enemy enemy;

		public EnemyRigidbody enemyRigidbody;

		public ValuableObject valuable;

		public PlayerTumble playerTumble;

		public PlayerAvatar playerAvatar;

		public PlayerController playerController;

		public bool initialized;
	}

	public State currentState;

	public float stateTimer;

	public float stateTimerMax;

	private bool stateStart;

	public Transform steeringTransform;

	public Rigidbody rb;

	public HurtCollider hurtColliderSmall;

	public HurtCollider hurtColliderMedium;

	public HurtCollider hurtColliderBig;

	public Transform enemyImpactBoxTransform;

	public Transform bumperCarBoxTransform;

	private PhysGrabObjectGrabArea grabArea;

	public VehicleSeat[] seats;

	public float forceMultiplier = 1f;

	public float maxSpeedKmh = 100f;

	public float softMaxSpeedKmh = 60f;

	public float softMaxSpeedDecayRate = 5f;

	public float maxRBVelocity = 30f;

	public float maxRBAngularVelocity = 10f;

	private float seatedLimitsTimer;

	public float minDownwardVelocity = 5f;

	public float maxDownwardVelocity = 20f;

	public float downwardVelocityMinSpeed = 2f;

	public float downwardVelocityMaxSpeed = 15f;

	public float stabilizationForce = 80f;

	public float angularDrag = 5f;

	public float maxTiltAngle = 75f;

	public float tiltCorrectionTorque = 50f;

	public float liftForce = 45f;

	public float boostLiftForceMultiplier = 2f;

	public float playerLiftForce = 10f;

	public float turnMultiplier = 6f;

	public float maxTurnForce = 10f;

	public float forwardLiftMultiplier = 0.5f;

	public float forwardTiltTorque = 2f;

	public float maxForwardLift = 10f;

	public float vehicleSeatDownForce = 100f;

	public float rollTowardSeatForce = 10f;

	public Collider[] vehicleColliders;

	public float maxForwardSpeed = 10f;

	public float maxReverseSpeed = 5f;

	public float forwardAcceleration = 0.5f;

	public float reverseAcceleration = 0.3f;

	public float deceleration = 0.05f;

	public float hyperAcceleration = 0.3f;

	public float hyperMaxSpeed = 20f;

	public bool isRacingVehicle;

	public float speedWhenNotInARace = 0.5f;

	public float airborneTargetPitch = -10f;

	public float airbornePitchSpeed = 5f;

	public float airbornePitchStrength = 20f;

	public float airborneBlendSpeed = 3f;

	public float airborneSteeringMultiplier = 0.7f;

	public float fallGravityAcceleration = 15f;

	public float fallGravityMax = 60f;

	public float fallGravityMinDownwardVelocity = 1f;

	public float fallGravityTopSpeed = 61f;

	public float fallGravityEaseInDuration = 2f;

	public float fallDamageThresholdPercent = 0.8f;

	public float fallDamageTumbleDuration = 3f;

	public HurtCollider fallDamageHurtCollider;

	public bool enableRampBoost = true;

	public float rampBoostMinSlopeAngle = 10f;

	public float rampBoostMaxSlopeAngle = 45f;

	public float rampBoostForce = 5f;

	public float rampBoostMinSpeed = 3f;

	public float steeringSpeed = 10f;

	public float steeringDamping = 0.6f;

	public float maxSteeringAngle = 65f;

	public float velocityRedirectStrength = 0.7f;

	public float manualTurnExtraAngle = 45f;

	public float yawSpringStrength = 50f;

	public float yawSpringDamping = 8f;

	public float yawAngularDrag = 8f;

	public bool hasDrifting;

	public float driftSpeedThreshold = 8f;

	public float driftAngleThreshold = 25f;

	public float driftMinDuration = 0.5f;

	public float driftForce = 20f;

	public float driftYawBoost = 1.2f;

	public float driftRedirectMultiplier = 0.3f;

	public float driftTurningSpeed = 5f;

	public float driftMaxSpeed = 12f;

	public float driftMinSpeedRatio = 0.3f;

	public float driftSpeedRampUpTime = 3f;

	public float driftExitBoost = 5f;

	public float driftDrag = 2f;

	public float driftDragDefault = 1f;

	public float driftDragSmoothTime = 0.1f;

	public float driftLowSpeedDrag = 40f;

	public float driftLowSpeedEaseDuration = 0.02f;

	public float driftLowSpeedStartDragMultiplier = 2f;

	public Transform driftingParticlesTransform;

	public ParticleSystem driftParticles;

	public ParticleSystem driftParticlesLevel1;

	public ParticleSystem driftParticlesLevel2;

	public ParticleSystem driftParticlesLevel3;

	public ParticleSystem driftBoostLevel0ReachedParticles;

	public ParticleSystem driftBoostLevel1ReachedParticles;

	public ParticleSystem driftBoostLevel2ReachedParticles;

	public ParticleSystem driftBoostLevel3ReachedParticles;

	public ParticleSystem driftBoostLevel1Particles;

	public ParticleSystem driftBoostLevel2Particles;

	public ParticleSystem driftBoostLevel3Particles;

	public Light driftLight;

	public DriftLightSettings[] driftLightSettings = new DriftLightSettings[4]
	{
		new DriftLightSettings
		{
			color = Color.white,
			intensity = 2f,
			flickerAmount = 0.5f,
			flickerSpeed = 20f,
			pulseIntensity = 0f,
			pulseSpeed = 5f
		},
		new DriftLightSettings
		{
			color = Color.yellow,
			intensity = 3f,
			flickerAmount = 0.6f,
			flickerSpeed = 25f,
			pulseIntensity = 0.3f,
			pulseSpeed = 6f
		},
		new DriftLightSettings
		{
			color = new Color(1f, 0.5f, 0f),
			intensity = 4f,
			flickerAmount = 0.7f,
			flickerSpeed = 30f,
			pulseIntensity = 0.5f,
			pulseSpeed = 8f
		},
		new DriftLightSettings
		{
			color = Color.red,
			intensity = 5f,
			flickerAmount = 0.8f,
			flickerSpeed = 35f,
			pulseIntensity = 0.7f,
			pulseSpeed = 10f
		}
	};

	public DriftLevelSettings[] driftLevelSettings = new DriftLevelSettings[4]
	{
		new DriftLevelSettings
		{
			rollAngle = 45f,
			yawOffset = 30f,
			meshHorizontalOffset = 0.3f,
			meshYRotationOffset = 10f,
			meshDownOffset = 0.3f,
			meshForwardOffset = 0.3f
		},
		new DriftLevelSettings
		{
			rollAngle = 50f,
			yawOffset = 35f,
			meshHorizontalOffset = 0.35f,
			meshYRotationOffset = 12f,
			meshDownOffset = 0.35f,
			meshForwardOffset = 0.35f
		},
		new DriftLevelSettings
		{
			rollAngle = 55f,
			yawOffset = 40f,
			meshHorizontalOffset = 0.4f,
			meshYRotationOffset = 14f,
			meshDownOffset = 0.4f,
			meshForwardOffset = 0.4f
		},
		new DriftLevelSettings
		{
			rollAngle = 60f,
			yawOffset = 45f,
			meshHorizontalOffset = 0.45f,
			meshYRotationOffset = 16f,
			meshDownOffset = 0.45f,
			meshForwardOffset = 0.45f
		}
	};

	public float driftLoopMinPitch = 0.8f;

	public float driftLoopMaxPitch = 1.5f;

	private bool isDrifting;

	private float driftTimer;

	private float driftDirectionMismatchTimer;

	private float driftSwitchImpactCooldown;

	private float driftIntensity;

	private float driftLightLerp;

	private bool driftLightFadingIn;

	private int driftLightCurrentLevel;

	private Color driftLightCurrentColor;

	private float driftLightCurrentIntensity;

	private float driftLightCurrentFlickerAmount;

	private float driftLightCurrentFlickerSpeed;

	private float driftLightCurrentPulseIntensity;

	private float driftLightCurrentPulseSpeed;

	private float driftCurrentRollAngle;

	private float driftCurrentYawOffset;

	private float driftCurrentMeshHorizontalOffset;

	private float driftCurrentMeshYRotationOffset;

	private float driftCurrentMeshDownOffset;

	private float driftCurrentMeshForwardOffset;

	private float driftSmoothedSteerOffset;

	private float driftSteerOffsetVelocity;

	private float driftMeshHorizontalVelocity;

	private float driftMeshYRotationVelocity;

	private float driftMeshDownVelocity;

	private float driftMeshForwardVelocity;

	private float driftDirection;

	private float driftExitSnapTimer;

	private float driftDurationTimer;

	private float driftCooldownTimer;

	private float driftStateChangeCooldown;

	private float driftLowSpeedEaseProgress;

	private float driftOriginalAngularDrag;

	private bool driftQueuedWhileAirborne;

	private float driftQueuedDirection;

	private float postDriftStabilizationBlend;

	private float smoothDragTarget;

	private float smoothDragCurrent;

	private float smoothDragVelocity;

	private float smoothAngularDragTarget;

	private float smoothAngularDragCurrent;

	private float smoothAngularDragVelocity;

	private float targetCameraYaw;

	private float driftStartSpeed;

	private float driftSpeedSampleSum;

	private int driftSpeedSampleCount;

	private float driftExitDelayTimer;

	private int driftExitPendingBoostLevel;

	private bool driftExitPendingSpeedRestore;

	private float driftExitPendingRestoreSpeed;

	private bool driftPausedByAirborne;

	private int driftPausedBoostLevel;

	private float driftPausedBoostProgress;

	public float driftBoostTimeAtFullSpeed = 4f;

	public float driftBoostTimeAtLowSpeed = 8f;

	public float driftBoostFullSpeedThreshold = 16f;

	public float driftBoostSpeedOverTimeRate = 5f;

	public float driftBoostHyperSpeedRate = 5f;

	public float driftBoostLevel1Threshold = 0.2f;

	public float driftBoostLevel1ForwardBoost = 3f;

	public float driftBoostLevel1Duration = 0.5f;

	public float driftBoostLevel1SpeedBoostMultiplier = 0.35f;

	public float driftBoostLevel2Threshold = 0.66f;

	public float driftBoostLevel2ForwardBoost = 5f;

	public float driftBoostLevel2Duration = 0.8f;

	public float driftBoostLevel2SpeedBoostMultiplier = 0.5f;

	public float driftBoostLevel3Threshold = 1f;

	public float driftBoostLevel3ForwardBoost = 8f;

	public float driftBoostLevel3Duration = 1.5f;

	public float driftBoostLevel3SpeedBoostMultiplier = 1f;

	private int driftBoostLevel;

	private float driftBoostProgress;

	private float driftBoostGraceTimer;

	private float driftBoostActiveTimer;

	private float driftBoostActiveMultiplier;

	private int driftBoostActivatedLevel;

	private float driftBoostActiveForce;

	private Vector3 driftBoostActiveDirection;

	private float driftBoostConstantDuration = 1f;

	private float driftBoostFadeDuration = 0.5f;

	public bool enableMovementPhysics = true;

	public bool enableStabilizationPhysics = true;

	public bool enableSteeringPhysics = true;

	public bool enableDriftPhysics = true;

	public bool enableAirbornePhysics = true;

	public bool enableWheelPhysics = true;

	public bool enablePlayerPhysics = true;

	public bool enableStairApproachAssist = true;

	public Transform frontGroundedTransform;

	public Transform backGroundedTransform;

	public float airborneLiftForce = 10f;

	public float maxGroundedAngle = 40f;

	private bool isFrontGrounded;

	private bool isBackGrounded;

	private bool isFrontColliding;

	private bool isBackColliding;

	private float groundCheckTimer;

	private float frontUngroundedTimer;

	private float backUngroundedTimer;

	private float landingFrontLiftCooldown;

	private float landingFrontDownForceGraceTimer;

	private float landingBackDownForceGraceTimer;

	private float landingAlignTimer;

	private float landingBothWheelsWindow;

	private float landingBufferedSpeed;

	private float landingPitchFullStrengthTimer;

	private float[] airborneSpeedBuffer = new float[75];

	private int airborneSpeedBufferIndex;

	private int airborneSpeedBufferCount;

	private float frontLiftUpCooldown;

	private float frontLiftDownCooldown;

	private float continuousAirborneTimer;

	private float nosediveCooldownTimer;

	private bool wasFrontGroundedLastFrame;

	private bool wasBackGroundedLastFrame;

	private float airborneBlend;

	private float airborneAnimatedPitch;

	private float airborneWindPitch = 1f;

	private float airborneWindPitchVelocity;

	private bool airborneWindActive;

	private bool wasFullyAirborne;

	private float pitchStabilizationBlend = 1f;

	private float previousPitchAngle;

	private float pitchChangeAccumulator;

	private float pitchChangeTimer;

	private float peakRecentSpeed;

	private float peakSpeedTimer;

	private float peakSpeedOverTime;

	private float peakHyperSpeedOverTime;

	public Transform wheelFront;

	public Transform wheelFrontForcePoint;

	public float wheelFrontForceMin = 10f;

	public float wheelFrontForceMax = 30f;

	public float wheelFrontForceMaxSpeed = 15f;

	public Transform wheelBack;

	public Transform wheelBackForcePoint;

	public float wheelBackForceMin = 10f;

	public float wheelBackForceMax = 30f;

	public float wheelBackForceMaxSpeed = 15f;

	public float wheelGroundingRayDistance = 0.2f;

	public float wheelGroundingForce = 50f;

	public Light frontLight;

	public Renderer frontLightRenderer;

	public Color frontLightEmissionColor = Color.white;

	public Light backLight;

	public Renderer backLightRenderer;

	public Color backLightEmissionColor = Color.red;

	public float lightFlickerDuration = 0.2f;

	public float lightMinOnDuration = 0.5f;

	public bool hasBackingUpBeeps;

	public float backingUpBeepInterval = 0.5f;

	private float frontLightFlickerTimer;

	private float frontLightOnTimer;

	private float backLightOnTimer;

	private bool frontLightOn;

	private bool backLightOn;

	private float backingUpBeepTimer;

	private bool frontLightFlickerOff;

	private float overrideFrontLightOffTimer;

	private bool overrideFrontLightOffActive;

	private float frontLightPhotosensitiveLerp;

	private float backLightPhotosensitiveLerp;

	public Light boostLight;

	public AnimationCurve boostLightCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

	public float boostLightDuration = 1.5f;

	public float boostLightIntensityLevel1 = 5f;

	public float boostLightIntensityLevel2 = 8f;

	public float boostLightIntensityLevel3 = 10f;

	private float boostLightTimer;

	private int boostLightActiveLevel;

	private float boostLightBaseIntensity;

	private Color boostLightTargetColor;

	public Transform meshTransform;

	public float meshOffsetSmoothTime = 0.06f;

	public float meshRotationXSmoothTime = 0.3f;

	public float meshDismountSpeed = 30f;

	public float meshTeleportDistance = 2f;

	public float meshMaxForwardOffset = 0.85f;

	public float meshTiltPushDistance = 0.5f;

	public float meshTiltPushSmoothTime = 0.3f;

	public float meshUptiltForwardOffset = 1f;

	public float meshUptiltDownOffset = 0.5f;

	public bool enableNonLocalMeshPrediction = true;

	public float nonLocalMeshPredictionLookahead = 0.12f;

	public float nonLocalMeshPredictionSmoothTime = 0.08f;

	public float nonLocalMeshPredictionCalmSmoothTime = 0.4f;

	public float nonLocalMeshPredictionCalmGapDistance = 0.15f;

	public float nonLocalMeshPredictionCatchUpGapDistance = 1.5f;

	public float nonLocalMeshPredictionMinSpeedRampRange = 3f;

	public float nonLocalMeshPredictionMaxPositionOffset = 2f;

	public float nonLocalMeshPredictionMaxRotationOffset = 25f;

	public float nonLocalMeshPredictionSnapDistance = 5f;

	public float nonLocalMeshPredictionSnapAngle = 90f;

	public float nonLocalMeshPredictionMinSpeed = 6f;

	public float wobbleMinVelocity = 1.5f;

	public float wobbleMinDeceleration = 0.1f;

	public float wobbleMaxDeceleration = 5f;

	public float wobbleForceDeadzone = 0.02f;

	public float wobbleCooldown = 0.3f;

	public float wobblePositionStrength = 0.06f;

	public float wobbleRotationStrength = 30f;

	public float wobblePositionMax = 0.3f;

	public float wobbleRotationMax = 40f;

	public float wobbleSpringStiffness = 500f;

	public float wobbleSpringDamping = 35f;

	public float driftRumblePositionStrength = 0.02f;

	public float driftRumbleRotationStrength = 2f;

	public float driftRumbleSpeed = 30f;

	public float driftRumbleBoostMultiplier = 2f;

	public MeshRenderer grabMesh;

	private Material grabMaterial;

	private int grabVisualState;

	private int grabVisualStatePrevious = -1;

	public Renderer hoverEffectRenderer;

	public Renderer hoverEffectFrontRenderer;

	public Renderer hoverEffectBackRenderer;

	public Vector2 hoverEffectScrollSpeed = new Vector2(0.15f, 0f);

	public float hoverEffectScrollNoiseSpeed = 0.2f;

	public float hoverEffectScrollNoiseStrength = 0.5f;

	public float hoverEffectRotateSpeedX = 80f;

	public float hoverEffectRotateSpeedZ = 50f;

	public float hoverEffectRotateNoiseSpeed = 0.3f;

	public float hoverEffectRotateNoiseStrength = 0.7f;

	public float hoverEffectScaleNoiseSpeed = 0.3f;

	public float hoverEffectScaleNoiseStrength = 0.25f;

	public float hoverEffectScrollOffsetPerMesh = 0.33f;

	private Material[] hoverEffectMaterials;

	private Transform[] hoverEffectTransforms;

	private float[] hoverEffectNoiseSeeds;

	private Vector3[] hoverEffectOriginalScales;

	private Transform meshOriginalParent;

	private Vector3 meshOriginalLocalPosition;

	private Quaternion meshOriginalLocalRotation;

	private bool meshDeparented;

	private Vector3 meshPreviousPosition;

	private Quaternion meshPreviousRotation;

	private Vector3 meshCalculatedVelocity;

	private Vector3 meshCalculatedAngularVelocity;

	private int localSeatIndex = -1;

	private bool localMeshFollowResetPending = true;

	private Vector3 smoothedCameraOffset;

	private Vector3 smoothedCameraOffsetVelocity;

	private bool smoothedCameraOffsetInitialized;

	private float forwardClampExcess;

	private float forwardClampExcessVelocity;

	private Vector3 previousFrameOffset;

	private bool previousFrameOffsetValid;

	private Vector3 previousCameraPosition;

	private bool previousCameraPositionValid;

	private Vector3[] offsetBuffer = new Vector3[12];

	private int offsetBufferIndex;

	private int offsetBufferCount;

	private Quaternion smoothedTargetRotation;

	private bool smoothedTargetRotationValid;

	private float meshRotationXVelocity;

	private float meshRotationYVelocity;

	private float meshRotationZVelocity;

	private float meshTiltPushCurrent;

	private float meshTiltPushVelocity;

	private float meshDriftPushCurrent;

	private float meshDriftPushVelocity;

	private float meshDriftHorizontalPushCurrent;

	private float meshDriftHorizontalPushVelocity;

	private float meshDriftYRotationCurrent;

	private float meshDriftYRotationVelocity;

	private float meshDriftForwardPushCurrent;

	private float meshDriftForwardPushVelocity;

	private float meshUptiltCurrentForward;

	private float meshUptiltForwardVelocity;

	private float meshUptiltCurrentDown;

	private float meshUptiltDownVelocity;

	private Vector3 wobblePreviousFakeVelocity;

	private float wobbleMovingTimer;

	private float wobbleCooldownTimer;

	private float wobblePositionOffset;

	private float wobblePositionSpringVelocity;

	private Vector3 wobbleDirection;

	private Vector3 wobbleDirectionSmoothed;

	private float previousWobblePositionOffset;

	private float previousWobbleRotationOffset;

	private float wobbleRotationOffset;

	private float wobbleRotationSpringVelocity;

	private float driftRumblePositionOffset;

	private float driftRumbleRotationOffset;

	private float driftRumbleIntensity;

	private float sparkShakePositionOffset;

	private float sparkShakeRotationOffset;

	private float sparkShakePositionTarget;

	private float sparkShakeRotationTarget;

	private float sparkShakeIntensity;

	private float sparkShakeUpdateTimer;

	private float sparkImpactShakeIntensity;

	private Vector3 sparkImpactShakePositionOffset;

	private Vector3 sparkImpactShakeRotationOffset;

	private Vector3 impactShakePositionOffset;

	private Vector3 impactShakeRotationOffset;

	private float impactShakeIntensity;

	private float impactShakeDecaySpeed;

	private float driftMeshOffsetBlend;

	private float velocityPitchAudioCurrent;

	private float velocityPitchAudioVelocity;

	private float smoothTurnPitch;

	private float meshAppliedBaseOffsetX;

	private float meshAppliedBaseOffsetY;

	private Vector3 nonLocalMeshPositionApplied;

	private Quaternion nonLocalMeshRotationApplied = Quaternion.identity;

	internal Vector3 meshVisualPositionOffset;

	internal Vector3 meshVisualRotationEulerOffset;

	private Vector3 nonLocalPredictedMeshWorldPosition;

	private Quaternion nonLocalPredictedMeshWorldRotation = Quaternion.identity;

	private bool nonLocalMeshPredictionInitialized;

	private Vector3 nonLocalSmoothedLinearVelocity;

	private Vector3 nonLocalSmoothedAngularVelocity;

	private bool nonLocalSmoothedVelocityInitialized;

	private int nonLocalMeshPredictionWallLayer;

	public float nonLocalMeshPredictionVelocitySmoothTime = 0.25f;

	public bool nonLocalMeshPredictionNetworkTimed;

	public float nonLocalMeshPredictionMaxExtrapolation = 0.4f;

	[Range(0f, 1f)]
	public float nonLocalMeshPredictionTurnFollow = 1f;

	public float nonLocalMeshPredictionExtraLead;

	public bool networkRigidbodyExtrapolate;

	public float nonLocalMeshPredictionAngularSmoothTime = 0.2f;

	public float nonLocalMeshPredictionRotationCalmSmoothTime = 0.4f;

	public float nonLocalMeshPredictionTimedCalmSmoothTime = 0.15f;

	public float nonLocalMeshPredictionTimedMaxRotation = 12f;

	public float nonLocalMeshPredictionAlongSmoothTime = 0.5f;

	[Range(0f, 1f)]
	public float nonLocalMeshPredictionMinForwardStep = 0.5f;

	public bool nonLocalMeshPredictionPacketClock = true;

	private int nonLocalPacketTimestamp;

	private float nonLocalPacketAge;

	private float nonLocalPacketLocalTime;

	private PhotonTransformView nonLocalPhotonTransformView;

	private Collider[] nonLocalPredictionColliders;

	private Collider[] nonLocalPredictionOverlap = new Collider[16];

	private float nonLocalPredictionColliderRadius;

	public float impactDetectionWindow = 0.1f;

	public float impactCooldown = 0.2f;

	public float impactTypeCooldown = 0.5f;

	public float smallImpactThreshold = 5f;

	public float mediumImpactThreshold = 10f;

	public float bigImpactThreshold = 20f;

	public float smallImpactDistanceRequired = 1f;

	public float mediumImpactDistanceRequired = 2f;

	public float bigImpactDistanceRequired = 3f;

	public float durabilityLossMultiplier = 1f;

	public float smallImpactDurabilityLoss = 0.05f;

	public float mediumImpactDurabilityLoss = 0.2f;

	public float bigImpactDurabilityLoss = 1f;

	public float smallImpactEnemyDurabilityLossLevel1 = 0.02f;

	public float smallImpactEnemyDurabilityLossLevel2 = 0.04f;

	public float smallImpactEnemyDurabilityLossLevel3 = 0.06f;

	public float smallImpactEnemyDurabilityCooldown = 0.5f;

	public float mediumImpactEnemyDurabilityLossLevel1 = 0.1f;

	public float mediumImpactEnemyDurabilityLossLevel2 = 0.15f;

	public float mediumImpactEnemyDurabilityLossLevel3 = 0.2f;

	public float mediumImpactEnemyDurabilityCooldown = 0.5f;

	public float bigImpactEnemyDurabilityLossLevel1 = 0.3f;

	public float bigImpactEnemyDurabilityLossLevel2 = 0.5f;

	public float bigImpactEnemyDurabilityLossLevel3 = 1f;

	public float bigImpactEnemyDurabilityCooldown = 0.5f;

	public Transform midpointTransform;

	public Transform impactTransform;

	public float impactShakePositionStrength = 0.12f;

	public float impactShakeRotationStrength = 8f;

	public float impactShakeDecayRate = 15f;

	public float impactShakeSmallIntensity = 0.3f;

	public float impactShakeMediumIntensity = 0.6f;

	public float impactShakeBigIntensity = 1f;

	public ParticleSystem impactParticlesSmall;

	public ParticleSystem impactParticlesMedium;

	public ParticleSystem impactParticlesBig;

	public ParticleSystem fallDamageParticles;

	public ParticleSystem nosediveParticles;

	public ParticleSystem nosediveParticlesHeavy;

	public ParticleSystem particlesForGoingFast;

	public ParticleSystem tumbleParticles;

	public float upsideDownTumbleThreshold = 1.5f;

	public float upsideDownTumbleDuration = 3f;

	internal FakeVelocities fakeVelocities;

	private float[] impactVelocityHistory;

	private Vector3[] impactVelocityDirectionHistory;

	private int impactHistoryIndex;

	private int impactHistorySampleCount;

	private ImpactType lastDetectedImpact;

	private float impactCooldownTimer;

	private Vector3 velocityBeforeImpact;

	private ImpactType pendingImpactType;

	private float pendingImpactConfirmTimer;

	private float pendingVelocityDrop;

	private float pendingOldestVelocity;

	private float pendingNewestVelocity;

	private int pendingOldestIndex;

	private float smoothedPitchAngle;

	private float smoothedPitchSpringTorque;

	private float smoothedAngularDragMagnitude;

	private float pitchStabAirborneGraceTimer;

	private float pitchSlopeTransitionTimer;

	private float lastValidSlopeAngle;

	private float lastValidSlopeTimer;

	private float groundSlopePitchAngle;

	private float previousGroundSlopePitchAngle;

	private bool groundSlopeValid;

	private float impactCooldownSmall;

	private float impactCooldownMedium;

	private float impactCooldownBig;

	private float impactStartupTimer;

	private float landingImpactCooldown;

	private float distanceTraveledAtSmallSpeed;

	private float distanceTraveledAtMediumSpeed;

	private float distanceTraveledAtBigSpeed;

	private Vector3 lastPositionForDistanceTracking;

	private bool goingFastParticlesPlaying;

	private float goingFastParticlesCooldown;

	private bool localPlayerIsMounted;

	private float soundEffectVolume;

	public float soundEffectVolumeForMountedPlayer = 1f;

	public float soundEffectVolumeForOthers = 1f;

	public Sound audioEngineLoop;

	public Sound hyperSpeedLoop;

	public Sound airborneWindLoop;

	public Sound engineStartSound;

	public Sound engineStopSound;

	public float minEnginePitch = 1.5f;

	public float maxEnginePitch = 5f;

	public Sound backingUpBeepSound;

	public float velocityPitchUpForce = 5f;

	public float velocityPitchUpMax = 10f;

	public float velocityPitchDownForce = 5f;

	public float velocityPitchDownMax = 10f;

	public float velocityPitchSmoothTime = 0.2f;

	public float velocityPitchYawForce = 2f;

	public float velocityPitchYawMax = 8f;

	public Sound driftStartSound;

	public Sound driftLoopSound;

	public Sound driftLoopLevel1Sound;

	public Sound driftLoopLevel2Sound;

	public Sound driftLoopLevel3Sound;

	public Sound driftEndSound;

	public Sound driftBoostLevel1ReachedSound;

	public Sound driftBoostLevel2ReachedSound;

	public Sound driftBoostLevel3ReachedSound;

	public Sound driftBoostLevel1Sound;

	public Sound driftBoostLevel2Sound;

	public Sound driftBoostLevel3Sound;

	public Sound impactSoundSmall;

	public Sound impactSoundMedium;

	public Sound impactSoundBig;

	public Sound entityHitSoundSmall;

	public Sound entityHitSoundMedium;

	public Sound entityHitSoundBig;

	public Sound entityHitSoundBigGlobal;

	public Sound wobbleImpactSound;

	public Sound fallDamageSound;

	public Sound nosediveSound;

	public Sound nosediveSoundHeavy;

	public Sound tumbleSound;

	public Sound tumbleStartSound;

	public Sound tumbleEndSound;

	public Sound tumbleWarningBeep;

	public Sound damageSmokeLoopSound;

	public Sound damageFireLoopSound;

	public Sound valuableBoxBreakBuildup;

	public Sound vehicleFixSound;

	public Sound vehicleBreakSound;

	public Sound turnLoopSound;

	public Sound sparkLoopLeft;

	public Sound sparkLoopRight;

	public Sound sparkStartSound;

	public Sound sparkEndSound;

	public Sound honkSound;

	public float aiHonkPitchMin = 0.9f;

	public float aiHonkPitchMax = 1.25f;

	public Transform sparkTransformLeft;

	public Transform sparkTransformRight;

	public ParticleSystem sparkParticlesLeft;

	public ParticleSystem sparkParticlesRight;

	public BoxCollider sparkColliderLeft;

	public BoxCollider sparkColliderRight;

	public Light sparkLightLeft;

	public Light sparkLightRight;

	public float sparkMinSpeed = 3f;

	public float sparkMaxSpeed = 15f;

	public float sparkMinPitch = 0.8f;

	public float sparkMaxPitch = 1.5f;

	public float sparkMinGain = 0.1f;

	public float sparkMaxGain = 1f;

	private float sparkLeftOverrideTimer;

	private float sparkRightOverrideTimer;

	private bool sparkLeftActive;

	private bool sparkRightActive;

	private float sparkLeftCooldownTimer;

	private float sparkRightCooldownTimer;

	private float sparkLightLeftLerp;

	private float sparkLightRightLerp;

	private float sparkLeftStartBurstTimer;

	private float sparkRightStartBurstTimer;

	private float sparkLeftStopBurstTimer;

	private float sparkRightStopBurstTimer;

	private float sparkLeftLastMultiplier;

	private float sparkRightLastMultiplier;

	private float[] sparkLeftDefaultEmissions;

	private int[] sparkLeftDefaultMaxParticles;

	private ParticleSystem[] sparkLeftAllParticles;

	private float[] sparkRightDefaultEmissions;

	private int[] sparkRightDefaultMaxParticles;

	private ParticleSystem[] sparkRightAllParticles;

	private LayerMask sparkLayerMask;

	public DestructionState[] destructionStates;

	public ParticleSystem stateChangeParticles;

	public ParticleSystem destroyParticles;

	public ParticleSystem damageSmoke;

	public ParticleSystem damageFire;

	public Light fireLight;

	public float fireLightIntensity = 3f;

	public float fireLightFlickerAmount = 1f;

	public Transform explosionPosTransform;

	public ItemValuableBox valuableBox;

	public ParticleSystem valuableBoxBreakBuildupParticle;

	public ParticleSystem valuableBoxComebackParticle;

	public ParticleSystem vehicleFixParticles;

	private ParticleScriptExplosion explosionScript;

	private ItemBattery itemBattery;

	private float prevBatteryLife;

	private int currentDestructionStateIndex = -1;

	private float fireWarningTimer;

	private float valuableBoxExplosionTimer;

	private float valuableBoxExplosionTimerMax;

	private bool valuableBoxExplosionPending;

	private float damageSmokeOverrideTimer;

	private float damageFireOverrideTimer;

	private float fireLightLerp;

	internal bool isFullyDestroyed;

	private bool canTriggerExplosion = true;

	private float vehicleExistenceTimer;

	internal PhysGrabObject physGrabObject;

	internal PhotonView photonView;

	private float lookAtTimer;

	private float currentSteeringAngle;

	private float targetSteeringAngle;

	private Quaternion steeringOriginalRotation;

	private SpringQuaternion steeringSpring;

	private Vector3 smoothedSteeringTarget;

	private bool smoothedSteeringTargetInitialized;

	internal float speedOverTime;

	internal float hyperSpeedOverTime;

	private float seatSlowdownRate;

	private float seatVelocityDrag;

	private float previousVelocityMagnitude;

	private float impactSpeedLossTimer;

	private float hurtColliderTimerSmall;

	private float hurtColliderTimerMedium;

	private float hurtColliderTimerBig;

	private float enemyHitCooldownTimerSmall;

	private float enemyHitCooldownTimerMedium;

	private float enemyHitCooldownTimerBig;

	private float enemyImpactFreezeCooldown;

	private Dictionary<int, float> bumperCarPairCooldowns;

	public float bumperCarForceMultiplier = 1f;

	public float bumperCarSideTackleMultiplier = 1.5f;

	private const float bumperCarPairCooldownDuration = 0.5f;

	private const float bumperCarProximityRadius = 20f;

	private const float bumperCarBounceForceSmall = 8f;

	private const float bumperCarBounceForceMedium = 16f;

	private const float bumperCarBounceForceBig = 28f;

	private float tumbleTimer;

	private float tumbleBlinkTimer;

	private bool tumbleBlinkRed;

	private bool arenaRaceTumbleYawPending;

	private float upsideDownTimer;

	private float velocityMismatchTimer;

	private float currentFallGravity;

	private float accumulatedFallVelocity;

	private float currentCustomGravitySpeed;

	private float fallGravityStartHeight;

	private bool fallDamageActive;

	private bool fallDamageDisabled;

	private bool wasAirborneLastFrame;

	private float vehicleUprightAngle;

	private float uprightMultiplier;

	private float stairClimbBoost;

	private float stairAssistActiveTimer;

	private float stairAssistCooldown;

	private Vector3 horizontalVelocity;

	private float currentSpeed;

	private bool isAnyWheelGrounded;

	private bool isVehicleAirborne;

	private float wallAheadScale;

	private float[] pitchHistory = new float[50];

	private int pitchHistoryIndex;

	private int pitchHistoryFillCount;

	private float airborneAnimatedPeakTarget;

	private float currentPitchAngle;

	private PlayerAvatar currentDriver;

	private bool currentDriverFullyMounted;

	private float inputY;

	private bool hasAnySeatedPlayerCached;

	private float dismountLowSpeedTimer;

	private bool dismountedDefaultMaterial;

	private bool unmountedFrictionActive;

	private bool unmountedFrictionWaitingForSlow;

	private float unmountedFrictionEaseTimer;

	private float honkCooldownTimer;

	private AudioSource activeHonkSource;

	private int activeHonkSeatIndex = -1;

	private float activeHonkPitch = 1f;

	private static int emissionColorID = -1;

	private static int layerMaskDefaultPhysGrab = -1;

	private static int layerMaskDefaultEnemyPlayer = -1;

	private static int layerMaskGroundCheck = -1;

	private static int layerMaskPhysGrabObject = -1;

	private static List<ItemVehicle> allActiveVehicles = new List<ItemVehicle>();

	private static Dictionary<Transform, CachedComponents> componentCache = new Dictionary<Transform, CachedComponents>();

	private static float componentCacheClearTimer;

	private Material cachedFrontLightMaterial;

	private Material cachedBackLightMaterial;

	private float torqueOverrideTimer;

	private Quaternion torqueOverrideRotation;

	internal float torqueOverrideSpring = 300f;

	internal float torqueOverrideDamping = 25f;

	private float activeTorqueOverrideSpring;

	private float activeTorqueOverrideDamping;

	private float yawOverrideTimer;

	private Vector3 yawOverrideDirection;

	private float pitchOverrideTimer;

	private float pitchOverrideAngle;

	private float customGravityOverrideTimer;

	private float loopModeOverrideTimer;

	private float cameraLookForwardOverrideTimer;

	public VehicleGamepad gamepad = new VehicleGamepad();

	private PlayerAvatar gamepadDriver;

	private Vector3 gamepadSteeringDirection;

	private const float RumbleEnginePower = 0.12f;

	private const float RumbleThrottlePower = 0.045f;

	private const float RumbleRoadPower = 0.035f;

	private const float RumbleTirePower = 0.18f;

	private const float RumbleBrakePower = 0.14f;

	private const float RumbleBumpPower = 0.07f;

	private const float RumbleLowPowerMax = 0.24f;

	private const float RumbleHighPowerMax = 0.35f;

	private readonly RaycastHit[] rumbleGroundHits = new RaycastHit[8];

	private float rumbleEnginePhase;

	private float rumbleTravel;

	private float rumbleGroundedTimer;

	private float rumbleLowPower;

	private float rumbleHighPower;

	private float aiControlTimer;

	private float aiControlInputThrottle;

	private float aiControlInputSteer;

	private Vector3 aiControlForwardReference = Vector3.forward;

	private PhysGrabObject aiRiderPhysObj;

	private EnemyTick aiRiderClaimant;

	private float aiRiderClaimTimer;

	private float aiTumbleImmuneTimer;

	private float aiDriftOverrideTimer;

	private float aiDriftOverrideDirection;

	private Collider[] aiRiderIgnoredColliders;

	private Collider[] aiVehicleAllColliders;

	private float aiRiderIgnoreTimer;

	private float tuningSpeedMultiplier = 1f;

	private float tuningAccelerationMultiplier = 1f;

	private float tuningSteeringMultiplier = 1f;

	private float tuningDriftBoostMultiplier = 1f;

	private float tuningYawMultiplier = 1f;

	private float originalMaxForwardSpeed;

	private float originalMaxReverseSpeed;

	private float originalHyperMaxSpeed;

	private float originalDriftMaxSpeed;

	private float originalForwardAcceleration;

	private float originalHyperAcceleration;

	private float originalSteeringSpeed;

	private float originalYawSpringStrength;

	private float originalDriftForce;

	private float originalDriftExitBoost;

	private float originalDriftBoostLevel1ForwardBoost;

	private float originalDriftBoostLevel2ForwardBoost;

	private float originalDriftBoostLevel3ForwardBoost;

	private float originalDriftDrag;

	private float originalDriftLowSpeedDrag;

	[HideInInspector]
	public float catchUpMultiplier = 1f;

	private float racingOutsideRaceForceCompensation = 1f;

	private float draftingRaycastTimer;

	private float draftingActiveTimer;

	private float vehicleUIKmh;

	private Vector3 VehicleUp
	{
		get
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			if (!(loopModeOverrideTimer > 0f))
			{
				return Vector3.up;
			}
			return ((Component)this).transform.up;
		}
	}

	private Vector3 VehicleDown
	{
		get
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			if (!(loopModeOverrideTimer > 0f))
			{
				return Vector3.down;
			}
			return -((Component)this).transform.up;
		}
	}

	private bool GamepadControlActive
	{
		get
		{
			if (!AIControlActive && gamepad.currentState != VehicleGamepad.State.Keyboard && Object.op_Implicit((Object)(object)Driver) && Driver.isLocal)
			{
				return DriverFullyMounted;
			}
			return false;
		}
	}

	private float ControlThrottle
	{
		get
		{
			if (!AIControlActive)
			{
				if (!Object.op_Implicit((Object)(object)Driver))
				{
					return 0f;
				}
				return Driver.InputDirectionRaw.z;
			}
			return aiControlInputThrottle;
		}
	}

	internal bool AIControlActive => aiControlTimer > 0f;

	internal bool AITumbleImmune => aiTumbleImmuneTimer > 0f;

	internal Transform AIDriverSeat
	{
		get
		{
			if (seats == null || seats.Length == 0 || !Object.op_Implicit((Object)(object)seats[0].seatTransform))
			{
				return ((Component)this).transform;
			}
			return seats[0].seatTransform;
		}
	}

	internal Transform AIDriverGrabPoint
	{
		get
		{
			if (seats == null || seats.Length == 0 || !Object.op_Implicit((Object)(object)seats[0].grabPoint))
			{
				return AIDriverSeat;
			}
			return seats[0].grabPoint;
		}
	}

	private bool HasDriverControl
	{
		get
		{
			if (!AIControlActive)
			{
				if (Object.op_Implicit((Object)(object)currentDriver))
				{
					return currentDriverFullyMounted;
				}
				return false;
			}
			return true;
		}
	}

	private float ControlInputX
	{
		get
		{
			if (!AIControlActive)
			{
				if (!Object.op_Implicit((Object)(object)Driver))
				{
					return 0f;
				}
				return Driver.InputDirectionRaw.x;
			}
			return aiControlInputSteer;
		}
	}

	private Vector3 ControlCameraForward
	{
		get
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			if (!AIControlActive)
			{
				return Driver.localCamera.GetOverrideTransform().forward;
			}
			return aiControlForwardReference;
		}
	}

	private PlayerAvatar Driver
	{
		get
		{
			if (seats == null || seats.Length == 0)
			{
				return null;
			}
			PlayerAvatar seatedPlayer = seats[0].seatedPlayer;
			if (!PlayerPhysicsReady(seatedPlayer))
			{
				return null;
			}
			return seatedPlayer;
		}
	}

	private bool DriverFullyMounted
	{
		get
		{
			if (seats == null || seats.Length == 0)
			{
				return false;
			}
			if ((Object)(object)seats[0].seatedPlayer == (Object)null)
			{
				return false;
			}
			if (seats[0].reachedFirstMount || (Object)(object)seats[0].firstMountTransform == (Object)null)
			{
				return seats[0].mountFinishTimer <= 0f;
			}
			return false;
		}
	}

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void Init()
	{
		emissionColorID = -1;
		layerMaskDefaultPhysGrab = -1;
		layerMaskDefaultEnemyPlayer = -1;
		layerMaskGroundCheck = -1;
		layerMaskPhysGrabObject = -1;
		allActiveVehicles = new List<ItemVehicle>();
		componentCache = new Dictionary<Transform, CachedComponents>();
		componentCacheClearTimer = 0f;
	}

	private static void InitCachedStatics()
	{
		if (emissionColorID == -1)
		{
			emissionColorID = Shader.PropertyToID("_EmissionColor");
			layerMaskDefaultPhysGrab = LayerMask.GetMask(new string[2] { "Default", "PhysGrabObject" });
			layerMaskDefaultEnemyPlayer = LayerMask.GetMask(new string[3] { "Default", "Enemy", "Player" });
			layerMaskGroundCheck = LayerMask.GetMask(new string[4] { "Default", "PhysGrabObject", "PhysGrabObjectHinge", "PhysGrabObjectCart" });
			layerMaskPhysGrabObject = LayerMask.GetMask(new string[1] { "PhysGrabObject" });
		}
	}

	private static CachedComponents GetCachedComponents(Collider _col)
	{
		Transform transform = ((Component)_col).transform;
		if (componentCache.TryGetValue(transform, out var value) && value.initialized)
		{
			return value;
		}
		value = new CachedComponents
		{
			vehicle = ((Component)_col).GetComponentInParent<ItemVehicle>(),
			enemy = ((Component)_col).GetComponentInParent<Enemy>(),
			enemyRigidbody = ((Component)_col).GetComponentInParent<EnemyRigidbody>(),
			valuable = ((Component)_col).GetComponentInParent<ValuableObject>(),
			playerTumble = ((Component)_col).GetComponentInParent<PlayerTumble>(),
			playerAvatar = ((Component)_col).GetComponentInParent<PlayerAvatar>(),
			playerController = ((Component)_col).GetComponentInParent<PlayerController>(),
			initialized = true
		};
		componentCache[transform] = value;
		return value;
	}

	internal void OverrideDisableCustomGravity(float _duration)
	{
		customGravityOverrideTimer = _duration;
	}

	internal void OverrideLoopMode(float _duration)
	{
		loopModeOverrideTimer = _duration;
	}

	internal void OverrideCameraLookForward(float _duration)
	{
		cameraLookForwardOverrideTimer = _duration;
	}

	private void GamepadInputReset()
	{
		gamepad.InputReset();
		gamepadDriver = null;
		GamepadCameraReset();
		smoothedSteeringTargetInitialized = false;
	}

	private void GamepadInputUpdate()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar driver = Driver;
		if ((Object)(object)gamepadDriver != (Object)(object)driver)
		{
			GamepadInputReset();
			gamepadDriver = driver;
		}
		if (!Object.op_Implicit((Object)(object)driver) || !driver.isLocal || !Object.op_Implicit((Object)(object)InputManager.instance))
		{
			return;
		}
		VehicleGamepad.State state = gamepad.currentState;
		if (InputManager.instance.CurrentMode == InputModes.Gamepad)
		{
			InputManager.instance.OverrideVehicleGamepad();
			if (currentState == State.Active && DriverFullyMounted && !driver.isDisabled && !AIControlActive)
			{
				InputManager.instance.VehicleGamepadInputUpdate(gamepad);
			}
			else
			{
				gamepad.InputUpdate(Vector2.zero, 0f, 0f, leftShoulder: false, rightShoulder: false);
			}
		}
		else
		{
			gamepad.InputReset();
		}
		bool flag = state != gamepad.currentState;
		bool flag2 = state == VehicleGamepad.State.Keyboard || gamepad.currentState == VehicleGamepad.State.Keyboard;
		if (flag & flag2)
		{
			smoothedSteeringTargetInitialized = false;
			GamepadCameraReset();
		}
	}

	internal Vector3 GamepadInputDirectionGet(PlayerAvatar player, Vector3 input)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)player) || !player.isLocal || (Object)(object)Driver != (Object)(object)player || !Object.op_Implicit((Object)(object)InputManager.instance))
		{
			return input;
		}
		GamepadInputUpdate();
		if (!InputManager.instance.VehicleInputAllowed())
		{
			return Vector3.zero;
		}
		if (gamepad.currentState == VehicleGamepad.State.Keyboard)
		{
			return input;
		}
		return new Vector3(gamepad.DriftDirection, 0f, gamepad.throttle);
	}

	private void GamepadCameraReset()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		gamepadSteeringDirection = Vector3.zero;
		gamepad.CameraTurnReset();
	}

	private void GamepadCameraUpdate(PlayerAvatar player)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		if (!GamepadControlActive || currentState != State.Active)
		{
			GamepadCameraReset();
			return;
		}
		if (!Object.op_Implicit((Object)(object)CameraAim.Instance) || !Object.op_Implicit((Object)(object)player.localCamera))
		{
			GamepadCameraReset();
			return;
		}
		if (!InputManager.instance.VehicleGamepadLookAllowed())
		{
			GamepadCameraReset();
			return;
		}
		Vector3 val = Vector3.ProjectOnPlane(((Component)this).transform.forward, Vector3.up);
		Vector3 normalized = val.normalized;
		if (normalized.sqrMagnitude < 0.01f)
		{
			GamepadCameraReset();
			return;
		}
		if (gamepadSteeringDirection.sqrMagnitude < 0.01f)
		{
			gamepadSteeringDirection = normalized;
		}
		float vehicleYaw = Mathf.Atan2(normalized.x, normalized.z) * 57.29578f;
		float yaw = Mathf.Atan2(gamepadSteeringDirection.x, gamepadSteeringDirection.z) * 57.29578f;
		float num = currentSpeed * 7.2f;
		float num2 = Mathf.Max(maxSpeedKmh * catchUpMultiplier, 1f);
		float speedRatio = num / num2;
		float turnSpeedMultiplier = gamepad.CameraTurnSpeedUpdate(isDrifting, Time.deltaTime);
		float num3 = gamepad.SteeringYawGet(yaw, vehicleYaw, speedRatio, Time.deltaTime, turnSpeedMultiplier);
		if (isDrifting)
		{
			DriftSteeringOffsetsGet(out var steerOffset, out var yawOffset);
			float yawOffset2 = steerOffset + yawOffset;
			num3 = gamepad.DriftCameraYawGet(yaw, num3, vehicleYaw, yawOffset2, Time.deltaTime);
		}
		yaw = num3;
		gamepadSteeringDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
		Transform overrideTransform = player.localCamera.GetOverrideTransform();
		float look = InputManager.instance.VehicleGamepadLookGet();
		Vector3 val2 = Quaternion.Euler(gamepad.CameraPitchGet(look, InputManager.instance.AimInvertVerticalGet()), yaw, 0f) * Vector3.forward;
		Vector3 position = overrideTransform.position + val2 * 10f;
		CameraAim.Instance.AimTargetSoftSet(position, 0.1f, 12f, 12f, ((Component)this).gameObject, 50);
	}

	internal void AIRiderClaim(EnemyTick _tick)
	{
		aiRiderClaimant = _tick;
		aiRiderClaimTimer = 0.5f;
	}

	internal bool AIRiderClaimedByOther(EnemyTick _tick)
	{
		if (aiRiderClaimTimer <= 0f)
		{
			return false;
		}
		if (Object.op_Implicit((Object)(object)aiRiderClaimant))
		{
			return (Object)(object)aiRiderClaimant != (Object)(object)_tick;
		}
		return false;
	}

	internal void AIOverrideTumbleImmune(float _duration)
	{
		if (_duration > aiTumbleImmuneTimer)
		{
			aiTumbleImmuneTimer = _duration;
		}
	}

	internal void AIOverrideDrift(float _direction, float _duration)
	{
		aiDriftOverrideDirection = _direction;
		if (_duration > aiDriftOverrideTimer)
		{
			aiDriftOverrideTimer = _duration;
		}
	}

	internal void OverrideAIControl(PhysGrabObject _rider, float _throttle, float _steer, Vector3 _forwardReference)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		aiRiderPhysObj = _rider;
		aiControlInputThrottle = Mathf.Clamp(_throttle, -1f, 1f);
		aiControlInputSteer = Mathf.Clamp(_steer, -1f, 1f);
		if (_forwardReference.sqrMagnitude > 0.001f)
		{
			aiControlForwardReference = _forwardReference.normalized;
		}
		aiControlTimer = 0.25f;
	}

	private void AIControlReset()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		aiRiderPhysObj = null;
		aiControlInputThrottle = 0f;
		aiControlInputSteer = 0f;
		aiControlForwardReference = ((Component)this).transform.forward;
		aiDriftOverrideTimer = 0f;
		aiDriftOverrideDirection = 0f;
		driftTimer = 0f;
		driftDirection = 0f;
		driftQueuedWhileAirborne = false;
		if (isDrifting)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetDriftingRPC", (RpcTarget)0, new object[1] { false });
			}
			else
			{
				SetDriftingLocal(_drifting: false);
			}
		}
	}

	internal void AIDriverEjectPlayers()
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		for (int i = 0; i < seats.Length; i++)
		{
			PlayerAvatar seatedPlayer = seats[i].seatedPlayer;
			if (!((Object)(object)seatedPlayer == (Object)null))
			{
				ClearSeat(i);
				if (Object.op_Implicit((Object)(object)seatedPlayer.physGrabber))
				{
					seatedPlayer.physGrabber.OverrideGrabRelease(physGrabObject.photonView.ViewID);
				}
				if (Object.op_Implicit((Object)(object)seatedPlayer.tumble))
				{
					Vector3 val = Vector3.up * 1.2f - ((Component)this).transform.forward * 0.5f + ((Component)this).transform.right * Random.Range(-0.3f, 0.3f);
					Vector3 normalized = val.normalized;
					seatedPlayer.tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
					seatedPlayer.tumble.TumbleForce(normalized * 5f);
					seatedPlayer.tumble.TumbleTorque(-((Component)seatedPlayer).transform.right * 5.5f);
				}
			}
		}
	}

	internal void AIRiderCollisionIgnore(Collider[] _riderColliders)
	{
		if (_riderColliders != null)
		{
			if (aiRiderIgnoredColliders != _riderColliders)
			{
				AIRiderCollisionRestore();
				aiRiderIgnoredColliders = _riderColliders;
				AIRiderCollisionSet(_ignore: true);
			}
			aiRiderIgnoreTimer = 0.5f;
		}
	}

	private void AIRiderCollisionSet(bool _ignore)
	{
		if (aiRiderIgnoredColliders == null)
		{
			return;
		}
		if (aiVehicleAllColliders == null)
		{
			aiVehicleAllColliders = ((Component)this).GetComponentsInChildren<Collider>(true);
		}
		for (int i = 0; i < aiVehicleAllColliders.Length; i++)
		{
			if (!Object.op_Implicit((Object)(object)aiVehicleAllColliders[i]))
			{
				continue;
			}
			for (int j = 0; j < aiRiderIgnoredColliders.Length; j++)
			{
				if (Object.op_Implicit((Object)(object)aiRiderIgnoredColliders[j]))
				{
					Physics.IgnoreCollision(aiVehicleAllColliders[i], aiRiderIgnoredColliders[j], _ignore);
				}
			}
		}
	}

	private void AIRiderCollisionRestore()
	{
		if (aiRiderIgnoredColliders != null)
		{
			AIRiderCollisionSet(_ignore: false);
			aiRiderIgnoredColliders = null;
		}
	}

	private bool AIRiderHasCollider(Collider _collider)
	{
		if (aiRiderIgnoredColliders == null)
		{
			return false;
		}
		for (int i = 0; i < aiRiderIgnoredColliders.Length; i++)
		{
			if ((Object)(object)aiRiderIgnoredColliders[i] == (Object)(object)_collider)
			{
				return true;
			}
		}
		return false;
	}

	private void ApplyAIRiderForces()
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		if (AIControlActive && Object.op_Implicit((Object)(object)aiRiderPhysObj) && Object.op_Implicit((Object)(object)aiRiderPhysObj.rb) && seats != null && seats.Length != 0)
		{
			VehicleSeat vehicleSeat = seats[0];
			Rigidbody val = aiRiderPhysObj.rb;
			aiRiderPhysObj.OverrideZeroGravity(0.2f);
			aiRiderPhysObj.OverrideMass(vehicleSeat.playerMass);
			if (Object.op_Implicit((Object)(object)aiRiderPhysObj.impactDetector))
			{
				aiRiderPhysObj.impactDetector.ImpactDisable(0.2f);
			}
			Vector3 val2 = vehicleSeat.seatTransform.position - val.position;
			if (val2.magnitude > 2f)
			{
				val.position = vehicleSeat.seatTransform.position;
				val.rotation = vehicleSeat.seatTransform.rotation;
				val.linearVelocity = rb.GetPointVelocity(vehicleSeat.seatTransform.position);
				val.angularVelocity = rb.angularVelocity;
			}
			else
			{
				val.linearVelocity = rb.GetPointVelocity(val.position) + val2 / Time.fixedDeltaTime * 0.5f;
				val.angularVelocity = rb.angularVelocity;
				Vector3 val3 = SemiFunc.PhysFollowRotation(((Component)val).transform, vehicleSeat.seatTransform.rotation, val, 1f);
				val.AddTorque(val3, (ForceMode)1);
			}
		}
	}

	private Vector3 FilterUpwardForce(Vector3 force)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (force.magnitude < 0.001f)
		{
			return force;
		}
		float num = Vector3.Angle(force.normalized, Vector3.up);
		if (num >= 45f)
		{
			return force;
		}
		if (num <= 10f)
		{
			return Vector3.zero;
		}
		float num2 = Mathf.InverseLerp(10f, 45f, num);
		return force * num2;
	}

	private bool PlayerPhysicsReady(PlayerAvatar _player)
	{
		if (Object.op_Implicit((Object)(object)_player) && Object.op_Implicit((Object)(object)_player.tumble))
		{
			return Object.op_Implicit((Object)(object)_player.tumble.rb);
		}
		return false;
	}

	public static ItemVehicle GetVehicleForPlayer(PlayerAvatar _player)
	{
		if (!Object.op_Implicit((Object)(object)_player))
		{
			return null;
		}
		for (int i = 0; i < allActiveVehicles.Count; i++)
		{
			ItemVehicle itemVehicle = allActiveVehicles[i];
			if (!Object.op_Implicit((Object)(object)itemVehicle) || itemVehicle.seats == null)
			{
				continue;
			}
			for (int j = 0; j < itemVehicle.seats.Length; j++)
			{
				if ((Object)(object)itemVehicle.seats[j].seatedPlayer == (Object)(object)_player)
				{
					return itemVehicle;
				}
			}
		}
		return null;
	}

	internal bool MountingInProgress(PlayerAvatar player)
	{
		if (!Object.op_Implicit((Object)(object)player) || player.isDisabled || currentState == State.Tumbled)
		{
			return false;
		}
		VehicleSeat[] array = seats;
		foreach (VehicleSeat vehicleSeat in array)
		{
			if ((Object)(object)vehicleSeat.seatedPlayer == (Object)(object)player)
			{
				return vehicleSeat.mountFinishTimer > 0f;
			}
		}
		return false;
	}

	private void MountFinishTick()
	{
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		VehicleSeat[] array = seats;
		foreach (VehicleSeat vehicleSeat in array)
		{
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (!Object.op_Implicit((Object)(object)seatedPlayer) || vehicleSeat.mountFinishTimer <= 0f)
			{
				continue;
			}
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				vehicleSeat.mountFinishTimer = Mathf.Max(0f, vehicleSeat.mountFinishTimer - Time.deltaTime);
			}
			if (vehicleSeat.mountFinishTimer <= 0f && !Object.op_Implicit((Object)(object)ArenaRace.instance) && SemiFunc.IsMasterClientOrSingleplayer())
			{
				if (Object.op_Implicit((Object)(object)seatedPlayer.tumble) && Object.op_Implicit((Object)(object)seatedPlayer.tumble.rb))
				{
					seatedPlayer.tumble.rb.linearVelocity = rb.GetPointVelocity(seatedPlayer.tumble.rb.position);
					seatedPlayer.tumble.rb.angularVelocity = rb.angularVelocity;
				}
				seatedPlayer.physGrabber.OverrideGrabRelease(physGrabObject.photonView.ViewID);
				ClearSeat(Array.IndexOf(seats, vehicleSeat));
			}
			else
			{
				if (seatedPlayer.isDisabled || currentState == State.Tumbled || !PlayerPhysicsReady(seatedPlayer))
				{
					continue;
				}
				Rigidbody val = seatedPlayer.tumble.rb;
				float num = Vector3.Distance(val.position, vehicleSeat.seatTransform.position);
				Vector3 val2 = val.linearVelocity - rb.GetPointVelocity(val.position);
				float magnitude = val2.magnitude;
				float num2 = Quaternion.Angle(val.rotation, vehicleSeat.seatTransform.rotation);
				if ((vehicleSeat.reachedFirstMount || !Object.op_Implicit((Object)(object)vehicleSeat.firstMountTransform)) && num <= 0.15f && magnitude <= 1f && num2 <= 15f)
				{
					vehicleSeat.mountSettleTimer += Time.deltaTime;
					if (vehicleSeat.mountSettleTimer >= 0.1f && SemiFunc.IsMasterClientOrSingleplayer())
					{
						int num3 = Array.IndexOf(seats, vehicleSeat);
						if (SemiFunc.IsMultiplayer())
						{
							photonView.RPC("FinishSeatMountRPC", (RpcTarget)0, new object[2]
							{
								num3,
								seatedPlayer.photonView.ViewID
							});
						}
						else
						{
							FinishSeatMountRPC(num3, seatedPlayer.photonView.ViewID);
						}
						continue;
					}
				}
				else
				{
					vehicleSeat.mountSettleTimer = 0f;
				}
				vehicleSeat.grabbedTimer = Mathf.Max(vehicleSeat.grabbedTimer, 0.5f);
				vehicleSeat.takeASeatTimer = Mathf.Max(vehicleSeat.takeASeatTimer, 0.2f);
				if (seatedPlayer.isLocal)
				{
					PlayerController.instance.tumbleInputDisableTimer = Mathf.Max(PlayerController.instance.tumbleInputDisableTimer, 0.2f);
				}
			}
		}
	}

	public static PlayerAvatar GetSeatedDriverForPhysObject(PhysGrabObject _physObject)
	{
		if (!Object.op_Implicit((Object)(object)_physObject))
		{
			return null;
		}
		for (int i = 0; i < allActiveVehicles.Count; i++)
		{
			ItemVehicle itemVehicle = allActiveVehicles[i];
			if (!Object.op_Implicit((Object)(object)itemVehicle) || (Object)(object)itemVehicle.physGrabObject != (Object)(object)_physObject)
			{
				continue;
			}
			if (itemVehicle.seats == null)
			{
				return null;
			}
			for (int j = 0; j < itemVehicle.seats.Length; j++)
			{
				if ((Object)(object)itemVehicle.seats[j].seatedPlayer != (Object)null)
				{
					return itemVehicle.seats[j].seatedPlayer;
				}
			}
			return null;
		}
		return null;
	}

	private void OnEnable()
	{
		allActiveVehicles.Add(this);
	}

	private void OnDisable()
	{
		GamepadInputReset();
		DrivingRumbleReset();
		allActiveVehicles.Remove(this);
		AIRiderCollisionRestore();
	}

	private void Awake()
	{
		photonView = ((Component)this).GetComponent<PhotonView>();
		if (!Object.op_Implicit((Object)(object)rb))
		{
			rb = ((Component)this).GetComponent<Rigidbody>();
		}
		bumperCarPairCooldowns = new Dictionary<int, float>();
		pitchHistory = new float[50];
		pitchHistoryFillCount = 0;
	}

	private void Start()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0532: Unknown result type (might be due to invalid IL or missing references)
		//IL_0537: Unknown result type (might be due to invalid IL or missing references)
		((MonoBehaviour)this).StartCoroutine(LateStart());
		InitCachedStatics();
		SetState(State.Idle);
		fakeVelocities = ((Component)this).GetComponent<FakeVelocities>();
		nonLocalPhotonTransformView = ((Component)this).GetComponent<PhotonTransformView>();
		sparkLayerMask = SemiFunc.LayerMaskGetVisionObstruct();
		nonLocalMeshPredictionWallLayer = LayerMask.NameToLayer("Default");
		EmissionModule emission;
		MinMaxCurve rateOverTime;
		MainModule main;
		if (Object.op_Implicit((Object)(object)sparkParticlesLeft))
		{
			sparkLeftAllParticles = ((Component)sparkParticlesLeft).GetComponentsInChildren<ParticleSystem>();
			sparkLeftDefaultEmissions = new float[sparkLeftAllParticles.Length];
			sparkLeftDefaultMaxParticles = new int[sparkLeftAllParticles.Length];
			for (int i = 0; i < sparkLeftAllParticles.Length; i++)
			{
				float[] array = sparkLeftDefaultEmissions;
				int num = i;
				emission = sparkLeftAllParticles[i].emission;
				rateOverTime = emission.rateOverTime;
				array[num] = rateOverTime.constant;
				int[] array2 = sparkLeftDefaultMaxParticles;
				int num2 = i;
				main = sparkLeftAllParticles[i].main;
				array2[num2] = main.maxParticles;
			}
		}
		if (Object.op_Implicit((Object)(object)sparkParticlesRight))
		{
			sparkRightAllParticles = ((Component)sparkParticlesRight).GetComponentsInChildren<ParticleSystem>();
			sparkRightDefaultEmissions = new float[sparkRightAllParticles.Length];
			sparkRightDefaultMaxParticles = new int[sparkRightAllParticles.Length];
			for (int j = 0; j < sparkRightAllParticles.Length; j++)
			{
				float[] array3 = sparkRightDefaultEmissions;
				int num3 = j;
				emission = sparkRightAllParticles[j].emission;
				rateOverTime = emission.rateOverTime;
				array3[num3] = rateOverTime.constant;
				int[] array4 = sparkRightDefaultMaxParticles;
				int num4 = j;
				main = sparkRightAllParticles[j].main;
				array4[num4] = main.maxParticles;
			}
		}
		itemBattery = ((Component)this).GetComponent<ItemBattery>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		grabArea = ((Component)this).GetComponent<PhysGrabObjectGrabArea>();
		explosionScript = ((Component)this).GetComponent<ParticleScriptExplosion>();
		originalMaxForwardSpeed = maxForwardSpeed;
		originalMaxReverseSpeed = maxReverseSpeed;
		originalHyperMaxSpeed = hyperMaxSpeed;
		originalDriftMaxSpeed = driftMaxSpeed;
		originalForwardAcceleration = forwardAcceleration;
		originalHyperAcceleration = hyperAcceleration;
		originalSteeringSpeed = steeringSpeed;
		originalYawSpringStrength = yawSpringStrength;
		originalDriftForce = driftForce;
		originalDriftExitBoost = driftExitBoost;
		originalDriftBoostLevel1ForwardBoost = driftBoostLevel1ForwardBoost;
		originalDriftBoostLevel2ForwardBoost = driftBoostLevel2ForwardBoost;
		originalDriftBoostLevel3ForwardBoost = driftBoostLevel3ForwardBoost;
		originalDriftDrag = driftDrag;
		originalDriftLowSpeedDrag = driftLowSpeedDrag;
		if (Object.op_Implicit((Object)(object)steeringTransform))
		{
			steeringOriginalRotation = steeringTransform.localRotation;
			steeringSpring = new SpringQuaternion();
			steeringSpring.speed = steeringSpeed;
			steeringSpring.damping = steeringDamping;
			steeringSpring.maxAngle = 80f;
			steeringSpring.clamp = true;
		}
		prevBatteryLife = (Object.op_Implicit((Object)(object)itemBattery) ? itemBattery.batteryLife : 100f);
		InitializeDestructionStates();
		smoothDragCurrent = driftDragDefault;
		smoothDragTarget = driftDragDefault;
		smoothAngularDragCurrent = (Object.op_Implicit((Object)(object)rb) ? rb.angularDamping : 5f);
		smoothAngularDragTarget = smoothAngularDragCurrent;
		if (Object.op_Implicit((Object)(object)meshTransform))
		{
			meshOriginalParent = meshTransform.parent;
			meshOriginalLocalPosition = meshTransform.localPosition;
			meshOriginalLocalRotation = meshTransform.localRotation;
			meshPreviousPosition = meshOriginalParent.position;
			meshPreviousRotation = meshOriginalParent.rotation;
		}
		if (Object.op_Implicit((Object)(object)grabMesh))
		{
			grabMaterial = ((Renderer)grabMesh).material;
		}
		Renderer[] array5 = new Renderer[3] { hoverEffectRenderer, hoverEffectFrontRenderer, hoverEffectBackRenderer };
		List<Material> list = new List<Material>();
		List<Transform> list2 = new List<Transform>();
		for (int k = 0; k < array5.Length; k++)
		{
			if (Object.op_Implicit((Object)(object)array5[k]))
			{
				list.Add(array5[k].material);
				list2.Add(((Component)array5[k]).transform);
			}
		}
		hoverEffectMaterials = list.ToArray();
		hoverEffectTransforms = list2.ToArray();
		hoverEffectNoiseSeeds = new float[hoverEffectTransforms.Length];
		hoverEffectOriginalScales = new Vector3[hoverEffectTransforms.Length];
		for (int l = 0; l < hoverEffectTransforms.Length; l++)
		{
			hoverEffectNoiseSeeds[l] = Random.Range(0f, 1000f);
			hoverEffectOriginalScales[l] = hoverEffectTransforms[l].localScale;
		}
		if (Object.op_Implicit((Object)(object)frontLightRenderer))
		{
			cachedFrontLightMaterial = frontLightRenderer.material;
		}
		if (Object.op_Implicit((Object)(object)backLightRenderer))
		{
			cachedBackLightMaterial = backLightRenderer.material;
		}
		int num5 = Mathf.CeilToInt(9f) + 1;
		impactVelocityHistory = new float[num5];
		impactVelocityDirectionHistory = new Vector3[num5];
		lastPositionForDistanceTracking = ((Component)this).transform.position;
		if (Object.op_Implicit((Object)(object)impactTransform))
		{
			impactTransform.SetParent((Transform)null);
		}
	}

	private IEnumerator LateStart()
	{
		while (!LevelGenerator.Instance.Generated)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		if (Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			fallDamageDisabled = true;
			ItemAttributes component = ((Component)this).GetComponent<ItemAttributes>();
			if (Object.op_Implicit((Object)(object)component))
			{
				component.disableUI = true;
			}
			if (Object.op_Implicit((Object)(object)itemBattery))
			{
				itemBattery.suppressBatteryUI = true;
			}
		}
	}

	private void OnDestroy()
	{
		if (Object.op_Implicit((Object)(object)impactTransform))
		{
			Object.Destroy((Object)(object)((Component)impactTransform).gameObject);
		}
		if (Object.op_Implicit((Object)(object)meshTransform) && meshDeparented)
		{
			Object.Destroy((Object)(object)((Component)meshTransform).gameObject);
		}
		if (seats == null)
		{
			return;
		}
		for (int i = 0; i < seats.Length; i++)
		{
			if (seats[i] != null && Object.op_Implicit((Object)(object)seats[i].seatedPlayer))
			{
				seats[i].seatedPlayer.playerAvatarVisuals.MountSeatVisualClear();
			}
		}
	}

	private void UpdateCatchUpMultiplier()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)ArenaRace.instance) || ArenaRace.instance.currentState != ArenaRace.States.RaceActive || !ArenaRace.instance.crownActivated)
		{
			catchUpMultiplier = 1f;
			draftingActiveTimer = 0f;
			return;
		}
		Vector3 crownCurrentPosition = ArenaRace.instance.crownCurrentPosition;
		float num = Vector2.Distance(new Vector2(((Component)this).transform.position.x, ((Component)this).transform.position.z), new Vector2(crownCurrentPosition.x, crownCurrentPosition.z));
		float num2 = Mathf.Lerp(1f, 1.2f, Mathf.InverseLerp(10f, 20f, num));
		draftingRaycastTimer -= Time.deltaTime;
		if (draftingRaycastTimer <= 0f)
		{
			draftingRaycastTimer = 0.2f;
			RaycastHit val = default;
			if (Physics.Raycast(((Component)this).transform.position, ((Component)this).transform.forward, ref val, 8f))
			{
				ItemVehicle componentInParent = ((Component)val.collider).GetComponentInParent<ItemVehicle>();
				if (Object.op_Implicit((Object)(object)componentInParent) && (Object)(object)componentInParent != (Object)(object)this)
				{
					draftingActiveTimer = 0.5f;
				}
			}
		}
		if (draftingActiveTimer > 0f)
		{
			draftingActiveTimer -= Time.deltaTime;
			catchUpMultiplier = Mathf.MoveTowards(catchUpMultiplier, 1.2f, 1f / 15f * Time.deltaTime);
		}
		else if (catchUpMultiplier > num2)
		{
			catchUpMultiplier = Mathf.MoveTowards(catchUpMultiplier, num2, 1f / 15f * Time.deltaTime);
		}
		else
		{
			catchUpMultiplier = num2;
		}
	}

	private void ApplyTuningMultipliers()
	{
		UpdateCatchUpMultiplier();
		int num;
		float num2;
		if (isRacingVehicle && !Object.op_Implicit((Object)(object)ArenaRace.instance) && !AIControlActive)
		{
			num = ((!AITumbleImmune) ? 1 : 0);
			if (num != 0)
			{
				num2 = speedWhenNotInARace;
				goto IL_003e;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 1f;
		goto IL_003e;
		IL_003e:
		float num3 = num2;
		if (num != 0 && speedWhenNotInARace > 0.0001f)
		{
			_ = 1f / speedWhenNotInARace;
		}
		racingOutsideRaceForceCompensation = ((num3 > 0.0001f) ? (1f / num3) : 1f);
		maxForwardSpeed = originalMaxForwardSpeed * tuningSpeedMultiplier * catchUpMultiplier * num3;
		maxReverseSpeed = originalMaxReverseSpeed * tuningSpeedMultiplier * num3;
		hyperMaxSpeed = originalHyperMaxSpeed * tuningSpeedMultiplier * catchUpMultiplier * num3;
		driftMaxSpeed = originalDriftMaxSpeed * tuningSpeedMultiplier * catchUpMultiplier * num3;
		forwardAcceleration = originalForwardAcceleration * tuningAccelerationMultiplier * catchUpMultiplier;
		hyperAcceleration = originalHyperAcceleration * tuningAccelerationMultiplier * catchUpMultiplier;
		steeringSpeed = originalSteeringSpeed * tuningSteeringMultiplier;
		yawSpringStrength = originalYawSpringStrength * tuningYawMultiplier;
		driftForce = originalDriftForce * tuningSpeedMultiplier * catchUpMultiplier * num3;
		driftExitBoost = originalDriftExitBoost * tuningDriftBoostMultiplier * catchUpMultiplier * num3;
		driftBoostLevel1ForwardBoost = originalDriftBoostLevel1ForwardBoost * tuningDriftBoostMultiplier * catchUpMultiplier * num3;
		driftBoostLevel2ForwardBoost = originalDriftBoostLevel2ForwardBoost * tuningDriftBoostMultiplier * catchUpMultiplier * num3;
		driftBoostLevel3ForwardBoost = originalDriftBoostLevel3ForwardBoost * tuningDriftBoostMultiplier * catchUpMultiplier * num3;
		driftDrag = originalDriftDrag;
		driftLowSpeedDrag = originalDriftLowSpeedDrag;
	}

	private void Update()
	{
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0580: Unknown result type (might be due to invalid IL or missing references)
		//IL_0585: Unknown result type (might be due to invalid IL or missing references)
		//IL_05aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_05af: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_08ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a53: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a74: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b5e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b7d: Unknown result type (might be due to invalid IL or missing references)
		//IL_11a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_11ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_11b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_11e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_11eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_11ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_1204: Unknown result type (might be due to invalid IL or missing references)
		//IL_11fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dfb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e07: Unknown result type (might be due to invalid IL or missing references)
		//IL_122b: Unknown result type (might be due to invalid IL or missing references)
		//IL_1232: Unknown result type (might be due to invalid IL or missing references)
		//IL_123b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efd: Unknown result type (might be due to invalid IL or missing references)
		MountFinishTick();
		NetworkExtrapolateTick();
		GamepadInputUpdate();
		if (Object.op_Implicit((Object)(object)Driver) && Driver.isLocal)
		{
			GamepadCameraUpdate(Driver);
		}
		LocalMeshUpdate();
		GrabberVisualOverrideAllSeated();
		if (Time.time > componentCacheClearTimer)
		{
			componentCache.Clear();
			componentCacheClearTimer = Time.time + 5f;
		}
		if (physGrabObject.grabbed)
		{
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				item.OverrideDisableGrabLookAt(0.2f);
			}
			physGrabObject.OverrideKnockOutOfGrabDisable(0.2f);
		}
		ApplyTuningMultipliers();
		if (aiControlTimer > 0f)
		{
			aiControlTimer -= Time.deltaTime;
			if (aiControlTimer <= 0f)
			{
				AIControlReset();
			}
		}
		if (aiRiderClaimTimer > 0f)
		{
			aiRiderClaimTimer -= Time.deltaTime;
			if (aiRiderClaimTimer <= 0f)
			{
				aiRiderClaimant = null;
			}
		}
		if (aiTumbleImmuneTimer > 0f)
		{
			aiTumbleImmuneTimer -= Time.deltaTime;
		}
		if (aiDriftOverrideTimer > 0f)
		{
			aiDriftOverrideTimer -= Time.deltaTime;
		}
		if (aiRiderIgnoreTimer > 0f)
		{
			aiRiderIgnoreTimer -= Time.deltaTime;
			if (aiRiderIgnoreTimer <= 0f)
			{
				AIRiderCollisionRestore();
			}
		}
		bool flag = false;
		for (int i = 0; i < seats.Length; i++)
		{
			VehicleSeat vehicleSeat = seats[i];
			if (Object.op_Implicit((Object)(object)ArenaRace.instance) && (Object)(object)vehicleSeat.seatedPlayer != (Object)null)
			{
				vehicleSeat.grabbedTimer = 1f;
				flag = true;
			}
			else if (vehicleSeat.grabbedTimer > 0f)
			{
				vehicleSeat.grabbedTimer -= Time.deltaTime;
				flag = true;
			}
			else if ((Object)(object)vehicleSeat.seatedPlayer != (Object)null && SemiFunc.IsMasterClientOrSingleplayer())
			{
				ClearSeat(i);
			}
			if (vehicleSeat.takeASeatTimer > 0f)
			{
				vehicleSeat.takeASeatTimer -= Time.deltaTime;
			}
		}
		hasAnySeatedPlayerCached = false;
		localPlayerIsMounted = false;
		for (int j = 0; j < seats.Length; j++)
		{
			if ((Object)(object)seats[j].seatedPlayer != (Object)null)
			{
				hasAnySeatedPlayerCached = true;
				if (seats[j].seatedPlayer.isLocal)
				{
					localPlayerIsMounted = true;
				}
			}
		}
		soundEffectVolume = (localPlayerIsMounted ? soundEffectVolumeForMountedPlayer : soundEffectVolumeForOthers);
		if (!hasAnySeatedPlayerCached && !AIControlActive && !physGrabObject.grabbed)
		{
			if ((Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f) < 1f)
			{
				dismountLowSpeedTimer += Time.deltaTime;
				if (dismountLowSpeedTimer >= 1f && !dismountedDefaultMaterial)
				{
					physGrabObject.OverrideMaterial(SemiFunc.PhysicMaterialDefault(), 0.2f);
					dismountedDefaultMaterial = true;
				}
			}
			else
			{
				dismountLowSpeedTimer = 0f;
			}
			Vector3 val = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity : Vector3.zero);
			Vector3 val2 = new Vector3(val.x, 0f, val.z);
			float magnitude = val2.magnitude;
			if (!unmountedFrictionActive)
			{
				if (unmountedFrictionWaitingForSlow)
				{
					if (magnitude < 1f)
					{
						unmountedFrictionActive = true;
						unmountedFrictionWaitingForSlow = false;
					}
				}
				else if (magnitude < 4f)
				{
					unmountedFrictionActive = true;
				}
				else
				{
					unmountedFrictionWaitingForSlow = true;
				}
			}
			if (unmountedFrictionActive)
			{
				unmountedFrictionEaseTimer = Mathf.Min(unmountedFrictionEaseTimer + Time.deltaTime, 1f);
			}
		}
		else
		{
			dismountLowSpeedTimer = 0f;
			dismountedDefaultMaterial = false;
			unmountedFrictionActive = false;
			unmountedFrictionWaitingForSlow = false;
			unmountedFrictionEaseTimer = 0f;
		}
		bool flag2 = Object.op_Implicit((Object)(object)itemBattery) && itemBattery.batteryLife <= 0f && fireWarningTimer <= 0f;
		if (currentState != State.Tumbled)
		{
			if ((flag || AIControlActive || AITumbleImmune) && !flag2)
			{
				SetState(State.Active);
			}
			else
			{
				SetState(State.Idle);
			}
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState == State.Active && !AITumbleImmune)
		{
			if (Vector3.Dot(((Component)this).transform.up, VehicleUp) < 0f)
			{
				upsideDownTimer += Time.deltaTime;
				if (upsideDownTimer >= upsideDownTumbleThreshold)
				{
					TumbleVehicle(upsideDownTumbleDuration);
					upsideDownTimer = 0f;
				}
			}
			else
			{
				upsideDownTimer = 0f;
			}
		}
		else
		{
			upsideDownTimer = 0f;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState == State.Active && Object.op_Implicit((Object)(object)rb) && !isDrifting && !AIControlActive && !AITumbleImmune)
		{
			Vector3 linearVelocity = rb.linearVelocity;
			linearVelocity.y = 0f;
			if (linearVelocity.magnitude > 5f)
			{
				Vector3 forward = ((Component)this).transform.forward;
				forward.y = 0f;
				forward.Normalize();
				if (Vector3.Dot(linearVelocity.normalized, forward) < 0.2f)
				{
					velocityMismatchTimer += Time.deltaTime;
					if (velocityMismatchTimer >= 0.5f)
					{
						TumbleVehicle(Random.Range(1f, 2f));
						velocityMismatchTimer = 0f;
					}
				}
				else
				{
					velocityMismatchTimer = 0f;
				}
			}
			else
			{
				velocityMismatchTimer = 0f;
			}
		}
		else
		{
			velocityMismatchTimer = 0f;
		}
		UpdateDestructionState();
		UpdateValuableBoxExplosion();
		DamageParticlesTick();
		UpdateSparks();
		UpdateHurtColliderTimer();
		UpdateEnemyHitCooldownTimers();
		if (stateTimer <= stateTimerMax)
		{
			stateTimer += Time.deltaTime;
		}
		if (lookAtTimer > 0f)
		{
			lookAtTimer -= Time.deltaTime;
		}
		if (vehicleExistenceTimer < 3f)
		{
			vehicleExistenceTimer += Time.deltaTime;
		}
		StateMachine();
		DrivingRumbleUpdate();
		LoopFailsafe();
		Vector3 linearVelocity2 = rb.linearVelocity;
		linearVelocity2.y = 0f;
		float num = maxForwardSpeed + hyperMaxSpeed;
		float num2 = Mathf.Clamp01(linearVelocity2.magnitude / num);
		float num3 = Mathf.Lerp(minEnginePitch, maxEnginePitch, num2);
		float num4 = 0f;
		float num5 = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.y : 0f);
		if (num5 > 0f && velocityPitchUpForce > 0f)
		{
			num4 += Mathf.Clamp(num5 / velocityPitchUpForce, 0f, 1f) * velocityPitchUpMax;
		}
		else if (num5 < 0f && velocityPitchDownForce > 0f)
		{
			num4 -= Mathf.Clamp((0f - num5) / velocityPitchDownForce, 0f, 1f) * velocityPitchDownMax;
		}
		if (velocityPitchYawForce > 0f)
		{
			float num6 = (Object.op_Implicit((Object)(object)fakeVelocities) ? Mathf.Abs(fakeVelocities.angularVelocity.y) : 0f);
			num4 += Mathf.Clamp(num6 / velocityPitchYawForce, 0f, 1f) * velocityPitchYawMax;
		}
		velocityPitchAudioCurrent = Mathf.SmoothDamp(velocityPitchAudioCurrent, num4, ref velocityPitchAudioVelocity, velocityPitchSmoothTime);
		num3 += velocityPitchAudioCurrent;
		audioEngineLoop.PlayLoop(currentState == State.Active, 2f, 2f, num3, soundEffectVolume);
		audioEngineLoop.Source.dopplerLevel = (localPlayerIsMounted ? 0f : audioEngineLoop.Doppler);
		if (currentState == State.Active && SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.PerSecond(1f, (Object)(object)this))
		{
			float range = Mathf.Lerp(0.5f, 16f, num2);
			SemiFunc.EnemyInvestigate(((Component)this).transform.position, range);
		}
		hyperSpeedLoop.PlayLoop(goingFastParticlesPlaying, 2f, 2f, 1f, soundEffectVolume);
		hyperSpeedLoop.Source.dopplerLevel = (localPlayerIsMounted ? 0f : hyperSpeedLoop.Doppler);
		float num7 = (Object.op_Implicit((Object)(object)fakeVelocities) ? Mathf.Abs(fakeVelocities.angularVelocity.y) : 0f);
		bool playing = num7 > 0.5f && currentState == State.Active;
		float num8 = Mathf.Lerp(1f, 2f, Mathf.Clamp01(num7 / 5f));
		smoothTurnPitch = Mathf.Lerp(smoothTurnPitch, num8, Time.deltaTime * 5f);
		turnLoopSound.PlayLoop(playing, 1f, 0.5f, smoothTurnPitch, soundEffectVolume);
		turnLoopSound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : turnLoopSound.Doppler);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			bool flag3 = continuousAirborneTimer >= 0.5f;
			if (flag3 != airborneWindActive)
			{
				airborneWindActive = flag3;
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("AirborneWindRPC", (RpcTarget)1, new object[1] { airborneWindActive });
				}
			}
		}
		float num9 = ((((Component)this).transform.forward.y >= 0f) ? Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(((Component)this).transform.forward.y / 0.7f)) : Mathf.Lerp(0.75f, 1.65f, Mathf.Clamp01((0f - ((Component)this).transform.forward.y) / 0.7f)));
		airborneWindPitch = Mathf.SmoothDamp(airborneWindPitch, num9, ref airborneWindPitchVelocity, 0.3f);
		airborneWindLoop.PlayLoop(airborneWindActive, 1f, 0.1f, airborneWindPitch, soundEffectVolume);
		airborneWindLoop.Source.dopplerLevel = (localPlayerIsMounted ? 0f : airborneWindLoop.Doppler);
		if (hasDrifting)
		{
			float pitchMultiplier = Mathf.Lerp(driftLoopMinPitch, driftLoopMaxPitch, driftIntensity / 10f);
			Vector3 val3 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
			float num10 = Mathf.Clamp01(val3.magnitude / (maxForwardSpeed + hyperMaxSpeed));
			float volumeMultiplier = soundEffectVolume * Mathf.Lerp(0.1f, 1f, num10);
			driftLoopSound.PlayLoop(isDrifting, 2f, 2f, pitchMultiplier, volumeMultiplier);
			driftLoopSound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : driftLoopSound.Doppler);
			driftLoopLevel1Sound.PlayLoop(isDrifting && driftBoostLevel == 1, 2f, 2f, pitchMultiplier, volumeMultiplier);
			driftLoopLevel1Sound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : driftLoopLevel1Sound.Doppler);
			driftLoopLevel2Sound.PlayLoop(isDrifting && driftBoostLevel == 2, 2f, 2f, pitchMultiplier, volumeMultiplier);
			driftLoopLevel2Sound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : driftLoopLevel2Sound.Doppler);
			driftLoopLevel3Sound.PlayLoop(isDrifting && driftBoostLevel == 3, 2f, 2f, pitchMultiplier, volumeMultiplier);
			driftLoopLevel3Sound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : driftLoopLevel3Sound.Doppler);
			if (Object.op_Implicit((Object)(object)driftLight) && this.driftLightSettings != null && this.driftLightSettings.Length != 0)
			{
				if (driftLightFadingIn)
				{
					driftLightLerp = Mathf.MoveTowards(driftLightLerp, 1f, Time.deltaTime / 0.2f);
				}
				else
				{
					driftLightLerp = Mathf.MoveTowards(driftLightLerp, 0f, Time.deltaTime / 0.2f);
					if (driftLightLerp <= 0f)
					{
						((Behaviour)driftLight).enabled = false;
					}
				}
				int num11 = Mathf.Clamp(driftBoostLevel, 0, this.driftLightSettings.Length - 1);
				DriftLightSettings driftLightSettings = this.driftLightSettings[num11];
				if (num11 != driftLightCurrentLevel)
				{
					driftLightCurrentLevel = num11;
					driftLightCurrentIntensity = driftLightSettings.intensity * 4f;
				}
				float num12 = 1f - Mathf.Exp(-10f * Time.deltaTime);
				driftLightCurrentColor = Color.Lerp(driftLightCurrentColor, driftLightSettings.color, num12);
				driftLightCurrentIntensity = Mathf.Lerp(driftLightCurrentIntensity, driftLightSettings.intensity, num12);
				driftLightCurrentFlickerAmount = Mathf.Lerp(driftLightCurrentFlickerAmount, driftLightSettings.flickerAmount, num12);
				driftLightCurrentFlickerSpeed = Mathf.Lerp(driftLightCurrentFlickerSpeed, driftLightSettings.flickerSpeed, num12);
				driftLightCurrentPulseIntensity = Mathf.Lerp(driftLightCurrentPulseIntensity, driftLightSettings.pulseIntensity, num12);
				driftLightCurrentPulseSpeed = Mathf.Lerp(driftLightCurrentPulseSpeed, driftLightSettings.pulseSpeed, num12);
				if (((Behaviour)driftLight).enabled)
				{
					float num13 = Mathf.PerlinNoise(Time.time * driftLightCurrentFlickerSpeed, 0f) * driftLightCurrentFlickerAmount;
					float num14 = Mathf.Sin(Time.time * driftLightCurrentPulseSpeed) * driftLightCurrentPulseIntensity;
					float num15 = driftLightCurrentIntensity + num13 + num14;
					driftLight.intensity = num15 * driftLightLerp;
					driftLight.color = driftLightCurrentColor;
				}
			}
			if (this.driftLevelSettings != null && this.driftLevelSettings.Length != 0)
			{
				int num16 = Mathf.Clamp(driftBoostLevel, 0, this.driftLevelSettings.Length - 1);
				DriftLevelSettings driftLevelSettings = this.driftLevelSettings[num16];
				float num17 = (isDrifting ? 12.5f : 20f);
				float num18 = 1f - Mathf.Exp((0f - num17) * Time.deltaTime);
				float num19 = (isDrifting ? driftLevelSettings.rollAngle : 0f);
				float num20 = (isDrifting ? driftLevelSettings.yawOffset : 0f);
				driftCurrentRollAngle = Mathf.Lerp(driftCurrentRollAngle, num19, num18);
				driftCurrentYawOffset = Mathf.Lerp(driftCurrentYawOffset, num20, num18);
				float num21 = (isDrifting ? 400f : 600f);
				float num22 = (isDrifting ? 30f : 40f);
				int num23 = Mathf.CeilToInt(Time.deltaTime / 0.02f);
				if (num23 < 1)
				{
					num23 = 1;
				}
				float num24 = Time.deltaTime / (float)num23;
				float num25 = (isDrifting ? driftLevelSettings.meshHorizontalOffset : 0f);
				float num26 = (isDrifting ? driftLevelSettings.meshYRotationOffset : 0f);
				float num27 = (isDrifting ? driftLevelSettings.meshDownOffset : 0f);
				float num28 = (isDrifting ? driftLevelSettings.meshForwardOffset : 0f);
				for (int k = 0; k < num23; k++)
				{
					float num29 = num25 - driftCurrentMeshHorizontalOffset;
					driftMeshHorizontalVelocity += (num29 * num21 - driftMeshHorizontalVelocity * num22) * num24;
					driftCurrentMeshHorizontalOffset += driftMeshHorizontalVelocity * num24;
					float num30 = num26 - driftCurrentMeshYRotationOffset;
					driftMeshYRotationVelocity += (num30 * num21 - driftMeshYRotationVelocity * num22) * num24;
					driftCurrentMeshYRotationOffset += driftMeshYRotationVelocity * num24;
					float num31 = num27 - driftCurrentMeshDownOffset;
					driftMeshDownVelocity += (num31 * num21 - driftMeshDownVelocity * num22) * num24;
					driftCurrentMeshDownOffset += driftMeshDownVelocity * num24;
					float num32 = num28 - driftCurrentMeshForwardOffset;
					driftMeshForwardVelocity += (num32 * num21 - driftMeshForwardVelocity * num22) * num24;
					driftCurrentMeshForwardOffset += driftMeshForwardVelocity * num24;
				}
			}
			if (Object.op_Implicit((Object)(object)driftingParticlesTransform))
			{
				float num33 = Mathf.Sign(Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.forward));
				float num34 = (isDrifting ? (70f * num33) : 0f);
				Vector3 localEulerAngles = driftingParticlesTransform.localEulerAngles;
				float num35 = Mathf.MoveTowards((localEulerAngles.z > 180f) ? (localEulerAngles.z - 360f) : localEulerAngles.z, num34, Time.deltaTime * 360f);
				driftingParticlesTransform.localEulerAngles = new Vector3(localEulerAngles.x, localEulerAngles.y, num35);
			}
			if (Object.op_Implicit((Object)(object)boostLight) && boostLightTimer > 0f)
			{
				boostLightTimer -= Time.deltaTime;
				float num36 = 1f - boostLightTimer / boostLightDuration;
				float num37 = boostLightCurve.Evaluate(num36);
				boostLight.intensity = boostLightBaseIntensity * num37;
				if (boostLightTimer <= 0f)
				{
					((Behaviour)boostLight).enabled = false;
				}
			}
			if (SemiFunc.IsMasterClientOrSingleplayer() && isDrifting && !AIControlActive)
			{
				bool flag4 = (Object)(object)Driver != (Object)null && Mathf.Abs(ControlInputX) > 0.1f;
				if ((Object)(object)Driver == (Object)null || !flag4)
				{
					if (SemiFunc.IsMultiplayer())
					{
						photonView.RPC("SetDriftingRPC", (RpcTarget)0, new object[1] { false });
					}
					else
					{
						SetDriftingLocal(_drifting: false);
					}
				}
				else if ((Object)(object)Driver != (Object)null)
				{
					SemiFunc.PlayerShopKeeperDriftingHost(Driver);
				}
			}
		}
		if (honkCooldownTimer > 0f)
		{
			honkCooldownTimer -= Time.deltaTime;
		}
		if (localSeatIndex >= 0 && honkCooldownTimer <= 0f && SemiFunc.InputDown(InputKey.Interact))
		{
			honkCooldownTimer = 0.1f;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("HonkRPC", (RpcTarget)0, new object[1] { localSeatIndex });
			}
			else
			{
				PlayHonk(localSeatIndex);
			}
		}
		UpdateHonkPitch();
		float num38 = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
		int num39;
		if (currentState == State.Idle)
		{
			num39 = ((num38 <= 0.5f) ? 1 : 0);
			if (num39 != 0)
			{
				goto IL_1438;
			}
		}
		else
		{
			num39 = 0;
		}
		UpdateSteering();
		goto IL_1438;
		IL_1438:
		UpdateLights();
		UpdateGrabVisual();
		UpdateHoverEffect();
		UpdateImpactDetection();
		UpdateImpactDistanceTracking();
		if (num39 == 0)
		{
			UpdateEnemyImpactBoxCheck();
		}
		if (num39 == 0)
		{
			UpdateBumperCarCollisions();
		}
		TumbleEffects();
		UpdateVehicleUI();
	}

	private void LoopFailsafe()
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || (currentState == State.Active && Object.op_Implicit((Object)(object)rb) && !rb.isKinematic))
		{
			return;
		}
		continuousAirborneTimer = 0f;
		if (goingFastParticlesPlaying)
		{
			goingFastParticlesPlaying = false;
			goingFastParticlesCooldown = 0.5f;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("GoingFastParticlesRPC", (RpcTarget)0, new object[1] { false });
			}
			else
			{
				GoingFastParticlesRPC(_play: false);
			}
		}
	}

	private void LateUpdate()
	{
		NonLocalMeshPredictionUpdate();
		GrabberVisualOverrideAllSeated();
		EnforcePhotosensitivity();
	}

	private void GrabberVisualOverrideAllSeated()
	{
		if (seats == null)
		{
			return;
		}
		for (int i = 0; i < seats.Length; i++)
		{
			VehicleSeat vehicleSeat = seats[i];
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (!((Object)(object)seatedPlayer == (Object)null) && !((Object)(object)vehicleSeat.grabPointVisual == (Object)null) && Object.op_Implicit((Object)(object)seatedPlayer.physGrabber))
			{
				seatedPlayer.physGrabber.OverrideGrabberVisualPosition(vehicleSeat.grabPointVisual);
				if (seatedPlayer.isLocal)
				{
					seatedPlayer.physGrabber.OverrideDisableRotationControls();
				}
			}
		}
	}

	private void FixedUpdate()
	{
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
		//IL_034a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Active)
		{
			FixedUpdateActive();
			return;
		}
		speedOverTime = Mathf.Max(0f, speedOverTime - Time.fixedDeltaTime * deceleration * 2f);
		hyperSpeedOverTime = Mathf.Max(0f, hyperSpeedOverTime - Time.fixedDeltaTime * deceleration * 2f);
		if (currentState == State.Tumbled && Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			ApplySeatedPlayerForces();
			if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb))
			{
				if (arenaRaceTumbleYawPending)
				{
					arenaRaceTumbleYawPending = false;
					float num = Random.Range(10f, 20f) * ((Random.value < 0.5f) ? (-1f) : 1f);
					rb.AddTorque(VehicleUp * num, (ForceMode)1);
				}
				float num2 = Vector3.Angle(((Component)this).transform.up, VehicleUp);
				float num3 = 1f - Mathf.InverseLerp(90f, 180f, num2);
				float num4 = Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.forward) * 0.5f * num3;
				float num5 = (0f - rb.angularVelocity.z) * 2f * num3;
				VehicleAddTorque(((Component)this).transform.forward * (num4 + num5), (ForceMode)0);
				Vector3 linearVelocity = rb.linearVelocity;
				if (linearVelocity.magnitude > 20f)
				{
					Rigidbody val = rb;
					linearVelocity = rb.linearVelocity;
					val.linearVelocity = linearVelocity.normalized * 20f;
				}
			}
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb) && physGrabObject.grabbed && !hasAnySeatedPlayerCached && (currentState != State.Tumbled || !Object.op_Implicit((Object)(object)ArenaRace.instance)))
		{
			float num6 = Vector3.Angle(((Component)this).transform.up, VehicleUp);
			float num7 = 1f - Mathf.InverseLerp(90f, 180f, num6);
			float num8 = Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.forward) * 0.5f * num7;
			float num9 = (0f - rb.angularVelocity.z) * 2f * num7;
			VehicleAddTorque(((Component)this).transform.forward * (num8 + num9), (ForceMode)0);
			float num10 = Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.right) * 0.5f * num7;
			float num11 = (0f - rb.angularVelocity.x) * 2f * num7;
			VehicleAddTorque(((Component)this).transform.right * (num10 + num11), (ForceMode)0);
		}
		if (!Object.op_Implicit((Object)(object)rb) || currentState != State.Idle || isVehicleAirborne || physGrabObject.grabbed)
		{
			return;
		}
		Vector3 linearVelocity2 = rb.linearVelocity;
		Vector3 val2 = new Vector3(linearVelocity2.x, 0f, linearVelocity2.z);
		float magnitude = val2.magnitude;
		if ((unmountedFrictionActive || magnitude < 1f) && magnitude > 0.01f)
		{
			if (!unmountedFrictionActive)
			{
				unmountedFrictionActive = true;
				unmountedFrictionEaseTimer = 0f;
			}
			float num12 = Mathf.Clamp01(unmountedFrictionEaseTimer);
			float num13 = Mathf.Lerp(2f, 10f, num12);
			val2 = Vector3.MoveTowards(val2, Vector3.zero, num13 * Time.fixedDeltaTime);
			rb.linearVelocity = new Vector3(val2.x, linearVelocity2.y, val2.z);
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Idle:
			StateIdle();
			break;
		case State.Active:
			StateActive();
			break;
		case State.Tumbled:
			StateTumbled();
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
		if (physGrabObject.grabbed && !hasAnySeatedPlayerCached)
		{
			physGrabObject.OverrideMaterial(SemiFunc.PhysicMaterialDefault());
			dismountLowSpeedTimer = 0f;
			dismountedDefaultMaterial = false;
			unmountedFrictionActive = false;
			unmountedFrictionWaitingForSlow = false;
			unmountedFrictionEaseTimer = 0f;
		}
	}

	private void StateActive()
	{
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateTimerMax = 999f;
			stateTimer = 0f;
			stateStart = false;
		}
		for (int i = 0; i < seats.Length; i++)
		{
			VehicleSeat vehicleSeat = seats[i];
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (!Object.op_Implicit((Object)(object)seatedPlayer))
			{
				continue;
			}
			if (seatedPlayer.isDisabled)
			{
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					ClearSeat(i);
				}
			}
			else
			{
				if (!PlayerPhysicsReady(seatedPlayer))
				{
					continue;
				}
				PlayerTumble tumble = seatedPlayer.tumble;
				bool flag = groundSlopeValid && Mathf.Abs(groundSlopePitchAngle) > 8f;
				physGrabObject.OverrideMaterial(flag ? SemiFunc.PhysicMaterialDefault() : SemiFunc.PhysicMaterialSlippery());
				if (Object.op_Implicit((Object)(object)ArenaRace.instance))
				{
					vehicleSeat.grabbedTimer = Mathf.Max(vehicleSeat.grabbedTimer, 0.5f);
					if (!seatedPlayer.physGrabber.grabbed || (Object)(object)seatedPlayer.physGrabber.grabbedPhysGrabObject != (Object)(object)physGrabObject)
					{
						SemiFunc.PhysGrabberForceGrab(seatedPlayer.physGrabber, physGrabObject);
					}
				}
				else if (seatedPlayer.physGrabber.grabbed && (Object)(object)seatedPlayer.physGrabber.grabbedPhysGrabObject == (Object)(object)physGrabObject)
				{
					vehicleSeat.grabbedTimer = Mathf.Max(vehicleSeat.grabbedTimer, 0.5f);
				}
				seatedPlayer.playerAvatarVisuals.DisableTumbleMoveAnimation();
				tumble.OverrideDisableTumbleMoveSound(0.2f);
				if (seatedPlayer.isLocal)
				{
					tumble.OverrideTumbleUIDisable(0.2f);
					PlayerController.instance.CollisionController.OverrideDisableFallLoop(0.2f);
				}
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					tumble.TumbleOverrideTime(0.5f);
					tumble.OverrideDisableLookAtCamera(0.5f);
					tumble.playerAvatar.FallDamageResetSet(0.5f);
					seatedPlayer.physGrabber.OverrideGrabStrength(0f);
					seatedPlayer.physGrabber.OverrideDisableRotationControls();
					tumble.physGrabObject.OverrideIndestructible(0.5f);
					tumble.impactHurtTimer = 0f;
					if (!tumble.isTumbling && SemiFunc.PerSecond(2f, (Object)(object)this))
					{
						tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
					}
				}
				seatedPlayer.physGrabber.OverrideGrabPoint(vehicleSeat.grabPoint);
				float num = Vector3.Distance(tumble.rb.position, vehicleSeat.seatTransform.position);
				float num2 = Mathf.Clamp01((speedOverTime + hyperSpeedOverTime) / (maxForwardSpeed + hyperMaxSpeed));
				float num3 = (Object.op_Implicit((Object)(object)ArenaRace.instance) ? 999f : Mathf.Lerp(1.8f, 2.5f, num2));
				if (num > num3 && vehicleSeat.takeASeatTimer <= 0f)
				{
					seatedPlayer.physGrabber.OverrideGrabRelease(physGrabObject.photonView.ViewID);
					if (SemiFunc.IsMasterClientOrSingleplayer())
					{
						tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
						tumble.TumbleOverrideTime(0.5f);
					}
				}
				if (!Object.op_Implicit((Object)(object)ArenaRace.instance) && currentState == State.Active && num > 0.4f && vehicleSeat.takeASeatTimer <= 0f)
				{
					TumbleVehicle(1f);
				}
				if (num > 0.3f && i == 0 && !Object.op_Implicit((Object)(object)ArenaRace.instance))
				{
					float num4 = Mathf.InverseLerp(0.3f, 0.5f, num);
					seatSlowdownRate = Mathf.Lerp(0f, 50f, num4);
					seatVelocityDrag = Mathf.Lerp(1f, 10f, num4);
				}
				if (vehicleSeat.reachedFirstMount || (Object)(object)vehicleSeat.firstMountTransform == (Object)null)
				{
					CameraPosition.instance.OverrideSpeed(50f);
				}
				if (stateTimer > 1.5f && seatedPlayer.physGrabber.overrideDisableSpecialGrabPowers)
				{
					TumbleVehicle(1f);
				}
			}
		}
	}

	private void StateTumbled()
	{
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateTimerMax = tumbleTimer;
			stateTimer = 0f;
			stateStart = false;
			speedOverTime = 0f;
			hyperSpeedOverTime = 0f;
			tumbleStartSound.Play(((Component)this).transform, soundEffectVolume);
			groundSlopeValid = false;
			groundSlopePitchAngle = 0f;
			previousGroundSlopePitchAngle = 0f;
			lastValidSlopeAngle = 0f;
			lastValidSlopeTimer = 0f;
			pitchHistory = new float[50];
			pitchHistoryFillCount = 0;
			pitchHistoryIndex = 0;
			if (Object.op_Implicit((Object)(object)ArenaRace.instance))
			{
				arenaRaceTumbleYawPending = true;
			}
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				for (int i = 0; i < seats.Length; i++)
				{
					if ((Object)(object)seats[i].seatedPlayer != (Object)null)
					{
						ClearSeat(i);
					}
				}
				if (!Object.op_Implicit((Object)(object)ArenaRace.instance))
				{
					PhysGrabber[] array = physGrabObject.playerGrabbing.ToArray();
					for (int j = 0; j < array.Length; j++)
					{
						if (Object.op_Implicit((Object)(object)array[j]))
						{
							array[j].OverrideGrabRelease(physGrabObject.photonView.ViewID);
						}
					}
				}
			}
		}
		for (int k = 0; k < seats.Length; k++)
		{
			VehicleSeat vehicleSeat = seats[k];
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (!Object.op_Implicit((Object)(object)seatedPlayer) || !PlayerPhysicsReady(seatedPlayer))
			{
				continue;
			}
			PlayerTumble tumble = seatedPlayer.tumble;
			if (seatedPlayer.isLocal)
			{
				tumble.OverrideTumbleUIDisable(0.2f);
				PlayerController.instance.CollisionController.OverrideDisableFallLoop(0.2f);
			}
			tumble.OverrideDisableTumbleMoveSound(0.2f);
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				tumble.TumbleOverrideTime(0.5f);
				tumble.OverrideDisableLookAtCamera(0.5f);
			}
			if (Object.op_Implicit((Object)(object)ArenaRace.instance))
			{
				vehicleSeat.grabbedTimer = Mathf.Max(vehicleSeat.grabbedTimer, 0.5f);
				if (seatedPlayer.isLocal)
				{
					PlayerController.instance.InputDisable(0.2f);
					PlayerController.instance.InputDirectionRaw = Vector3.zero;
					seatedPlayer.physGrabber.OverrideGrab(physGrabObject, 0.5f);
					seatedPlayer.physGrabber.OverrideAlwaysGrabbable(0.5f);
				}
				if (Object.op_Implicit((Object)(object)vehicleSeat.grabPoint))
				{
					seatedPlayer.physGrabber.OverrideGrabPoint(vehicleSeat.grabPoint);
				}
			}
		}
		if (!(stateTimer >= stateTimerMax))
		{
			return;
		}
		tumbleEndSound.Play(((Component)this).transform, soundEffectVolume);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (hasAnySeatedPlayerCached)
			{
				SetState(State.Active);
			}
			else
			{
				SetState(State.Idle);
			}
		}
	}

	private void FixedUpdateActive()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (seatSlowdownRate > 0f)
		{
			speedOverTime = Mathf.Max(0f, speedOverTime - seatSlowdownRate * Time.fixedDeltaTime);
			hyperSpeedOverTime = Mathf.Max(0f, hyperSpeedOverTime - seatSlowdownRate * Time.fixedDeltaTime);
			rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, 1f - Mathf.Exp((0f - seatVelocityDrag) * Time.fixedDeltaTime));
			seatSlowdownRate = 0f;
			seatVelocityDrag = 0f;
		}
		AllPhysicsManipulations();
	}

	private void AllPhysicsManipulations()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb) && !rb.isKinematic)
		{
			if (customGravityOverrideTimer > 0f)
			{
				customGravityOverrideTimer -= Time.fixedDeltaTime;
			}
			if (loopModeOverrideTimer > 0f)
			{
				loopModeOverrideTimer -= Time.fixedDeltaTime;
			}
			if (stairAssistActiveTimer > 0f)
			{
				stairAssistActiveTimer -= Time.fixedDeltaTime;
			}
			PerformGroundCheck();
			UpdatePhysicsState();
			UpdateWallAheadDetection();
			ApplyTorqueOverride();
			ApplyVehicleMovement();
			ApplyVehicleStabilization();
			ApplyAntiFlip();
			ApplySeatedPlayerForces();
			ApplyAIRiderForces();
			ApplyVehicleSteering();
			ApplyVehicleDrift();
			UpdateSmoothDrag();
			UpdateDriftBoostActive();
			ApplyVehicleAirborne();
			ApplyVehicleWheelForces();
			ApplyStairApproachAssist();
			ApplyWheelGrounding();
			ApplySlopeDownforce();
			ApplyLandingAlign();
			ApplyRampBoost();
			UpdateDriftSpeedSampling();
			UpdateDriftExitDelay();
			ApplyVelocityLimits();
		}
	}

	private void UpdatePhysicsState()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0412: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		vehicleUprightAngle = Vector3.Angle(((Component)this).transform.up, VehicleUp);
		uprightMultiplier = Mathf.InverseLerp(90f, 80f, vehicleUprightAngle);
		if (groundSlopeValid && groundSlopePitchAngle > 70f)
		{
			uprightMultiplier *= Mathf.InverseLerp(75f, 70f, groundSlopePitchAngle);
		}
		isAnyWheelGrounded = isFrontGrounded || isBackGrounded;
		stairClimbBoost = ((isFrontGrounded && isBackGrounded) ? (1f + Mathf.InverseLerp(0f, 45f, vehicleUprightAngle) * 0.5f * wallAheadScale) : 1f);
		isVehicleAirborne = !isFrontGrounded && !isBackGrounded;
		if (loopModeOverrideTimer > 0f)
		{
			horizontalVelocity = rb.linearVelocity - Vector3.Dot(rb.linearVelocity, ((Component)this).transform.up) * ((Component)this).transform.up;
		}
		else
		{
			horizontalVelocity = rb.linearVelocity;
			horizontalVelocity.y = 0f;
		}
		currentSpeed = horizontalVelocity.magnitude;
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		currentPitchAngle = Vector3.SignedAngle(forward, ((Component)this).transform.forward, ((Component)this).transform.right);
		currentDriver = Driver;
		currentDriverFullyMounted = DriverFullyMounted;
		inputY = 0f;
		if (AIControlActive)
		{
			inputY = aiControlInputThrottle;
		}
		else if (Object.op_Implicit((Object)(object)currentDriver) && currentDriverFullyMounted)
		{
			inputY = ControlThrottle;
		}
		Vector3 linearVelocity;
		if (isVehicleAirborne)
		{
			float[] array = airborneSpeedBuffer;
			int num = airborneSpeedBufferIndex;
			linearVelocity = rb.linearVelocity;
			array[num] = linearVelocity.magnitude;
			airborneSpeedBufferIndex = (airborneSpeedBufferIndex + 1) % airborneSpeedBuffer.Length;
			if (airborneSpeedBufferCount < airborneSpeedBuffer.Length)
			{
				airborneSpeedBufferCount++;
			}
		}
		float num2 = (Object.op_Implicit((Object)(object)ArenaRace.instance) ? 12f : 4f);
		if (wasAirborneLastFrame && !isVehicleAirborne)
		{
			landingAlignTimer = 1f;
			if (continuousAirborneTimer >= 0.3f)
			{
				landingBothWheelsWindow = 1f;
				float num3 = 0f;
				for (int i = 0; i < airborneSpeedBufferCount; i++)
				{
					if (airborneSpeedBuffer[i] > num3)
					{
						num3 = airborneSpeedBuffer[i];
					}
				}
				landingBufferedSpeed = Mathf.Min(num3, maxRBVelocity);
			}
			airborneSpeedBufferCount = 0;
			airborneSpeedBufferIndex = 0;
		}
		if (landingBothWheelsWindow > 0f)
		{
			if (isVehicleAirborne)
			{
				landingBothWheelsWindow = 0f;
			}
			else
			{
				landingBothWheelsWindow -= Time.fixedDeltaTime;
				float num4 = Mathf.Abs(currentPitchAngle);
				if (inputY > 0.1f && landingBufferedSpeed > 1f && vehicleUprightAngle < 30f && num4 < 30f)
				{
					Vector3 forward2 = ((Component)this).transform.forward;
					forward2.y = Mathf.Clamp(forward2.y, -0.15f, 0.5f);
					forward2.Normalize();
					linearVelocity = rb.linearVelocity;
					float num5 = Mathf.Max(linearVelocity.magnitude, landingBufferedSpeed);
					float num6 = 1f - Mathf.InverseLerp(25f, 40f, num4);
					float num7 = Mathf.Clamp01(landingBothWheelsWindow) * num6;
					rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, forward2 * num5, num7 * Time.fixedDeltaTime * 10f);
				}
			}
		}
		if (landingAlignTimer > 0f)
		{
			landingAlignTimer -= Time.fixedDeltaTime;
		}
		if (!(landingAlignTimer > 0f) && previousVelocityMagnitude - currentSpeed >= num2)
		{
			if (Object.op_Implicit((Object)(object)ArenaRace.instance))
			{
				impactSpeedLossTimer = 0.5f;
			}
			else
			{
				speedOverTime = 0f;
				hyperSpeedOverTime = 0f;
			}
		}
		if (impactSpeedLossTimer > 0f)
		{
			impactSpeedLossTimer -= Time.fixedDeltaTime;
			speedOverTime = Mathf.MoveTowards(speedOverTime, 0f, maxForwardSpeed * Time.fixedDeltaTime / 0.5f);
			hyperSpeedOverTime = Mathf.MoveTowards(hyperSpeedOverTime, 0f, hyperMaxSpeed * Time.fixedDeltaTime / 0.5f);
		}
		previousVelocityMagnitude = currentSpeed;
	}

	private void ApplyVehicleMovement()
	{
		//IL_07ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_078f: Unknown result type (might be due to invalid IL or missing references)
		//IL_079e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_061d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0624: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		//IL_0687: Unknown result type (might be due to invalid IL or missing references)
		//IL_068f: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0727: Unknown result type (might be due to invalid IL or missing references)
		//IL_072c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0707: Unknown result type (might be due to invalid IL or missing references)
		//IL_0716: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0418: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		if (!enableMovementPhysics)
		{
			return;
		}
		if (HasDriverControl)
		{
			bool flag = isFrontGrounded && isBackGrounded && vehicleUprightAngle < 55f;
			bool flag2 = (groundSlopeValid && groundSlopePitchAngle < -8f) || (lastValidSlopeTimer > 0f && lastValidSlopeAngle > 8f);
			Vector3 forward;
			if (flag)
			{
				forward = ((Component)this).transform.forward;
			}
			else if (flag2)
			{
				forward = ((Component)this).transform.forward;
			}
			else
			{
				forward = ((Component)this).transform.forward;
				forward.y = 0f;
				forward.Normalize();
			}
			if (forward.y < 0f)
			{
				forward.y = 0f;
				if (forward.sqrMagnitude > 0.001f)
				{
					forward.Normalize();
				}
			}
			if (wallAheadScale < 1f && forward.y > 0f && (!groundSlopeValid || !(groundSlopePitchAngle > 5f)))
			{
				forward.y *= wallAheadScale;
				forward.Normalize();
			}
			Vector3 val = forward;
			float num = 1f;
			Vector3 val2;
			if (isVehicleAirborne)
			{
				Vector3 controlCameraForward = ControlCameraForward;
				controlCameraForward.y = 0f;
				controlCameraForward.Normalize();
				float num2 = (flag2 ? 0f : Mathf.Clamp01(accumulatedFallVelocity / 2f));
				val2 = Vector3.Lerp(forward, controlCameraForward, num2);
				val = val2.normalized;
				num = 1f;
			}
			if ((!isFrontGrounded || !isBackGrounded) && !flag2)
			{
				float num3 = Mathf.Clamp(Mathf.Asin(((Component)this).transform.forward.y) * 57.29578f, -45f, 45f);
				val2 = Vector3.Cross(VehicleUp, val);
				Vector3 normalized = val2.normalized;
				val = Quaternion.AngleAxis(0f - num3, normalized) * val;
			}
			bool flag3 = isDrifting && Mathf.Abs(ControlInputX) > 0.1f;
			bool flag4 = driftBoostActiveTimer > 0f;
			if ((inputY > 0f && !flag3) | flag4)
			{
				float num4 = (flag4 ? 1f : inputY);
				Vector3 force = val * maxForwardSpeed * num4 * racingOutsideRaceForceCompensation * (1f + speedOverTime * 2f + hyperSpeedOverTime * 2f) * uprightMultiplier * stairClimbBoost * num;
				VehicleAddForce(force, (ForceMode)5);
				speedOverTime += Time.fixedDeltaTime * forwardAcceleration * forceMultiplier;
				speedOverTime = Mathf.Clamp(speedOverTime, 0f, maxForwardSpeed);
				if (speedOverTime >= maxForwardSpeed)
				{
					hyperSpeedOverTime += Time.fixedDeltaTime * hyperAcceleration * forceMultiplier;
					hyperSpeedOverTime = Mathf.Clamp(hyperSpeedOverTime, 0f, hyperMaxSpeed);
				}
				if (flag)
				{
					float num5 = Mathf.Min(horizontalVelocity.magnitude * forwardLiftMultiplier, maxForwardLift) * wallAheadScale;
					float num6 = Mathf.Clamp01(speedOverTime / maxForwardSpeed);
					float num7 = ((hyperMaxSpeed > 0f) ? (Mathf.Clamp01(hyperSpeedOverTime / hyperMaxSpeed) * 0.25f) : 0f);
					float num8 = (flag4 ? boostLiftForceMultiplier : 1f);
					float num9 = num5 + liftForce * (num6 + num7) * num8;
					if (AIControlActive)
					{
						num9 = 0f;
					}
					if ((!groundSlopeValid || !(groundSlopePitchAngle < 0f)) && !(((Component)this).transform.forward.y < 0f))
					{
						VehicleAddForce(VehicleUp * num9, (ForceMode)0);
						int num10 = 0;
						for (int i = 0; i < seats.Length; i++)
						{
							if ((Object)(object)seats[i].seatedPlayer != (Object)null)
							{
								num10++;
							}
						}
						if (num10 > 1)
						{
							VehicleAddForce(VehicleUp * playerLiftForce * (float)(num10 - 1), (ForceMode)0);
						}
					}
				}
			}
			else if (inputY < 0f && !flag4)
			{
				float num11 = Mathf.Clamp01(fakeVelocities.velocity.magnitude / maxForwardSpeed);
				float num12 = Mathf.Lerp(1f, 0.2f, num11);
				if (isVehicleAirborne)
				{
					num12 *= 0.25f;
				}
				VehicleAddForce(-forward * maxReverseSpeed * 10f * num12, (ForceMode)0);
				speedOverTime = 0f;
				hyperSpeedOverTime = 0f;
			}
			else
			{
				float num13 = (isVehicleAirborne ? 0.1f : 1f);
				if (isDrifting)
				{
					speedOverTime += Time.fixedDeltaTime * forwardAcceleration * 0.25f * forceMultiplier;
					speedOverTime = Mathf.Clamp(speedOverTime, 0f, maxForwardSpeed);
					hyperSpeedOverTime = Mathf.Max(0f, hyperSpeedOverTime - Time.fixedDeltaTime * deceleration * 2f * num13);
				}
				else
				{
					speedOverTime = Mathf.Max(0f, speedOverTime - Time.fixedDeltaTime * deceleration * num13);
				}
				hyperSpeedOverTime = Mathf.Max(0f, hyperSpeedOverTime - Time.fixedDeltaTime * deceleration * num13);
				if (speedOverTime > 0f || hyperSpeedOverTime > 0f)
				{
					Vector3 val3 = forward * maxForwardSpeed;
					float num14 = 1f;
					if (isDrifting)
					{
						float num15 = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
						num14 = Mathf.Lerp(0.05f, 1f, Mathf.Clamp01(num15 / (maxForwardSpeed + hyperMaxSpeed)));
					}
					VehicleAddForce(val3 * racingOutsideRaceForceCompensation * (1f + speedOverTime + hyperSpeedOverTime) * uprightMultiplier * stairClimbBoost * num14, (ForceMode)5);
				}
				else if (horizontalVelocity.magnitude < 0.3f)
				{
					if (loopModeOverrideTimer <= 0f)
					{
						rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
					}
				}
				else
				{
					VehicleAddForce(-horizontalVelocity, (ForceMode)5);
				}
			}
		}
		else
		{
			speedOverTime = Mathf.Max(0f, speedOverTime - Time.fixedDeltaTime * deceleration);
			if (horizontalVelocity.magnitude < 0.3f)
			{
				if (loopModeOverrideTimer <= 0f)
				{
					rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
				}
			}
			else
			{
				VehicleAddForce(-horizontalVelocity * 0.5f, (ForceMode)5);
			}
		}
		if (vehicleUprightAngle > 80f)
		{
			speedOverTime = 0f;
			hyperSpeedOverTime = 0f;
		}
	}

	internal void OverrideTorque(Transform _target, float _duration, int _forceDriftBoostLevel = 0, float _spring = -1f, float _damping = -1f)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		torqueOverrideRotation = Quaternion.LookRotation(_target.forward, Vector3.up);
		torqueOverrideTimer = _duration;
		activeTorqueOverrideSpring = ((_spring > 0f) ? _spring : torqueOverrideSpring);
		activeTorqueOverrideDamping = ((_damping > 0f) ? _damping : torqueOverrideDamping);
		if (_forceDriftBoostLevel > 0)
		{
			driftBoostActiveDirection = _target.forward;
			driftBoostLevel = _forceDriftBoostLevel;
			ApplyDriftBoost();
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("DriftBoostEffectsRPC", (RpcTarget)0, new object[1] { _forceDriftBoostLevel });
			}
			else
			{
				DriftBoostEffects(_forceDriftBoostLevel);
			}
			driftBoostLevel = 0;
			driftBoostProgress = 0f;
		}
	}

	internal void OverrideYaw(Vector3 _direction, float _duration, int _forceDriftBoostLevel = 0)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = _direction;
		val.y = 0f;
		val.Normalize();
		yawOverrideDirection = val;
		yawOverrideTimer = _duration;
		if (_forceDriftBoostLevel > 0)
		{
			driftBoostActiveDirection = _direction;
			driftBoostLevel = _forceDriftBoostLevel;
			ApplyDriftBoost();
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("DriftBoostEffectsRPC", (RpcTarget)0, new object[1] { _forceDriftBoostLevel });
			}
			else
			{
				DriftBoostEffects(_forceDriftBoostLevel);
			}
			driftBoostLevel = 0;
			driftBoostProgress = 0f;
		}
	}

	internal void OverridePitch(float _angle, float _duration)
	{
		pitchOverrideAngle = _angle;
		pitchOverrideTimer = _duration;
	}

	private void ApplyTorqueOverride()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (!(torqueOverrideTimer <= 0f))
		{
			torqueOverrideTimer -= Time.fixedDeltaTime;
			Quaternion targetRotation = torqueOverrideRotation;
			SemiFunc.PhysFollowRotationTorque(rb, targetRotation, activeTorqueOverrideSpring, activeTorqueOverrideDamping);
		}
	}

	private void ApplyVehicleStabilization()
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0352: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_0443: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Unknown result type (might be due to invalid IL or missing references)
		//IL_050b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_0519: Unknown result type (might be due to invalid IL or missing references)
		//IL_0520: Unknown result type (might be due to invalid IL or missing references)
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		//IL_052c: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0562: Unknown result type (might be due to invalid IL or missing references)
		//IL_056c: Unknown result type (might be due to invalid IL or missing references)
		//IL_05dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0614: Unknown result type (might be due to invalid IL or missing references)
		//IL_0620: Unknown result type (might be due to invalid IL or missing references)
		//IL_0628: Unknown result type (might be due to invalid IL or missing references)
		//IL_062d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0640: Unknown result type (might be due to invalid IL or missing references)
		//IL_0647: Unknown result type (might be due to invalid IL or missing references)
		//IL_073f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0780: Unknown result type (might be due to invalid IL or missing references)
		//IL_0786: Unknown result type (might be due to invalid IL or missing references)
		//IL_0897: Unknown result type (might be due to invalid IL or missing references)
		//IL_089c: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_09a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_09aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b16: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dcf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ef0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f28: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f43: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f96: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e9a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d69: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d70: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c79: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c7e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ca2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0caf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ccd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cf6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cfb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d1f: Unknown result type (might be due to invalid IL or missing references)
		if (!enableStabilizationPhysics || (!hasAnySeatedPlayerCached && !AIControlActive && !AITumbleImmune) || loopModeOverrideTimer > 0f || torqueOverrideTimer > 0f)
		{
			return;
		}
		float num = stabilizationForce;
		for (int i = 0; i < seats.Length; i++)
		{
			if ((Object)(object)seats[i].seatedPlayer != (Object)null)
			{
				num += seats[i].addStabilizationForce;
			}
		}
		Vector3 val = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity : Vector3.zero);
		Vector3 val2 = val;
		val2.y = 0f;
		float magnitude = val2.magnitude;
		float num2 = Mathf.Clamp01(magnitude / 5f);
		if (isAnyWheelGrounded)
		{
			num2 = Mathf.Max(num2, 0.5f);
		}
		float num3 = ((magnitude > 0.1f) ? (Mathf.Atan2(val.y, magnitude) * 57.29578f) : 0f);
		bool flag = isFrontGrounded && isBackGrounded;
		if (isFrontColliding && isBackColliding)
		{
			pitchSlopeTransitionTimer = 0f;
		}
		else if (pitchSlopeTransitionTimer == 0f && Mathf.Abs(smoothedPitchAngle) > 15f)
		{
			pitchSlopeTransitionTimer = 0.5f;
		}
		float num4 = smoothedPitchAngle;
		if (pitchOverrideTimer > 0f)
		{
			smoothedPitchAngle = Mathf.Lerp(smoothedPitchAngle, pitchOverrideAngle, Time.fixedDeltaTime * 10f);
			pitchOverrideTimer -= Time.fixedDeltaTime;
		}
		else if (flag && groundSlopeValid)
		{
			float num5 = Mathf.Clamp(groundSlopePitchAngle, -45f, 45f);
			float num6 = ((Mathf.Abs(num5 - smoothedPitchAngle) > 15f) ? 30f : 15f);
			smoothedPitchAngle = Mathf.Lerp(smoothedPitchAngle, num5, Time.fixedDeltaTime * num6);
		}
		else if (pitchStabAirborneGraceTimer > 0f && groundSlopeValid && (!isVehicleAirborne || !(driftBoostActiveTimer > 0f)))
		{
			float num7 = Mathf.Clamp(groundSlopePitchAngle, -45f, 45f);
			float num8 = ((Mathf.Abs(num7 - smoothedPitchAngle) > 15f) ? 20f : 10f);
			smoothedPitchAngle = Mathf.Lerp(smoothedPitchAngle, num7, Time.fixedDeltaTime * num8);
		}
		else
		{
			float num9 = (isVehicleAirborne ? (-10f) : 0f);
			float num10 = Mathf.Clamp(Mathf.Min(num3 * num2, num9), -25f, 25f);
			smoothedPitchAngle = Mathf.Lerp(smoothedPitchAngle, num10, Time.fixedDeltaTime * 7.5f);
		}
		if (pitchSlopeTransitionTimer > 0f)
		{
			smoothedPitchAngle = num4;
			smoothedPitchSpringTorque = 0f;
			pitchStabilizationBlend = 0f;
			pitchSlopeTransitionTimer -= Time.fixedDeltaTime;
		}
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 val3 = Quaternion.AngleAxis(0f - smoothedPitchAngle, ((Component)this).transform.right) * forward;
		Vector3 up = ((Component)this).transform.up;
		Vector3 val4 = VehicleUp;
		bool flag2 = HasDriverControl && Mathf.Abs(ControlInputX) > 0.1f;
		if (isDrifting && (flag2 || driftTimer > 0f))
		{
			float num11 = driftLowSpeedEaseProgress * driftLowSpeedEaseProgress * (3f - 2f * driftLowSpeedEaseProgress);
			float num12 = Mathf.Lerp(0.05f, 1f, num11);
			val4 = Quaternion.AngleAxis(driftCurrentRollAngle * num12 * driftDirection, ((Component)this).transform.forward) * VehicleUp;
			postDriftStabilizationBlend = 1f;
		}
		else
		{
			postDriftStabilizationBlend = Mathf.MoveTowards(postDriftStabilizationBlend, 0f, Time.fixedDeltaTime * 5f);
		}
		Vector3 val5 = Vector3.Cross(up, val4);
		float num13 = Mathf.InverseLerp(80f, 60f, vehicleUprightAngle);
		float num14 = Mathf.Clamp01(driftExitSnapTimer) * 2f;
		float num15 = Mathf.Lerp(1.2f, 4f, postDriftStabilizationBlend) + num14;
		float num16 = maxForwardSpeed + hyperMaxSpeed;
		float num17 = (isDrifting ? 1f : Mathf.Lerp(1f, 3f, Mathf.Clamp01(currentSpeed / num16)));
		float num18 = Mathf.Lerp(1f, 1.25f, Mathf.Clamp01(currentSpeed * 3.6f * 2f / maxSpeedKmh));
		Vector3 torque = Vector3.Project(val5, ((Component)this).transform.forward) * num * num13 * num15 * num17 * num18;
		VehicleAddTorque(torque, (ForceMode)0);
		float num19 = Mathf.Max(1f, num17);
		Vector3 val6 = new Vector3(rb.angularVelocity.x, 0f, rb.angularVelocity.z);
		float num20 = (isVehicleAirborne ? 0.3f : 1f);
		float num21 = Mathf.Min(val6.magnitude * angularDrag * num19 * num20, 40f);
		smoothedAngularDragMagnitude = Mathf.Lerp(smoothedAngularDragMagnitude, num21, Time.fixedDeltaTime * 10f);
		Vector3 torque2 = ((val6.sqrMagnitude > 0.001f) ? val6.normalized : Vector3.zero) * (0f - smoothedAngularDragMagnitude);
		VehicleAddTorque(torque2, (ForceMode)0);
		if (torqueOverrideTimer > 0f)
		{
			return;
		}
		Vector3 val7 = (isVehicleAirborne ? Vector3.up : ((Component)this).transform.up);
		float num22 = Vector3.Dot(rb.angularVelocity, val7) * (0f - yawAngularDrag);
		VehicleAddTorque(VehicleUp * num22, (ForceMode)0);
		bool flag3 = HasDriverControl && Mathf.Abs(ControlInputX) > 0.1f;
		bool flag4 = isDrifting || (flag3 && isAnyWheelGrounded);
		bool flag5 = isFrontGrounded && isBackGrounded && pitchOverrideTimer <= 0f && !groundSlopeValid;
		float num23 = ((lastValidSlopeAngle > 10f && lastValidSlopeTimer > 0f) ? 0.35f : 0.15f);
		if (isAnyWheelGrounded)
		{
			pitchStabAirborneGraceTimer = num23;
		}
		else
		{
			pitchStabAirborneGraceTimer -= Time.fixedDeltaTime;
		}
		if (isVehicleAirborne)
		{
			float num24 = continuousAirborneTimer;
			continuousAirborneTimer += Time.fixedDeltaTime;
			if (num24 < 0.12f && continuousAirborneTimer >= 0.12f && rb.linearVelocity.y > 1f && currentSpeed >= rampBoostMinSpeed)
			{
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("TakeoffCameraShakeRPC", (RpcTarget)0, Array.Empty<object>());
				}
				else
				{
					TakeoffCameraShakeRPC();
				}
			}
		}
		if (wasAirborneLastFrame && !isVehicleAirborne)
		{
			smoothedPitchSpringTorque = 0f;
			landingFrontLiftCooldown = 1f;
			landingPitchFullStrengthTimer = 1f;
			if (isBackGrounded && !isFrontGrounded)
			{
				landingFrontDownForceGraceTimer = 1.5f;
			}
			if (isFrontGrounded && !isBackGrounded)
			{
				landingBackDownForceGraceTimer = 1f;
			}
			bool flag6 = false;
			if (isFrontGrounded && !isBackGrounded && continuousAirborneTimer >= 0.5f && nosediveCooldownTimer <= 0f)
			{
				bool flag7 = continuousAirborneTimer >= 1.5f;
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("NosediveLandingEffectsRPC", (RpcTarget)0, new object[1] { flag7 });
				}
				else
				{
					NosediveLandingEffects(flag7);
				}
				nosediveCooldownTimer = 1f;
				flag6 = true;
			}
			if (continuousAirborneTimer >= 0.5f && !flag6)
			{
				wobbleDirection = -((Component)this).transform.up;
				wobblePositionSpringVelocity += 1.5f * wobblePositionStrength * 0.5f * 60f;
				wobblePositionSpringVelocity = Mathf.Clamp(wobblePositionSpringVelocity, (0f - wobblePositionMax) * 30f, wobblePositionMax * 30f);
				wobbleRotationSpringVelocity += 1.5f * wobbleRotationStrength * 60f;
				wobbleRotationSpringVelocity = Mathf.Clamp(wobbleRotationSpringVelocity, (0f - wobbleRotationMax) * 30f, wobbleRotationMax * 30f);
				wobbleCooldownTimer = Mathf.Max(wobbleCooldownTimer, 0.5f);
			}
			if (continuousAirborneTimer >= 0.3f && !flag6 && !fallDamageActive)
			{
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("LandingCameraShakeRPC", (RpcTarget)0, new object[1] { landingBufferedSpeed });
				}
				else
				{
					LandingCameraShakeRPC(landingBufferedSpeed);
				}
			}
			continuousAirborneTimer = 0f;
		}
		if (landingFrontLiftCooldown > 0f)
		{
			landingFrontLiftCooldown -= Time.fixedDeltaTime;
		}
		if (nosediveCooldownTimer > 0f)
		{
			nosediveCooldownTimer -= Time.fixedDeltaTime;
		}
		if (landingFrontDownForceGraceTimer > 0f)
		{
			if (isFrontGrounded)
			{
				landingFrontDownForceGraceTimer = 0f;
			}
			else
			{
				landingFrontDownForceGraceTimer -= Time.fixedDeltaTime;
			}
		}
		if (landingBackDownForceGraceTimer > 0f)
		{
			if (isBackGrounded)
			{
				landingBackDownForceGraceTimer = 0f;
			}
			else
			{
				landingBackDownForceGraceTimer -= Time.fixedDeltaTime;
			}
		}
		bool flag8 = isVehicleAirborne && pitchOverrideTimer <= 0f && pitchStabAirborneGraceTimer <= 0f;
		float num25 = ((flag4 | flag5 | flag8) ? 0f : 1f);
		if (pitchOverrideTimer > 0f && isVehicleAirborne && airborneBlend > 0.5f)
		{
			num25 = 1f;
		}
		if (flag8 && num25 == 0f)
		{
			pitchStabilizationBlend = 0f;
		}
		else
		{
			pitchStabilizationBlend = Mathf.MoveTowards(pitchStabilizationBlend, num25, Time.fixedDeltaTime * 5f);
		}
		float num26 = (0f - Vector3.Dot(rb.angularVelocity, ((Component)this).transform.right)) * num * 0.1f;
		num26 = Mathf.Clamp(num26, -60f, 60f);
		if (pitchStabilizationBlend > 0.01f)
		{
			float num27 = Mathf.Clamp(Vector3.SignedAngle(((Component)this).transform.forward, val3, ((Component)this).transform.right), -15f, 15f) * num * 0.1f * num2;
			num27 = Mathf.Clamp(num27, -40f, 40f);
			smoothedPitchSpringTorque = Mathf.Lerp(smoothedPitchSpringTorque, num27, Time.fixedDeltaTime * 8f);
			float num28 = 1f;
			if (isAnyWheelGrounded && groundSlopeValid && groundSlopePitchAngle < 0f)
			{
				num28 = 1f - Mathf.Clamp01(Mathf.Abs(groundSlopePitchAngle) / 90f);
			}
			bool flag9 = isAnyWheelGrounded && groundSlopeValid && currentPitchAngle + groundSlopePitchAngle > 5f;
			float num29 = ((isAnyWheelGrounded && currentPitchAngle > 0f && !flag9) ? 0.3f : 1f);
			float num30;
			if (flag9)
			{
				num30 = 5f;
				if (HasDriverControl && inputY > 0.1f)
				{
					Vector3 forward2 = ((Component)this).transform.forward;
					forward2.y = 0f;
					if (forward2.sqrMagnitude > 0.001f)
					{
						forward2.Normalize();
						VehicleAddForce(forward2 * rb.mass * Mathf.Abs(Physics.gravity.y) * 2f, (ForceMode)0);
					}
					if (Object.op_Implicit((Object)(object)wheelFrontForcePoint))
					{
						VehicleAddForceAtPosition(Vector3.up * rb.mass * Mathf.Abs(Physics.gravity.y) * 1.5f, wheelFrontForcePoint.position, (ForceMode)0);
					}
				}
			}
			else
			{
				num30 = ((landingPitchFullStrengthTimer > 0f) ? 0.75f : (0.25f * num28)) * num29;
			}
			float num31 = (smoothedPitchSpringTorque + num26) * pitchStabilizationBlend * num30;
			VehicleAddTorque(((Component)this).transform.right * num31, (ForceMode)0);
		}
		if (landingPitchFullStrengthTimer > 0f)
		{
			landingPitchFullStrengthTimer -= Time.fixedDeltaTime;
		}
		else
		{
			smoothedPitchSpringTorque = Mathf.Lerp(smoothedPitchSpringTorque, 0f, Time.fixedDeltaTime * 8f);
			if (flag5)
			{
				VehicleAddTorque(((Component)this).transform.right * num26 * 3f, (ForceMode)0);
			}
		}
		bool flag10 = false;
		for (int j = 0; j < seats.Length; j++)
		{
			if ((Object)(object)seats[j].seatedPlayer != (Object)null && (seats[j].takeASeatTimer > 0f || !seats[j].reachedFirstMount))
			{
				flag10 = true;
				break;
			}
		}
		if (flag10)
		{
			if (!Object.op_Implicit((Object)(object)ArenaRace.instance) && rb.linearVelocity.y > 0f)
			{
				float num32 = Mathf.Min(rb.linearVelocity.y * vehicleSeatDownForce, vehicleSeatDownForce);
				VehicleAddForce(VehicleDown * num32, (ForceMode)0);
			}
			if (!Object.op_Implicit((Object)(object)ArenaRace.instance))
			{
				smoothDragTarget = 2f;
			}
			float num33 = 1f - Mathf.InverseLerp(90f, 180f, vehicleUprightAngle);
			float num34 = Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.forward) * 0.5f * num33;
			float num35 = (0f - rb.angularVelocity.z) * 2f * num33;
			VehicleAddTorque(((Component)this).transform.forward * (num34 + num35), (ForceMode)0);
			float num36 = Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.right) * 0.5f * num33;
			float num37 = (0f - rb.angularVelocity.x) * 2f * num33;
			VehicleAddTorque(((Component)this).transform.right * (num36 + num37), (ForceMode)0);
		}
	}

	private void ApplyAntiFlip()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		if (torqueOverrideTimer > 0f || loopModeOverrideTimer > 0f)
		{
			return;
		}
		float num = Vector3.Dot(((Component)this).transform.up, VehicleUp);
		float num2 = -0.3f;
		if (!(num > num2))
		{
			float num3 = Mathf.InverseLerp(num2, -1f, num);
			num3 = Mathf.Lerp(0.5f, 1f, num3);
			Vector3 val = Vector3.Cross(((Component)this).transform.up, VehicleUp);
			if (val.sqrMagnitude < 0.01f)
			{
				val = ((Component)this).transform.right;
			}
			val.Normalize();
			float num4 = 3000f * num3;
			VehicleAddTorque(val * num4, (ForceMode)5);
			float num5 = 30f * num3;
			VehicleAddTorque(-rb.angularVelocity * num5, (ForceMode)5);
		}
	}

	private void DriftSteeringOffsetsGet(out float steerOffset, out float yawOffset)
	{
		float num = driftLowSpeedEaseProgress * driftLowSpeedEaseProgress * (3f - 2f * driftLowSpeedEaseProgress);
		float num2 = (isDrifting ? Mathf.Lerp(0.1f, 1f, num) : 1f);
		steerOffset = ((isDrifting && Mathf.Abs(ControlInputX) > 0.1f) ? (25f * num2 * (0f - driftDirection)) : 0f);
		yawOffset = driftCurrentYawOffset * num2 * (0f - driftDirection);
	}

	private void ApplyVehicleSteering()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_047d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0589: Unknown result type (might be due to invalid IL or missing references)
		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_062a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0631: Unknown result type (might be due to invalid IL or missing references)
		//IL_063c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0641: Unknown result type (might be due to invalid IL or missing references)
		//IL_0646: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0684: Unknown result type (might be due to invalid IL or missing references)
		if (torqueOverrideTimer > 0f || loopModeOverrideTimer > 0f || !enableSteeringPhysics || !HasDriverControl)
		{
			return;
		}
		Vector3 val = (isVehicleAirborne ? Vector3.up : ((Component)this).transform.up);
		Vector3 controlCameraForward = ControlCameraForward;
		controlCameraForward.y = 0f;
		controlCameraForward.Normalize();
		float num = Mathf.Atan2(controlCameraForward.x, controlCameraForward.z) * 57.29578f;
		if (isDrifting)
		{
			float num2 = Mathf.DeltaAngle(targetCameraYaw, num);
			targetCameraYaw += num2 * driftTurningSpeed * Time.fixedDeltaTime;
		}
		else
		{
			targetCameraYaw = num;
		}
		Vector3 val2 = new Vector3(Mathf.Sin(targetCameraYaw * (MathF.PI / 180f)), 0f, Mathf.Cos(targetCameraYaw * (MathF.PI / 180f)));
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		float controlInputX = ControlInputX;
		bool flag = Mathf.Abs(controlInputX) > 0.1f;
		Vector3 linearVelocity = rb.linearVelocity;
		float magnitude = linearVelocity.magnitude;
		float num3 = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
		float num4 = (isDrifting ? driftYawBoost : 1f);
		Vector3 val3 = ((!flag) ? val2 : (Quaternion.AngleAxis(controlInputX * manualTurnExtraAngle, Vector3.up) * val2));
		float num5 = Mathf.Clamp01(speedOverTime / driftBoostFullSpeedThreshold);
		bool flag2 = Mathf.Abs(driftCurrentYawOffset) > 0.5f;
		bool flag3 = Mathf.Abs(driftSmoothedSteerOffset) > 0.5f;
		if (isDrifting | flag2 | flag3)
		{
			DriftSteeringOffsetsGet(out var steerOffset, out var yawOffset);
			float num6 = (isDrifting ? 0.002f : 0.001f);
			driftSmoothedSteerOffset = Mathf.SmoothDamp(driftSmoothedSteerOffset, steerOffset, ref driftSteerOffsetVelocity, num6);
			val3 = Quaternion.AngleAxis(driftSmoothedSteerOffset + yawOffset, Vector3.up) * val2;
			if (isDrifting)
			{
				smoothDragTarget = Mathf.Lerp(driftLowSpeedDrag, driftDrag, num5);
			}
		}
		UpdateDriftBoostProgress(num5);
		float num7 = Mathf.Clamp01(num3 / (maxForwardSpeed + hyperMaxSpeed));
		float num8 = (isDrifting ? 999f : Mathf.Lerp(999f, 220f, num7));
		if (!smoothedSteeringTargetInitialized)
		{
			smoothedSteeringTarget = val3;
			smoothedSteeringTargetInitialized = true;
		}
		smoothedSteeringTarget = Vector3.RotateTowards(smoothedSteeringTarget, val3, num8 * (MathF.PI / 180f) * Time.fixedDeltaTime, 0f);
		smoothedSteeringTarget.y = 0f;
		smoothedSteeringTarget.Normalize();
		if (yawOverrideTimer > 0f)
		{
			smoothedSteeringTarget = Vector3.RotateTowards(smoothedSteeringTarget, yawOverrideDirection, MathF.PI * 4f * Time.fixedDeltaTime, 0f);
			smoothedSteeringTarget.y = 0f;
			smoothedSteeringTarget.Normalize();
			yawOverrideTimer -= Time.fixedDeltaTime;
		}
		Vector3 val4 = smoothedSteeringTarget;
		float num9 = Vector3.SignedAngle(forward, val4, val);
		float num10 = Mathf.Abs(num9);
		float num11 = ((num10 < 15f) ? Mathf.Lerp(0.15f, 1f, num10 / 15f) : 1f);
		bool num12 = isDrifting && Mathf.Abs(controlInputX) < 0.1f;
		float num13 = Mathf.Clamp01(driftDirectionMismatchTimer / 0.1f);
		num13 = num13 * num13 * (3f - 2f * num13);
		float num14 = Mathf.Lerp(0.5f, 1.5f, num13);
		float num15;
		if (num12)
		{
			num15 = 0.8f;
		}
		else
		{
			num15 = (isDrifting ? num14 : 1f);
		}
		if (driftExitSnapTimer > 0f)
		{
			if (flag)
			{
				driftExitSnapTimer = 0f;
			}
			else
			{
				driftExitSnapTimer -= Time.fixedDeltaTime;
			}
		}
		float num16 = Mathf.Lerp(1f, 2f, num7);
		float num17 = yawSpringStrength * num11 * num15 * num16;
		float num18 = Mathf.Lerp(1f, 2f, num7);
		float num19 = yawSpringDamping * num18;
		float num20 = num9 * num17;
		float num21 = (0f - Vector3.Dot(rb.angularVelocity, val)) * num19;
		float num22 = 1f;
		if (isDrifting)
		{
			float num23 = Mathf.Clamp01((Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f) / (maxForwardSpeed + hyperMaxSpeed));
			num22 = Mathf.Lerp(0.333f, 1f, num23);
		}
		float num24 = Mathf.Clamp01(num3 / 4f);
		num24 = Mathf.Lerp(0.3f, 1f, num24);
		float num25 = (num20 + num21) * num4 * num22 * (isDrifting ? 1f : num24);
		VehicleAddTorque(((Component)this).transform.up * num25, (ForceMode)5);
		float num26 = maxForwardSpeed + hyperMaxSpeed;
		float num27 = Mathf.Min(magnitude, num26);
		if (isVehicleAirborne)
		{
			float num28 = velocityRedirectStrength * 0.3f;
			Vector3 val5 = ((Component)this).transform.forward * magnitude - rb.linearVelocity;
			val5.y = 0f;
			VehicleAddForce(val5 * num28 * num27, (ForceMode)5);
			return;
		}
		float num29 = ((hasDrifting && isDrifting) ? (velocityRedirectStrength * driftRedirectMultiplier) : velocityRedirectStrength);
		if (groundSlopeValid && groundSlopePitchAngle < -10f)
		{
			num29 *= Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(Mathf.Abs(groundSlopePitchAngle) / 45f));
		}
		Vector3 val6 = ((Component)this).transform.forward * magnitude - rb.linearVelocity;
		if (!groundSlopeValid || !(Mathf.Abs(groundSlopePitchAngle) > 10f))
		{
			val6.y = 0f;
		}
		else if (wallAheadScale < 1f)
		{
			val6.y = Mathf.Min(val6.y, 0f) * wallAheadScale;
		}
		Vector3 force = val6 * num29 * num27;
		VehicleAddForce(force, (ForceMode)5);
	}

	private void ApplyVehicleDrift()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0325: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0513: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Unknown result type (might be due to invalid IL or missing references)
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_0530: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Unknown result type (might be due to invalid IL or missing references)
		//IL_053a: Unknown result type (might be due to invalid IL or missing references)
		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0602: Unknown result type (might be due to invalid IL or missing references)
		//IL_0607: Unknown result type (might be due to invalid IL or missing references)
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		//IL_060e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0647: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0667: Unknown result type (might be due to invalid IL or missing references)
		//IL_066c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0705: Unknown result type (might be due to invalid IL or missing references)
		//IL_070c: Unknown result type (might be due to invalid IL or missing references)
		//IL_067b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0750: Unknown result type (might be due to invalid IL or missing references)
		//IL_0724: Unknown result type (might be due to invalid IL or missing references)
		//IL_0755: Unknown result type (might be due to invalid IL or missing references)
		//IL_0756: Unknown result type (might be due to invalid IL or missing references)
		//IL_0758: Unknown result type (might be due to invalid IL or missing references)
		//IL_075d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0762: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07da: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0821: Unknown result type (might be due to invalid IL or missing references)
		//IL_0826: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0804: Unknown result type (might be due to invalid IL or missing references)
		//IL_0809: Unknown result type (might be due to invalid IL or missing references)
		//IL_080e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0812: Unknown result type (might be due to invalid IL or missing references)
		//IL_0817: Unknown result type (might be due to invalid IL or missing references)
		//IL_0828: Unknown result type (might be due to invalid IL or missing references)
		//IL_0833: Unknown result type (might be due to invalid IL or missing references)
		//IL_0847: Unknown result type (might be due to invalid IL or missing references)
		//IL_084b: Unknown result type (might be due to invalid IL or missing references)
		if (!enableDriftPhysics || loopModeOverrideTimer > 0f || !hasDrifting || !HasDriverControl)
		{
			return;
		}
		Vector3 controlCameraForward = ControlCameraForward;
		controlCameraForward.y = 0f;
		controlCameraForward.Normalize();
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		float controlInputX = ControlInputX;
		bool flag = Mathf.Abs(controlInputX) > 0.1f;
		Vector3 val = ((!flag) ? controlCameraForward : (Quaternion.AngleAxis(controlInputX * manualTurnExtraAngle, Vector3.up) * controlCameraForward));
		float num = Vector3.SignedAngle(forward, val, Vector3.up);
		float num2 = driftAngleThreshold * 0.5f;
		float num3 = ((Mathf.Abs(controlInputX) > 0.1f) ? Mathf.Sign(controlInputX) : 0f);
		float num4 = Vector3.SignedAngle(Vector3.up, ((Component)this).transform.up, ((Component)this).transform.forward);
		float num5 = Mathf.Sign(num4);
		if (Mathf.Abs(num4) < 5f)
		{
			num5 = 0f;
		}
		if (driftCooldownTimer > 0f)
		{
			driftCooldownTimer -= Time.fixedDeltaTime;
		}
		float num6 = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
		float num7 = num3;
		if (isVehicleAirborne)
		{
			if (flag && driftCooldownTimer <= 0f)
			{
				driftQueuedWhileAirborne = true;
				driftQueuedDirection = num7;
			}
			else if (!flag)
			{
				driftQueuedWhileAirborne = false;
			}
		}
		bool flag2 = flag && Mathf.Abs(num) > num2 && isBackGrounded && driftCooldownTimer <= 0f;
		if (((driftQueuedWhileAirborne && isBackGrounded) & flag) && driftCooldownTimer <= 0f)
		{
			driftQueuedWhileAirborne = false;
			flag2 = true;
			num7 = driftQueuedDirection;
		}
		if ((isDrifting & flag) && num5 != 0f && num7 != 0f && num7 != num5)
		{
			driftDirectionMismatchTimer += Time.fixedDeltaTime;
			driftTimer = driftMinDuration;
			driftDirection = Mathf.MoveTowards(driftDirection, num7, Time.fixedDeltaTime * 10f);
			driftSwitchImpactCooldown = 0.5f;
			impactHistorySampleCount = 0;
		}
		else
		{
			driftDirectionMismatchTimer = 0f;
			if (flag2 && (!isDrifting || (num7 != 0f && (num7 == num5 || num5 == 0f))))
			{
				if (!isDrifting)
				{
					driftLowSpeedEaseProgress = 1f;
					Vector3 linearVelocity = rb.linearVelocity;
					float y = linearVelocity.y;
					Vector3 val2 = new Vector3(linearVelocity.x, 0f, linearVelocity.z);
					float magnitude = val2.magnitude;
					Vector3 controlCameraForward2 = ControlCameraForward;
					controlCameraForward2.y = 0f;
					controlCameraForward2.Normalize();
					Vector3 val3 = controlCameraForward2 * magnitude;
					rb.linearVelocity = new Vector3(val3.x, y, val3.z);
				}
				driftTimer = driftMinDuration;
				driftDirection = num7;
			}
			else if ((isDrifting & flag) && num7 != 0f && (num7 == num5 || num5 == 0f))
			{
				driftTimer = driftMinDuration;
				driftDirection = num7;
			}
		}
		bool flag3 = Mathf.Abs(controlInputX) <= 0.1f;
		if ((driftTimer > 0f) & flag3)
		{
			driftTimer = 0f;
		}
		else if (driftTimer > 0f)
		{
			driftTimer -= Time.fixedDeltaTime;
		}
		if (isVehicleAirborne)
		{
			if (isDrifting && !driftPausedByAirborne)
			{
				driftPausedByAirborne = true;
				driftPausedBoostLevel = driftBoostLevel;
				driftPausedBoostProgress = driftBoostProgress;
			}
			driftTimer = 0f;
		}
		if (driftExitSnapTimer > 0f)
		{
			driftTimer = 0f;
		}
		if (driftStateChangeCooldown > 0f)
		{
			driftStateChangeCooldown -= Time.fixedDeltaTime;
		}
		if (AIControlActive && aiDriftOverrideTimer > 0f)
		{
			driftDirection = ((aiDriftOverrideDirection >= 0f) ? 1f : (-1f));
			driftTimer = driftMinDuration;
		}
		bool flag4 = driftTimer > 0f;
		if (flag4 != isDrifting && driftStateChangeCooldown <= 0f)
		{
			driftStateChangeCooldown = 0.1f;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetDriftingRPC", (RpcTarget)0, new object[1] { flag4 });
			}
			else
			{
				SetDriftingLocal(flag4);
			}
		}
		if (!isVehicleAirborne && driftPausedByAirborne && !isDrifting)
		{
			driftPausedByAirborne = false;
		}
		Vector3 linearVelocity2 = rb.linearVelocity;
		linearVelocity2.y = 0f;
		Vector3 val4 = linearVelocity2 - Vector3.Project(linearVelocity2, ((Component)this).transform.forward);
		driftIntensity = val4.magnitude;
		if (!isDrifting)
		{
			return;
		}
		driftDurationTimer += Time.fixedDeltaTime;
		if (!isVehicleAirborne)
		{
			if (driftLowSpeedEaseProgress < 1f)
			{
				driftLowSpeedEaseProgress += Time.fixedDeltaTime / driftLowSpeedEaseDuration;
				driftLowSpeedEaseProgress = Mathf.Clamp01(driftLowSpeedEaseProgress);
			}
			float num8 = driftLowSpeedEaseProgress * driftLowSpeedEaseProgress * (3f - 2f * driftLowSpeedEaseProgress);
			smoothAngularDragTarget = Mathf.Lerp(6f, driftOriginalAngularDrag, num8);
			Vector3 controlCameraForward3 = ControlCameraForward;
			controlCameraForward3.y = 0f;
			controlCameraForward3.Normalize();
			Vector3 val5 = Vector3.Cross(Vector3.up, controlCameraForward3);
			float num9 = Mathf.Clamp01(num6 / (maxForwardSpeed + hyperMaxSpeed));
			float num10 = Mathf.Lerp(0.05f, 1f, num9);
			float num11 = num9 * num9;
			Vector3 forward2 = ((Component)this).transform.forward;
			forward2.y = 0f;
			forward2.Normalize();
			Vector3 forward3 = ((Component)this).transform.forward;
			if (wallAheadScale < 1f && forward3.y > 0f)
			{
				forward3.y *= wallAheadScale;
				forward3.Normalize();
			}
			VehicleAddForce(val5 * driftForce * 10f * driftDirection * (num11 * 3.5f), (ForceMode)0);
			VehicleAddForce(controlCameraForward3 * driftForce * num10, (ForceMode)0);
			VehicleAddForce(forward3 * driftForce * 5f * num10, (ForceMode)0);
			_003F val6 = (Object.op_Implicit((Object)(object)fakeVelocities) ? new Vector3(fakeVelocities.velocity.x, 0f, fakeVelocities.velocity.z) : Vector3.zero);
			Vector3 val7 = (Vector3)val6 - Vector3.Project((Vector3)val6, forward2);
			float magnitude2 = val7.magnitude;
			float num12 = 1f - Mathf.Clamp01(magnitude2 / Mathf.Max(num6, 0.1f));
			float num13 = Mathf.Max(driftForce * 10f * (num11 * 3.5f) * num12, 0.1f);
			VehicleAddForce(forward3 * num13, (ForceMode)0);
			Vector3 val8 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
			Vector3 val9;
			if (val8.sqrMagnitude > 1f)
			{
				val7 = Vector3.ProjectOnPlane(val8.normalized, ((Component)this).transform.up);
				val9 = val7.normalized;
			}
			else
			{
				val9 = ((Component)this).transform.forward;
			}
			float num14 = (0f - Physics.gravity.y) * val9.y;
			if (num14 > 0f)
			{
				VehicleAddForce(val9 * num14, (ForceMode)5);
			}
		}
	}

	private void ApplyVehicleAirborne()
	{
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		if (!enableAirbornePhysics)
		{
			return;
		}
		bool flag = !isFrontGrounded && !isBackGrounded;
		if (flag)
		{
			airborneBlend = Mathf.MoveTowards(airborneBlend, 1f, Time.fixedDeltaTime * airborneBlendSpeed);
		}
		else
		{
			airborneBlend = Mathf.MoveTowards(airborneBlend, 0f, Time.fixedDeltaTime * airborneBlendSpeed);
		}
		if (!flag)
		{
			pitchHistory[pitchHistoryIndex] = (groundSlopeValid ? groundSlopePitchAngle : currentPitchAngle);
			pitchHistoryIndex = (pitchHistoryIndex + 1) % pitchHistory.Length;
			if (pitchHistoryFillCount < pitchHistory.Length)
			{
				pitchHistoryFillCount++;
			}
		}
		if (flag)
		{
			float num = 0f - currentPitchAngle;
			if (!wasFullyAirborne)
			{
				int num2 = Mathf.Min(pitchHistoryFillCount, 50);
				float num3 = num;
				for (int i = 0; i < num2; i++)
				{
					int num4 = (pitchHistoryIndex - 1 - i + pitchHistory.Length) % pitchHistory.Length;
					if (pitchHistory[num4] > num3)
					{
						num3 = pitchHistory[num4];
					}
				}
				airborneAnimatedPitch = num3;
				airborneAnimatedPeakTarget = 0f - num3;
			}
			float num5 = airborneTargetPitch;
			float num6 = ((airborneAnimatedPitch > num5) ? 1.275f : 0f);
			float num7 = airbornePitchSpeed * (1f + num6);
			airborneAnimatedPitch = Mathf.MoveTowards(airborneAnimatedPitch, num5, Time.fixedDeltaTime * num7);
		}
		wasFullyAirborne = flag;
		if (flag && airborneBlend > 0f && airbornePitchStrength > 0f)
		{
			float num8 = 0f - currentPitchAngle;
			float num9 = Vector3.Dot(rb.angularVelocity, ((Component)this).transform.right);
			float num10 = Mathf.Clamp(airborneAnimatedPitch - num8, -15f, 15f);
			float num11 = ((num10 > 0f) ? 1.5f : 1f);
			float num12 = (0f - num10) * airbornePitchStrength * num11;
			float num13 = (0f - num9) * airbornePitchStrength * 0.5f;
			VehicleAddTorque(((Component)this).transform.right * (num12 + num13) * airborneBlend, (ForceMode)0);
		}
		if (isVehicleAirborne)
		{
			bool flag2 = lastValidSlopeTimer > 0f && lastValidSlopeAngle > 10f;
			if ((customGravityOverrideTimer > 0f) | flag2)
			{
				currentFallGravity = 0f;
				accumulatedFallVelocity = 0f;
				currentCustomGravitySpeed = 0f;
				if (AIControlActive)
				{
					currentCustomGravitySpeed = fallGravityTopSpeed;
				}
			}
			else
			{
				if (rb.linearVelocity.y > 0.1f)
				{
					currentFallGravity = 0f;
					accumulatedFallVelocity = 0f;
					currentCustomGravitySpeed = 0f;
				}
				else
				{
					if (currentFallGravity <= 0f)
					{
						fallGravityStartHeight = ((Component)this).transform.position.y;
					}
					currentFallGravity += fallGravityAcceleration * Time.fixedDeltaTime;
					currentFallGravity = Mathf.Min(currentFallGravity, fallGravityMax);
					accumulatedFallVelocity += Time.fixedDeltaTime;
				}
				float num14 = Mathf.Clamp01(accumulatedFallVelocity / fallGravityEaseInDuration);
				num14 = num14 * num14 * num14;
				currentCustomGravitySpeed = num14 * fallGravityTopSpeed;
				if (AIControlActive)
				{
					currentCustomGravitySpeed = fallGravityTopSpeed;
				}
				float num15 = fallGravityStartHeight - ((Component)this).transform.position.y;
				float num16 = fallGravityMax * fallDamageThresholdPercent;
				if (currentFallGravity >= num16 && num15 >= 6f && !fallDamageDisabled)
				{
					fallDamageActive = true;
				}
			}
		}
		else if (!isVehicleAirborne)
		{
			if (wasAirborneLastFrame && fallDamageActive)
			{
				TriggerFallDamageLanding();
			}
			if (wasAirborneLastFrame && airborneBlend > 0.3f)
			{
				landingImpactCooldown = 0.5f;
				impactHistorySampleCount = 0;
				distanceTraveledAtSmallSpeed = 0f;
				distanceTraveledAtMediumSpeed = 0f;
				distanceTraveledAtBigSpeed = 0f;
			}
			currentFallGravity = 0f;
			accumulatedFallVelocity = 0f;
			currentCustomGravitySpeed = 0f;
			fallDamageActive = false;
		}
		wasAirborneLastFrame = isVehicleAirborne;
		wasFrontGroundedLastFrame = isFrontGrounded;
		wasBackGroundedLastFrame = isBackGrounded;
	}

	private void ApplyVehicleWheelForces()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_057e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0589: Unknown result type (might be due to invalid IL or missing references)
		//IL_059a: Unknown result type (might be due to invalid IL or missing references)
		//IL_059f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
		//IL_048d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0621: Unknown result type (might be due to invalid IL or missing references)
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0630: Unknown result type (might be due to invalid IL or missing references)
		//IL_0634: Unknown result type (might be due to invalid IL or missing references)
		//IL_0647: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0671: Unknown result type (might be due to invalid IL or missing references)
		//IL_0676: Unknown result type (might be due to invalid IL or missing references)
		//IL_067e: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0506: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_070c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0713: Unknown result type (might be due to invalid IL or missing references)
		//IL_0733: Unknown result type (might be due to invalid IL or missing references)
		//IL_0738: Unknown result type (might be due to invalid IL or missing references)
		if (!enableWheelPhysics || (isFrontGrounded && isBackGrounded && airborneBlend <= 0f))
		{
			return;
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude < 0.5f)
		{
			return;
		}
		float num = Vector3.Dot(linearVelocity, ((Component)this).transform.forward);
		bool flag = num > 0f;
		bool flag2 = num < 0f;
		int num2 = layerMaskDefaultPhysGrab;
		if (frontLiftUpCooldown > 0f)
		{
			frontLiftUpCooldown -= Time.fixedDeltaTime;
		}
		if (frontLiftDownCooldown > 0f)
		{
			frontLiftDownCooldown -= Time.fixedDeltaTime;
		}
		if (flag && Object.op_Implicit((Object)(object)wheelFront) && Object.op_Implicit((Object)(object)wheelFrontForcePoint) && landingFrontLiftCooldown <= 0f)
		{
			RaycastHit val4 = default;
			if (isFrontGrounded && frontLiftUpCooldown <= 0f)
			{
				Vector3 val = wheelFront.localScale * 0.5f;
				val.x = Mathf.Abs(val.x);
				val.y = Mathf.Abs(val.y);
				val.z = Mathf.Abs(val.z);
				Collider[] array = Physics.OverlapBox(wheelFront.position, val, wheelFront.rotation, num2, (QueryTriggerInteraction)1);
				RaycastHit val3 = default;
				for (int i = 0; i < array.Length; i++)
				{
					if (((Component)array[i]).transform.IsChildOf(((Component)this).transform) || (Object)(object)array[i].attachedRigidbody == (Object)(object)rb)
					{
						continue;
					}
					float num3 = wallAheadScale;
					Vector3 val2 = array[i].ClosestPoint(wheelFront.position) - wheelFront.position;
					if (val2.sqrMagnitude > 0.0001f && Physics.Raycast(wheelFront.position, val2.normalized, ref val3, val2.magnitude + 0.5f, num2, (QueryTriggerInteraction)1))
					{
						float num4 = Vector3.Angle(val3.normal, Vector3.up);
						if (num4 > 50f)
						{
							float num5 = Mathf.Clamp01(1f - (num4 - 50f) / 40f);
							num3 = Mathf.Min(num3, num5);
						}
					}
					float num6 = Mathf.Abs(num);
					float num7 = Mathf.Clamp01(num6 / wheelFrontForceMaxSpeed);
					float num8 = Mathf.Lerp(wheelFrontForceMin, wheelFrontForceMax, num7);
					float num9 = Mathf.Clamp01((speedOverTime + hyperSpeedOverTime) / (maxForwardSpeed + hyperMaxSpeed));
					float num10 = ((isFrontGrounded && isBackGrounded) ? (3f + num9 * 1f) : 1f);
					VehicleAddForceAtPosition(Vector3.up * num6 * num8 * num3 * num10, wheelFrontForcePoint.position, (ForceMode)0);
					frontLiftDownCooldown = 1f;
					break;
				}
			}
			else if (isBackGrounded && frontLiftDownCooldown <= 0f && Physics.Raycast(wheelFrontForcePoint.position, Vector3.down, ref val4, 1f) && ((Component)val4.collider).gameObject.layer == LayerMask.NameToLayer("Default"))
			{
				float num11 = Mathf.Abs(num);
				float num12 = Mathf.Clamp01(num11 / wheelFrontForceMaxSpeed);
				float num13 = Mathf.Lerp(wheelFrontForceMin, wheelFrontForceMax, num12);
				VehicleAddForceAtPosition(Vector3.down * num11 * num13 * 0.5f, wheelFrontForcePoint.position, (ForceMode)0);
				frontLiftUpCooldown = 1f;
			}
		}
		bool flag3 = groundSlopeValid && Mathf.Abs(groundSlopePitchAngle) > 10f;
		if (((landingFrontDownForceGraceTimer > 0f && landingFrontDownForceGraceTimer <= 1f) & flag) && Object.op_Implicit((Object)(object)wheelFrontForcePoint) && isBackGrounded && !isFrontGrounded && !flag3)
		{
			float num14 = Mathf.Abs(num);
			float num15 = Mathf.Clamp01(num14 / wheelFrontForceMaxSpeed);
			float num16 = Mathf.Lerp(wheelFrontForceMin, wheelFrontForceMax, num15);
			VehicleAddForceAtPosition(Vector3.down * num14 * num16 * 0.5f, wheelFrontForcePoint.position, (ForceMode)0);
		}
		if (landingBackDownForceGraceTimer > 0f && Object.op_Implicit((Object)(object)wheelBackForcePoint) && isFrontGrounded && !isBackGrounded && !flag3)
		{
			float num17 = Mathf.Abs(num);
			float num18 = Mathf.Clamp01(num17 / wheelBackForceMaxSpeed);
			float num19 = Mathf.Lerp(wheelBackForceMin, wheelBackForceMax, num18);
			VehicleAddForceAtPosition(Vector3.down * num17 * num19 * 0.5f, wheelBackForcePoint.position, (ForceMode)0);
		}
		if (isFrontGrounded && !isBackGrounded && Object.op_Implicit((Object)(object)wheelBackForcePoint))
		{
			float num20 = rb.mass * Mathf.Abs(Physics.gravity.y) * 3f;
			VehicleAddForceAtPosition(Vector3.down * num20, wheelBackForcePoint.position, (ForceMode)0);
			Vector3 forward = ((Component)this).transform.forward;
			forward.y = 0f;
			if (forward.sqrMagnitude > 0.001f)
			{
				forward.Normalize();
				float num21 = rb.mass * Mathf.Abs(Physics.gravity.y) * 1f;
				VehicleAddForce(forward * num21, (ForceMode)0);
			}
		}
		if (!flag2 || !Object.op_Implicit((Object)(object)wheelBack) || !Object.op_Implicit((Object)(object)wheelBackForcePoint))
		{
			return;
		}
		Vector3 val5 = wheelBack.localScale * 0.5f;
		val5.x = Mathf.Abs(val5.x);
		val5.y = Mathf.Abs(val5.y);
		val5.z = Mathf.Abs(val5.z);
		Collider[] array2 = Physics.OverlapBox(wheelBack.position, val5, wheelBack.rotation, num2, (QueryTriggerInteraction)1);
		RaycastHit val7 = default;
		for (int j = 0; j < array2.Length; j++)
		{
			if (((Component)array2[j]).transform.IsChildOf(((Component)this).transform) || (Object)(object)array2[j].attachedRigidbody == (Object)(object)rb)
			{
				continue;
			}
			float num22 = wallAheadScale;
			Vector3 val6 = array2[j].ClosestPoint(wheelBack.position) - wheelBack.position;
			if (val6.sqrMagnitude > 0.0001f && Physics.Raycast(wheelBack.position, val6.normalized, ref val7, val6.magnitude + 0.5f, num2, (QueryTriggerInteraction)1))
			{
				float num23 = Vector3.Angle(val7.normal, Vector3.up);
				if (num23 > 50f)
				{
					float num24 = Mathf.Clamp01(1f - (num23 - 50f) / 40f);
					num22 = Mathf.Min(num22, num24);
				}
			}
			float num25 = Mathf.Abs(num);
			float num26 = Mathf.Clamp01(num25 / wheelBackForceMaxSpeed);
			float num27 = Mathf.Lerp(wheelBackForceMin, wheelBackForceMax, num26);
			VehicleAddForceAtPosition(VehicleUp * num25 * num27 * num22, wheelBackForcePoint.position, (ForceMode)0);
			break;
		}
	}

	private void ApplyWheelGrounding()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		float num = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
		if (num < 0.5f)
		{
			return;
		}
		LayerMask val = SemiFunc.LayerMaskGetVisionObstruct();
		float num2 = num / (maxForwardSpeed + hyperMaxSpeed);
		float num3 = (groundSlopeValid ? Mathf.Abs(groundSlopePitchAngle) : 0f);
		float num4 = 1f;
		float num5 = Mathf.Lerp(1f, 3f, Mathf.Clamp01(num3 / 45f));
		int num6;
		float num7;
		if (isVehicleAirborne)
		{
			num6 = ((pitchStabAirborneGraceTimer > 0f) ? 1 : 0);
			if (num6 != 0)
			{
				num7 = 0.5f;
				goto IL_00b9;
			}
		}
		else
		{
			num6 = 0;
		}
		num7 = (isVehicleAirborne ? 0f : 1f);
		goto IL_00b9;
		IL_00b9:
		float num8 = num7;
		float num9 = Mathf.Clamp01((vehicleUprightAngle - 30f) / 30f);
		Vector3 val2 = Vector3.Lerp(VehicleDown, Vector3.down, num9);
		float num10 = Mathf.Lerp(4f, 8f, num2);
		Vector3 force = val2 * wheelGroundingForce * num10 * num2 * num4 * num8;
		float num11 = ((num6 != 0) ? (wheelGroundingRayDistance * 2f * num5) : (wheelGroundingRayDistance * num5));
		Vector3 val3 = Vector3.Lerp(VehicleDown, Vector3.down, num9);
		if (Object.op_Implicit((Object)(object)wheelFront) && Object.op_Implicit((Object)(object)wheelFrontForcePoint) && Physics.Raycast(wheelFront.position, val3, num11, LayerMask.op_Implicit(val), (QueryTriggerInteraction)1))
		{
			VehicleAddForceAtPosition(force, wheelFrontForcePoint.position, (ForceMode)0);
		}
		if (Object.op_Implicit((Object)(object)wheelBack) && Object.op_Implicit((Object)(object)wheelBackForcePoint) && Physics.Raycast(wheelBack.position, val3, num11, LayerMask.op_Implicit(val), (QueryTriggerInteraction)1))
		{
			VehicleAddForceAtPosition(force, wheelBackForcePoint.position, (ForceMode)0);
		}
	}

	private void ApplySlopeDownforce()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		bool flag = (groundSlopeValid ? (groundSlopePitchAngle < -3f) : (lastValidSlopeAngle > 5f && Vector3.Dot(rb.linearVelocity, VehicleDown) > 0.5f));
		float num;
		if (groundSlopeValid)
		{
			num = Mathf.Abs(groundSlopePitchAngle);
			if (isVehicleAirborne)
			{
				lastValidSlopeAngle = num;
				lastValidSlopeTimer = 1f;
			}
			if (wasAirborneLastFrame && !isVehicleAirborne)
			{
				lastValidSlopeAngle = 0f;
				lastValidSlopeTimer = 0f;
			}
		}
		else
		{
			if (!(lastValidSlopeTimer > 0f))
			{
				return;
			}
			num = lastValidSlopeAngle;
			lastValidSlopeTimer -= Time.fixedDeltaTime;
		}
		if (groundSlopeValid && groundSlopePitchAngle > 70f)
		{
			float num2 = Mathf.InverseLerp(70f, 80f, groundSlopePitchAngle);
			VehicleAddForce(-((Component)this).transform.forward * rb.mass * Mathf.Abs(Physics.gravity.y) * num2, (ForceMode)0);
		}
		if (num < 5f || !flag)
		{
			return;
		}
		float num3 = num * (MathF.PI / 180f);
		float num4 = Mathf.Abs(Physics.gravity.y) * Mathf.Sin(num3);
		VehicleAddForce(((Component)this).transform.forward * rb.mass * num4, (ForceMode)0);
		float num5 = Mathf.Abs(Physics.gravity.y) * Mathf.Cos(num3);
		float num6 = (flag ? Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(num / 30f)) : 1f);
		float num7 = (isAnyWheelGrounded ? (1f - Mathf.Clamp01(num / 90f)) : 1f);
		Vector3 val = (isAnyWheelGrounded ? Vector3.down : (-((Component)this).transform.up));
		VehicleAddForce(val * rb.mass * num5 * num6 * num7, (ForceMode)0);
		if (airborneBlend > 0.01f)
		{
			float num8 = Mathf.Abs(Physics.gravity.y) * Mathf.Clamp01(airborneBlend * 2f);
			VehicleAddForce(Vector3.down * rb.mass * num8, (ForceMode)0);
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (!(linearVelocity.magnitude < 0.5f))
		{
			Vector3 up = ((Component)this).transform.up;
			float num9 = Vector3.Dot(linearVelocity, up);
			if (num9 > 0f)
			{
				float num10 = Mathf.Clamp01(num / 25f);
				Vector3 linearVelocity2 = linearVelocity - up * num9 * num10;
				rb.linearVelocity = linearVelocity2;
			}
		}
	}

	private void ApplyStairApproachAssist()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
		if (!enableStairApproachAssist || !Object.op_Implicit((Object)(object)wheelFront) || !Object.op_Implicit((Object)(object)wheelFrontForcePoint) || !HasDriverControl || inputY <= 0f || isVehicleAirborne || !isAnyWheelGrounded || vehicleUprightAngle > 30f)
		{
			return;
		}
		if (stairAssistCooldown > 0f)
		{
			stairAssistCooldown -= Time.fixedDeltaTime;
			return;
		}
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 position = wheelFront.position;
		Vector3 linearVelocity = rb.linearVelocity;
		_ = linearVelocity.magnitude;
		float num = Vector3.Dot(rb.linearVelocity, forward);
		if (!(speedOverTime >= 0.5f) || !(num < 1.5f))
		{
			return;
		}
		LayerMask val = SemiFunc.LayerMaskGetVisionObstruct();
		bool flag = false;
		int num2 = 0;
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		RaycastHit val2 = default;
		for (int i = 0; i < 3; i++)
		{
			if (Physics.Raycast(position + Vector3.up * (0.05f + (float)i * 0.15f), forward, ref val2, 1.2f, LayerMask.op_Implicit(val), (QueryTriggerInteraction)1) && !val2.transform.IsChildOf(((Component)this).transform))
			{
				flag = true;
				num2++;
				if (val2.distance < num3)
				{
					num3 = val2.distance;
				}
				if (val2.distance > num4)
				{
					num4 = val2.distance;
				}
			}
		}
		if (flag)
		{
			if (num2 >= 3 && num4 - num3 < 0.15f)
			{
				Vector3 forward2 = ((Component)this).transform.forward;
				forward2.y = 0f;
				forward2.Normalize();
				float num5 = Vector3.SignedAngle(((Component)this).transform.forward, forward2, ((Component)this).transform.right);
				float num6 = Vector3.Dot(rb.angularVelocity, ((Component)this).transform.right);
				float num7 = num5 * 10f - num6 * 5f;
				VehicleAddTorque(((Component)this).transform.right * num7, (ForceMode)0);
				stairAssistActiveTimer = 0.15f;
			}
			else
			{
				float num8 = Mathf.InverseLerp(0.5f, 2f, speedOverTime);
				float num9 = Mathf.Lerp(60f, 150f, num8);
				float num10 = Mathf.Lerp(10f, 25f, num8);
				VehicleAddForceAtPosition(Vector3.up * num9, wheelFrontForcePoint.position, (ForceMode)0);
				VehicleAddForce(forward * num10, (ForceMode)5);
				stairAssistActiveTimer = 0.15f;
			}
		}
	}

	private void ApplyLandingAlign()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		if (landingAlignTimer <= 0f || isVehicleAirborne || !groundSlopeValid || vehicleUprightAngle > 60f)
		{
			return;
		}
		Vector3 linearVelocity = rb.linearVelocity;
		float magnitude = linearVelocity.magnitude;
		if (magnitude < 1f)
		{
			return;
		}
		Vector3 forward = ((Component)this).transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 val = Vector3.Cross(Quaternion.AngleAxis(0f - groundSlopePitchAngle, ((Component)this).transform.right) * forward, ((Component)this).transform.right);
		Vector3 val2 = val.normalized;
		if (Vector3.Dot(val2, Vector3.up) < 0f)
		{
			val2 = -val2;
		}
		float num = Mathf.Clamp01(landingAlignTimer);
		float num2 = Mathf.Abs(0f - currentPitchAngle - groundSlopePitchAngle);
		if (num2 < 45f)
		{
			float num3 = Vector3.Dot(linearVelocity, val2);
			if (num3 > 0.5f)
			{
				float num4 = 1f - Mathf.InverseLerp(20f, 45f, num2);
				Vector3 val3 = linearVelocity - val2 * num3 * num * num4;
				rb.linearVelocity = val3.normalized * magnitude;
			}
		}
		if (isFrontGrounded && isBackGrounded)
		{
			float num5 = 0f - currentPitchAngle - groundSlopePitchAngle;
			if (Mathf.Abs(num5) > 2f)
			{
				float num6 = Mathf.Clamp(num5, -15f, 15f) * 2f * num;
				VehicleAddTorque(((Component)this).transform.right * num6, (ForceMode)0);
			}
		}
	}

	private void ApplyRampBoost()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (enableRampBoost && groundSlopeValid && !(groundSlopePitchAngle < rampBoostMinSlopeAngle) && !(groundSlopePitchAngle > 70f) && !isVehicleAirborne && isFrontGrounded && isBackGrounded && !(inputY <= 0.1f) && !(Mathf.Asin(((Component)this).transform.forward.y) * 57.29578f < -5f))
		{
			Vector3 val = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
			if (!(val.magnitude < rampBoostMinSpeed))
			{
				float num = Mathf.InverseLerp(rampBoostMinSlopeAngle, rampBoostMaxSlopeAngle, groundSlopePitchAngle);
				float num2 = rampBoostForce * catchUpMultiplier * 10f * num * inputY;
				VehicleAddForce(((Component)this).transform.forward * num2, (ForceMode)5);
			}
		}
	}

	private void ApplySeatedPlayerForces()
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0280: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_039a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0439: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_043e: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0452: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0464: Unknown result type (might be due to invalid IL or missing references)
		//IL_0470: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		if (!enablePlayerPhysics)
		{
			return;
		}
		for (int i = 0; i < seats.Length; i++)
		{
			VehicleSeat vehicleSeat = seats[i];
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (!Object.op_Implicit((Object)(object)seatedPlayer) || !PlayerPhysicsReady(seatedPlayer))
			{
				continue;
			}
			PlayerTumble tumble = seatedPlayer.tumble;
			tumble.physGrabObject.OverrideMaterial(SemiFunc.PhysicMaterialSlippery());
			tumble.physGrabObject.OverrideZeroGravity(0.2f);
			tumble.physGrabObject.impactDetector.ImpactDisable(0.2f);
			seatedPlayer.physGrabber.OverridePhysGrabForcesDisable(0.2f);
			if (seatedPlayer.isLocal)
			{
				tumble.OverrideTumbleUIDisable(0.2f);
			}
			if (loopModeOverrideTimer > 0f)
			{
				tumble.DisableCustomGravity(0.2f);
			}
			bool flag = loopModeOverrideTimer > 0f || Mathf.Abs(Mathf.DeltaAngle(((Component)this).transform.eulerAngles.z, 0f)) <= 10f;
			float num = Vector3.Distance(tumble.rb.position, vehicleSeat.seatTransform.position);
			if (!Object.op_Implicit((Object)(object)ArenaRace.instance) && num > 0.5f && vehicleSeat.reachedFirstMount && (Object)(object)vehicleSeat.firstMountTransform != (Object)null)
			{
				vehicleSeat.reachedFirstMount = false;
			}
			if (vehicleSeat.reachedFirstMount || (Object)(object)vehicleSeat.firstMountTransform == (Object)null)
			{
				if (!tumble.rb.isKinematic)
				{
					Vector3 pointVelocity = rb.GetPointVelocity(tumble.rb.position);
					tumble.rb.linearVelocity = pointVelocity;
					Vector3 val = vehicleSeat.seatTransform.position - tumble.rb.position;
					Rigidbody val2 = tumble.rb;
					val2.linearVelocity += val / Time.fixedDeltaTime * 0.5f;
					tumble.rb.angularVelocity = rb.angularVelocity;
					Vector3 val3 = SemiFunc.PhysFollowRotation(((Component)tumble).transform, vehicleSeat.seatTransform.rotation, tumble.rb, 1f);
					tumble.rb.AddTorque(val3, (ForceMode)1);
				}
			}
			else
			{
				Transform val4;
				if (!vehicleSeat.reachedFirstMount && (Object)(object)vehicleSeat.firstMountTransform != (Object)null)
				{
					val4 = vehicleSeat.firstMountTransform;
					if ((Vector3.Distance(tumble.rb.position, vehicleSeat.firstMountTransform.position) <= 0.05f) & flag)
					{
						vehicleSeat.reachedFirstMount = true;
					}
				}
				else
				{
					val4 = vehicleSeat.seatTransform;
				}
				Vector3 val5 = val4.position;
				Vector3 val6 = tumble.rb.position - vehicleSeat.seatTransform.position;
				Vector3 val7 = ((Component)this).transform.InverseTransformDirection(val6);
				if (num > 0.3f)
				{
					float num2 = Mathf.Max(0f, 0f - val7.z);
					float num3 = num2 * 0.3f;
					float x = val7.x;
					float num4 = Mathf.Sign(x) * Mathf.Max(0f, Mathf.Abs(x) - 0.2f) * 0.5f;
					if (Mathf.Abs(x) < 0.3f && num2 > 0.3f)
					{
						num4 = 0.5f;
					}
					val5 += ((Component)this).transform.up * num3 + ((Component)this).transform.right * num4;
				}
				Vector3 val8 = SemiFunc.PhysFollowPosition(tumble.rb.position, val5, tumble.rb.linearVelocity, vehicleSeat.followForce);
				tumble.rb.AddForce(val8 * vehicleSeat.followForce * Time.fixedDeltaTime, (ForceMode)1);
				Vector3 val9 = SemiFunc.PhysFollowRotation(((Component)tumble).transform, val4.rotation, tumble.rb, 3f);
				tumble.rb.AddTorque(val9, (ForceMode)1);
			}
			tumble.physGrabObject.OverrideMass(vehicleSeat.playerMass);
			if (vehicleUprightAngle < 90f)
			{
				bool num5 = isFrontGrounded && isBackGrounded;
				bool flag2 = groundSlopeValid && groundSlopePitchAngle < -3f;
				bool num6 = !num5 | flag2;
				Vector3 val10 = -vehicleSeat.seatTransform.up;
				Vector3 val11 = (num6 ? Vector3.up : VehicleUp);
				tumble.rb.AddForce(val10 * vehicleSeat.downSeatForce * 2f, (ForceMode)0);
				VehicleAddForce(val11 * vehicleSeat.downSeatForce * 2f, (ForceMode)0);
			}
		}
	}

	private void ApplyVelocityLimits()
	{
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0395: Unknown result type (might be due to invalid IL or missing references)
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0404: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_0466: Unknown result type (might be due to invalid IL or missing references)
		//IL_0470: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_050d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0512: Unknown result type (might be due to invalid IL or missing references)
		//IL_0516: Unknown result type (might be due to invalid IL or missing references)
		//IL_0521: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04db: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0619: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		//IL_065c: Unknown result type (might be due to invalid IL or missing references)
		//IL_066e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		if (hasAnySeatedPlayerCached || AIControlActive || AITumbleImmune)
		{
			seatedLimitsTimer = 1f;
			physGrabObject.OverrideZeroGravity(0.2f);
		}
		else if (seatedLimitsTimer > 0f)
		{
			seatedLimitsTimer -= Time.fixedDeltaTime;
		}
		float num = currentCustomGravitySpeed * Time.fixedDeltaTime;
		Vector3 val = ((loopModeOverrideTimer > 0f || ((Component)this).transform.forward.y > 0f) ? (-((Component)this).transform.up) : Vector3.down);
		Rigidbody val2 = rb;
		val2.linearVelocity += val * num;
		if (isDrifting)
		{
			Vector3 val3 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
			float num2 = Mathf.Clamp01(driftStartSpeed / (maxForwardSpeed + hyperMaxSpeed));
			float num3 = Mathf.Lerp(driftMaxSpeed * driftMinSpeedRatio, driftMaxSpeed, num2);
			float num4 = ((driftSpeedRampUpTime > 0f) ? Mathf.Clamp01(driftDurationTimer / driftSpeedRampUpTime) : 1f);
			float num5 = Mathf.Lerp(num3, driftMaxSpeed * 1.2f, num4);
			if (val3.magnitude > num5)
			{
				val3 = val3.normalized * num5;
				rb.linearVelocity = new Vector3(val3.x, rb.linearVelocity.y, val3.z);
			}
		}
		Vector3 val4 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
		float magnitude = val4.magnitude;
		float num6 = magnitude * 3.6f * 2f;
		bool num7 = (groundSlopeValid ? (groundSlopePitchAngle < -10f) : (lastValidSlopeTimer > 0f && lastValidSlopeAngle > 10f));
		float num8 = (groundSlopeValid ? Mathf.Abs(groundSlopePitchAngle) : lastValidSlopeAngle);
		float num9 = (num7 ? Mathf.Lerp(1f, 2f, Mathf.Clamp01(num8 / 45f)) : 1f);
		float num10 = maxSpeedKmh * catchUpMultiplier;
		float num11 = softMaxSpeedKmh * catchUpMultiplier;
		float num12 = (isVehicleAirborne ? 0.15f : 1f);
		Vector3 val5 = ((magnitude > 0.001f) ? (val4 / magnitude) : Vector3.zero);
		if (num6 > num10 * num9)
		{
			float num13 = num10 * num9 / 3.6f / 2f;
			float num14 = Mathf.Lerp(magnitude, num13, 1f - Mathf.Exp(-10f * num12 * Time.fixedDeltaTime));
			rb.linearVelocity = new Vector3(val5.x * num14, rb.linearVelocity.y, val5.z * num14);
		}
		else if (!isDrifting && num6 > num11 * num9)
		{
			float num15 = softMaxSpeedDecayRate * num12 * Time.fixedDeltaTime;
			float num16 = Mathf.MoveTowards(num6, num11 * num9, num15) / 3.6f / 2f;
			rb.linearVelocity = new Vector3(val5.x * num16, rb.linearVelocity.y, val5.z * num16);
		}
		if (!(seatedLimitsTimer > 0f))
		{
			return;
		}
		Vector3 val6 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
		if (val6.magnitude > maxRBVelocity)
		{
			val6 = val6.normalized * maxRBVelocity;
			rb.linearVelocity = new Vector3(val6.x, rb.linearVelocity.y, val6.z);
		}
		float num17 = maxRBVelocity;
		if (rb.linearVelocity.y > num17)
		{
			rb.linearVelocity = new Vector3(rb.linearVelocity.x, num17, rb.linearVelocity.z);
		}
		bool flag = groundSlopeValid && Mathf.Abs(groundSlopePitchAngle) > 10f;
		if (rb.linearVelocity.y < 0f - num17 && !flag)
		{
			rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f - num17, rb.linearVelocity.z);
		}
		Vector3 angularVelocity = rb.angularVelocity;
		if (angularVelocity.magnitude > maxRBAngularVelocity)
		{
			Rigidbody val7 = rb;
			angularVelocity = rb.angularVelocity;
			val7.angularVelocity = angularVelocity.normalized * maxRBAngularVelocity;
		}
		if (isVehicleAirborne)
		{
			return;
		}
		if (!groundSlopeValid || !(groundSlopePitchAngle < -10f))
		{
			float magnitude2 = horizontalVelocity.magnitude;
			float num18 = Mathf.InverseLerp(downwardVelocityMinSpeed, downwardVelocityMaxSpeed, magnitude2);
			float num19 = Mathf.Lerp(maxDownwardVelocity, minDownwardVelocity, num18);
			if (rb.linearVelocity.y < 0f - num19)
			{
				rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f - num19, rb.linearVelocity.z);
			}
		}
		if (wallAheadScale < 0.5f && rb.linearVelocity.y > 1f && (!groundSlopeValid || !(groundSlopePitchAngle > 10f)))
		{
			float num20 = Mathf.Lerp(rb.linearVelocity.y, Mathf.Min(rb.linearVelocity.y, 1f), 1f - wallAheadScale);
			rb.linearVelocity = new Vector3(rb.linearVelocity.x, num20, rb.linearVelocity.z);
		}
	}

	private void UpdateSteering()
	{
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)steeringTransform))
		{
			return;
		}
		PlayerAvatar driver = Driver;
		bool flag = AIControlActive || (Object.op_Implicit((Object)(object)driver) && DriverFullyMounted);
		if ((currentState == State.Active) & flag)
		{
			Vector3 controlCameraForward = ControlCameraForward;
			controlCameraForward.y = 0f;
			controlCameraForward.Normalize();
			Vector3 forward = ((Component)this).transform.forward;
			forward.y = 0f;
			forward.Normalize();
			float controlInputX = ControlInputX;
			if (Mathf.Abs(controlInputX) > 0.1f)
			{
				Vector3 val = Quaternion.AngleAxis(controlInputX * manualTurnExtraAngle, Vector3.up) * controlCameraForward;
				targetSteeringAngle = Vector3.SignedAngle(forward, val, Vector3.up);
			}
			else
			{
				targetSteeringAngle = Vector3.SignedAngle(forward, controlCameraForward, Vector3.up);
			}
			targetSteeringAngle = Mathf.Clamp(targetSteeringAngle, 0f - maxSteeringAngle, maxSteeringAngle);
		}
		else
		{
			targetSteeringAngle = 0f;
		}
		float num = ((driftBoostActiveTimer > 0f) ? 6f : 1f);
		currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, 1f - Mathf.Exp(-8f * num * Time.deltaTime));
		Quaternion targetRotation = Quaternion.Euler(0f, currentSteeringAngle, 0f);
		Quaternion val2 = SemiFunc.SpringQuaternionGet(steeringSpring, targetRotation);
		steeringTransform.localRotation = steeringOriginalRotation * val2;
	}

	public void GrabSeat(int _seatIndex)
	{
		if (_seatIndex < 0 || _seatIndex >= seats.Length || _seatIndex >= grabArea.grabAreas.Count || currentState == State.Tumbled || AIControlActive || (destructionStates != null && destructionStates.Length != 0 && currentDestructionStateIndex == destructionStates.Length - 1))
		{
			return;
		}
		VehicleSeat vehicleSeat = seats[_seatIndex];
		if (vehicleSeat.grabbedTimer <= 0f)
		{
			List<PhysGrabber> listOfGrabbers = grabArea.grabAreas[_seatIndex].listOfGrabbers;
			if (listOfGrabbers.Count == 0)
			{
				return;
			}
			PlayerAvatar playerAvatar = listOfGrabbers[listOfGrabbers.Count - 1].playerAvatar;
			if (SemiFunc.IsMultiplayer())
			{
				if (SemiFunc.IsMasterClientOrSingleplayer())
				{
					photonView.RPC("SetSeatGrabberRPC", (RpcTarget)0, new object[2]
					{
						_seatIndex,
						playerAvatar.photonView.ViewID
					});
				}
				else
				{
					photonView.RPC("RequestSeatRPC", (RpcTarget)2, new object[2]
					{
						_seatIndex,
						playerAvatar.photonView.ViewID
					});
				}
			}
			else
			{
				SetSeatGrabber(_seatIndex, playerAvatar);
			}
		}
		vehicleSeat.grabbedTimer = 0.5f;
	}

	public void GrabSeat0()
	{
		GrabSeat(0);
	}

	public void GrabSeat1()
	{
		GrabSeat(1);
	}

	public void GrabSeat2()
	{
		GrabSeat(2);
	}

	public void GrabSeat3()
	{
		GrabSeat(3);
	}

	public void ForceSeatPlayer(int _seatIndex, PlayerAvatar _player)
	{
		if (_seatIndex >= 0 && _seatIndex < seats.Length && !((Object)(object)seats[_seatIndex].seatedPlayer == (Object)(object)_player))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetSeatGrabberRPC", (RpcTarget)0, new object[2]
				{
					_seatIndex,
					_player.photonView.ViewID
				});
			}
			else
			{
				SetSeatGrabber(_seatIndex, _player);
			}
		}
	}

	public void TumbleVehicle(float _time)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Tumbled || isFullyDestroyed || AITumbleImmune)
		{
			return;
		}
		tumbleTimer = _time;
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("TumbleVehicleRPC", (RpcTarget)0, new object[1] { _time });
			}
			else
			{
				TumbleVehicleRPC(_time);
			}
		}
	}

	private void PlayHonk(int _seatIndex)
	{
		PlayHonkInternal(GetHonkPitchForSeat(_seatIndex), _seatIndex);
	}

	internal void AIHonk()
	{
		float num = Random.Range(aiHonkPitchMin, aiHonkPitchMax);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("AIHonkRPC", (RpcTarget)0, new object[1] { num });
		}
		else
		{
			PlayHonkInternal(num, -1);
		}
	}

	[PunRPC]
	private void AIHonkRPC(float _pitch, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			PlayHonkInternal(_pitch, -1);
		}
	}

	private void PlayHonkInternal(float _pitch, int _seatIndex)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			EnemyDirector.instance.SetInvestigate(((Component)this).transform.position, 35f);
		}
		honkSound.Pitch = _pitch;
		activeHonkSource = honkSound.Play(((Component)this).transform, soundEffectVolume);
		activeHonkSeatIndex = _seatIndex;
		activeHonkPitch = _pitch;
	}

	private float GetHonkPitchForSeat(int _seatIndex)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (_seatIndex < 0 || _seatIndex >= seats.Length)
		{
			return 1f;
		}
		PlayerAvatar seatedPlayer = seats[_seatIndex].seatedPlayer;
		if (!Object.op_Implicit((Object)(object)seatedPlayer) || !Object.op_Implicit((Object)(object)seatedPlayer.localCamera))
		{
			return 1f;
		}
		Vector3 forward = seatedPlayer.localCamera.GetOverrideTransform().forward;
		if (forward.y >= 0f)
		{
			return Mathf.Lerp(1.1f, 2.2f, forward.y);
		}
		return Mathf.Lerp(0.7f, 1.1f, forward.y + 1f);
	}

	private void UpdateHonkPitch()
	{
		if (Object.op_Implicit((Object)(object)activeHonkSource))
		{
			if (!activeHonkSource.isPlaying)
			{
				activeHonkSource = null;
			}
			else if (activeHonkSeatIndex >= 0 && activeHonkSeatIndex < seats.Length && Object.op_Implicit((Object)(object)seats[activeHonkSeatIndex].seatedPlayer))
			{
				float honkPitchForSeat = GetHonkPitchForSeat(activeHonkSeatIndex);
				float num = 1f - Mathf.Exp(-12f * Time.deltaTime);
				activeHonkPitch = Mathf.Lerp(activeHonkPitch, honkPitchForSeat, num);
				activeHonkSource.pitch = activeHonkPitch;
			}
		}
	}

	[PunRPC]
	public void SetSeatGrabberRPC(int _seatIndex, int _playerAvatarPhotonID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_playerAvatarPhotonID);
			if (Object.op_Implicit((Object)(object)playerAvatar))
			{
				SetSeatGrabber(_seatIndex, playerAvatar);
			}
		}
	}

	[PunRPC]
	private void SetStateRPC(int _newStateInt, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			if (_newStateInt == 1 && currentState != State.Active)
			{
				engineStartSound.Play(((Component)this).transform, soundEffectVolume);
			}
			else if (_newStateInt != 1 && currentState == State.Active)
			{
				engineStopSound.Play(((Component)this).transform, soundEffectVolume);
			}
			stateStart = true;
			currentState = (State)_newStateInt;
		}
	}

	[PunRPC]
	private void AirborneWindRPC(bool _active, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			airborneWindActive = _active;
		}
	}

	[PunRPC]
	private void TumbleVehicleRPC(float _time, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && !AITumbleImmune)
		{
			if (currentState != State.Tumbled)
			{
				RiderCameraShake(1.5f, 3f, 0.1f);
			}
			tumbleTimer = _time;
			stateStart = true;
			currentState = State.Tumbled;
		}
	}

	[PunRPC]
	private void RequestSeatRPC(int _seatIndex, int _playerAvatarPhotonID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && _seatIndex >= 0 && _seatIndex < seats.Length && !(seats[_seatIndex].grabbedTimer > 0f) && currentState != State.Tumbled && !AIControlActive)
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_playerAvatarPhotonID);
			if (Object.op_Implicit((Object)(object)playerAvatar) && SemiFunc.OwnerOnlyRPC(_info, playerAvatar.photonView))
			{
				photonView.RPC("SetSeatGrabberRPC", (RpcTarget)0, new object[2] { _seatIndex, _playerAvatarPhotonID });
				seats[_seatIndex].grabbedTimer = 0.5f;
			}
		}
	}

	[PunRPC]
	private void FinishSeatMountRPC(int seatIndex, int playerPhotonID, PhotonMessageInfo info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(info) && seatIndex >= 0 && seatIndex < seats.Length)
		{
			VehicleSeat vehicleSeat = seats[seatIndex];
			if (Object.op_Implicit((Object)(object)vehicleSeat.seatedPlayer) && vehicleSeat.seatedPlayer.photonView.ViewID == playerPhotonID)
			{
				vehicleSeat.reachedFirstMount = true;
				vehicleSeat.mountFinishTimer = 0f;
			}
		}
	}

	[PunRPC]
	private void ClearSeatRPC(int _seatIndex, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info) || _seatIndex < 0 || _seatIndex >= seats.Length || Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			return;
		}
		VehicleSeat vehicleSeat = seats[_seatIndex];
		if (Object.op_Implicit((Object)(object)vehicleSeat.seatedPlayer))
		{
			vehicleSeat.seatedPlayer.playerAvatarVisuals.MountSeatVisualClear();
		}
		vehicleSeat.seatedPlayer = null;
		vehicleSeat.mountFinishTimer = 0f;
		vehicleSeat.mountSettleTimer = 0f;
		vehicleSeat.reachedFirstMount = false;
		vehicleSeat.grabbedTimer = 0f;
		if (_seatIndex == 0)
		{
			GamepadInputReset();
			speedOverTime = 0f;
			hyperSpeedOverTime = 0f;
			previousVelocityMagnitude = 0f;
			driftTimer = 0f;
			driftIntensity = 0f;
			frontUngroundedTimer = 0f;
			backUngroundedTimer = 0f;
			isFrontColliding = false;
			isBackColliding = false;
			if (isDrifting)
			{
				SetDriftingLocal(_drifting: false);
			}
			driftPausedByAirborne = false;
			driftBoostGraceTimer = 0f;
			driftExitDelayTimer = 0f;
			driftExitPendingBoostLevel = 0;
			driftBoostLevel = 0;
			driftBoostProgress = 0f;
		}
	}

	[PunRPC]
	private void SetDriftingRPC(bool _drifting, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			SetDriftingLocal(_drifting);
		}
	}

	private void SetDriftingLocal(bool _drifting)
	{
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
		if (isDrifting == _drifting)
		{
			return;
		}
		isDrifting = _drifting;
		if (isDrifting)
		{
			if (!SemiFunc.IsMasterClientOrSingleplayer())
			{
				driftLowSpeedEaseProgress = 1f;
			}
			driftBoostActiveTimer = 0f;
			if (Object.op_Implicit((Object)(object)driftParticles))
			{
				driftParticles.Play(true);
			}
			if (Object.op_Implicit((Object)(object)driftBoostLevel0ReachedParticles))
			{
				driftBoostLevel0ReachedParticles.Play(true);
			}
			driftStartSound.Play(((Component)this).transform, soundEffectVolume);
			driftLightFadingIn = true;
			if (Object.op_Implicit((Object)(object)driftLight))
			{
				((Behaviour)driftLight).enabled = true;
			}
			if (this.driftLightSettings != null && this.driftLightSettings.Length != 0)
			{
				DriftLightSettings driftLightSettings = this.driftLightSettings[0];
				driftLightCurrentColor = driftLightSettings.color;
				driftLightCurrentIntensity = driftLightSettings.intensity;
				driftLightCurrentFlickerAmount = driftLightSettings.flickerAmount;
				driftLightCurrentFlickerSpeed = driftLightSettings.flickerSpeed;
				driftLightCurrentPulseIntensity = driftLightSettings.pulseIntensity;
				driftLightCurrentPulseSpeed = driftLightSettings.pulseSpeed;
			}
			if (this.driftLevelSettings != null && this.driftLevelSettings.Length != 0)
			{
				DriftLevelSettings driftLevelSettings = this.driftLevelSettings[0];
				driftCurrentRollAngle = driftLevelSettings.rollAngle;
				driftCurrentYawOffset = driftLevelSettings.yawOffset;
				driftCurrentMeshHorizontalOffset = driftLevelSettings.meshHorizontalOffset;
				driftCurrentMeshYRotationOffset = driftLevelSettings.meshYRotationOffset;
				driftCurrentMeshDownOffset = driftLevelSettings.meshDownOffset;
				driftCurrentMeshForwardOffset = driftLevelSettings.meshForwardOffset;
				driftMeshHorizontalVelocity = 0f;
				driftMeshYRotationVelocity = 0f;
				driftMeshDownVelocity = 0f;
				driftMeshForwardVelocity = 0f;
			}
			driftExitSnapTimer = 0f;
			driftDurationTimer = 0f;
			driftOriginalAngularDrag = rb.angularDamping;
			if (driftBoostGraceTimer > 0f)
			{
				driftExitDelayTimer = 0f;
				driftExitPendingBoostLevel = 0;
				driftBoostGraceTimer = 0f;
			}
			else
			{
				driftBoostLevel = 0;
				driftBoostProgress = 0f;
			}
			if (driftPausedByAirborne)
			{
				driftBoostLevel = driftPausedBoostLevel;
				driftBoostProgress = driftPausedBoostProgress;
				driftPausedByAirborne = false;
				if (driftBoostLevel >= 3 && Object.op_Implicit((Object)(object)driftParticlesLevel3))
				{
					driftParticlesLevel3.Play(true);
				}
				else if (driftBoostLevel >= 2 && Object.op_Implicit((Object)(object)driftParticlesLevel2))
				{
					driftParticlesLevel2.Play(true);
				}
				else if (driftBoostLevel >= 1 && Object.op_Implicit((Object)(object)driftParticlesLevel1))
				{
					driftParticlesLevel1.Play(true);
				}
			}
			Vector3 val = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
			driftStartSpeed = val.magnitude;
			driftSpeedSampleSum = 0f;
			driftSpeedSampleCount = 0;
			return;
		}
		if (Object.op_Implicit((Object)(object)driftParticles))
		{
			driftParticles.Stop(true);
		}
		if (Object.op_Implicit((Object)(object)driftParticlesLevel1))
		{
			driftParticlesLevel1.Stop(true);
		}
		if (Object.op_Implicit((Object)(object)driftParticlesLevel2))
		{
			driftParticlesLevel2.Stop(true);
		}
		if (Object.op_Implicit((Object)(object)driftParticlesLevel3))
		{
			driftParticlesLevel3.Stop(true);
		}
		driftLightFadingIn = false;
		smoothDragCurrent = driftDragDefault;
		smoothDragTarget = driftDragDefault;
		smoothDragVelocity = 0f;
		smoothAngularDragCurrent = ((driftOriginalAngularDrag > 0f) ? driftOriginalAngularDrag : rb.angularDamping);
		smoothAngularDragTarget = smoothAngularDragCurrent;
		smoothAngularDragVelocity = 0f;
		if (!driftPausedByAirborne)
		{
			driftEndSound.Play(((Component)this).transform, soundEffectVolume);
			driftCooldownTimer = 0.2f;
			if (Mathf.Abs(Vector3.SignedAngle(((Component)this).transform.up, VehicleUp, ((Component)this).transform.forward)) > 25f && driftDurationTimer > 0.2f)
			{
				driftExitSnapTimer = 1f;
			}
		}
		float num = ((driftSpeedSampleCount > 0) ? (driftSpeedSampleSum / (float)driftSpeedSampleCount) : 0f);
		driftExitPendingSpeedRestore = num < driftMaxSpeed * 0.5f;
		driftExitPendingRestoreSpeed = driftStartSpeed;
		driftExitPendingBoostLevel = driftBoostLevel;
		driftExitDelayTimer = 0.2f;
		driftBoostGraceTimer = 0.5f;
		driftBoostLevel = 0;
		driftBoostProgress = 0f;
	}

	private void ApplyDriftBoost()
	{
		if (driftBoostLevel > 0)
		{
			float num = 0f;
			float num2 = 0f;
			switch (driftBoostLevel)
			{
			case 1:
				num = driftBoostLevel1ForwardBoost;
				_ = driftBoostLevel1Duration;
				num2 = driftBoostLevel1SpeedBoostMultiplier;
				break;
			case 2:
				num = driftBoostLevel2ForwardBoost;
				_ = driftBoostLevel2Duration;
				num2 = driftBoostLevel2SpeedBoostMultiplier;
				break;
			case 3:
				num = driftBoostLevel3ForwardBoost;
				_ = driftBoostLevel3Duration;
				num2 = driftBoostLevel3SpeedBoostMultiplier;
				break;
			}
			driftBoostActiveForce += num;
			driftBoostActiveTimer = Mathf.Max(driftBoostActiveTimer, driftBoostConstantDuration + driftBoostFadeDuration);
			smoothDragCurrent = driftDragDefault;
			smoothDragTarget = driftDragDefault;
			smoothDragVelocity = 0f;
			smoothAngularDragCurrent = ((driftOriginalAngularDrag > 0f) ? driftOriginalAngularDrag : rb.angularDamping);
			smoothAngularDragTarget = smoothAngularDragCurrent;
			smoothAngularDragVelocity = 0f;
			driftBoostActiveMultiplier = num2;
			driftBoostActivatedLevel = driftBoostLevel;
		}
	}

	private void DriftBoostEffects(int _level)
	{
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		if (_level >= 1 && _level <= 3)
		{
			float num = Mathf.Lerp(0.75f, 2f, (float)(_level - 1) * 0.5f);
			RiderCameraShake(num, num * 2f, 0.1f);
		}
		switch (_level)
		{
		case 1:
			driftBoostLevel1Sound.Play(((Component)this).transform, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)driftBoostLevel1Particles))
			{
				driftBoostLevel1Particles.Play(true);
			}
			break;
		case 2:
			driftBoostLevel2Sound.Play(((Component)this).transform, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)driftBoostLevel2Particles))
			{
				driftBoostLevel2Particles.Play(true);
			}
			break;
		case 3:
			driftBoostLevel3Sound.Play(((Component)this).transform, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)driftBoostLevel3Particles))
			{
				driftBoostLevel3Particles.Play(true);
			}
			break;
		}
		if (Object.op_Implicit((Object)(object)boostLight))
		{
			boostLightTimer = boostLightDuration;
			boostLightActiveLevel = _level;
			int num2 = Mathf.Clamp(_level, 0, driftLightSettings.Length - 1);
			boostLightTargetColor = driftLightSettings[num2].color;
			switch (_level)
			{
			case 1:
				boostLightBaseIntensity = boostLightIntensityLevel1;
				break;
			case 2:
				boostLightBaseIntensity = boostLightIntensityLevel2;
				break;
			case 3:
				boostLightBaseIntensity = boostLightIntensityLevel3;
				break;
			default:
				boostLightBaseIntensity = boostLightIntensityLevel1;
				break;
			}
			boostLight.color = boostLightTargetColor;
			((Behaviour)boostLight).enabled = true;
		}
	}

	[PunRPC]
	private void DriftBoostEffectsRPC(int _level, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			DriftBoostEffects(_level);
		}
	}

	private void UpdateDriftSpeedSampling()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (isDrifting)
		{
			Vector3 val = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
			driftSpeedSampleSum += val.magnitude;
			driftSpeedSampleCount++;
		}
	}

	private void UpdateDriftExitDelay()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		if (driftExitDelayTimer <= 0f || isVehicleAirborne || isDrifting || (HasDriverControl && Mathf.Abs(ControlInputX) > 0.1f))
		{
			return;
		}
		driftExitDelayTimer -= Time.fixedDeltaTime;
		if (driftExitDelayTimer > 0f)
		{
			return;
		}
		if (driftExitPendingBoostLevel > 0)
		{
			driftBoostActiveDirection = ((Component)this).transform.forward;
			driftBoostLevel = driftExitPendingBoostLevel;
			ApplyDriftBoost();
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("DriftBoostEffectsRPC", (RpcTarget)0, new object[1] { driftExitPendingBoostLevel });
			}
			else
			{
				DriftBoostEffects(driftExitPendingBoostLevel);
			}
			driftBoostLevel = 0;
			driftBoostProgress = 0f;
			driftBoostGraceTimer = 0f;
		}
		if (driftExitPendingSpeedRestore)
		{
			Vector3 forward = ((Component)this).transform.forward;
			forward.y = 0f;
			forward.Normalize();
			Vector3 val = forward * driftExitPendingRestoreSpeed;
			rb.linearVelocity = new Vector3(val.x, rb.linearVelocity.y, val.z);
		}
		if (driftExitBoost > 0f)
		{
			Vector3 force = ((Component)this).transform.forward * driftExitBoost;
			Vector3 force2 = FilterUpwardForce(force);
			VehicleAddForce(force2, (ForceMode)2);
		}
		driftExitPendingBoostLevel = 0;
		driftExitPendingSpeedRestore = false;
	}

	private void UpdateDriftBoostProgress(float _dragBlend)
	{
		if (!isDrifting)
		{
			if (driftBoostGraceTimer > 0f)
			{
				if (!isVehicleAirborne)
				{
					driftBoostGraceTimer -= Time.fixedDeltaTime;
				}
				if (driftBoostGraceTimer <= 0f)
				{
					driftBoostProgress = 0f;
					driftBoostLevel = 0;
				}
			}
			else
			{
				driftBoostProgress = 0f;
				driftBoostLevel = 0;
			}
			return;
		}
		driftBoostGraceTimer = 0f;
		if ((Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f) < 2f)
		{
			return;
		}
		float num = Mathf.Lerp(driftBoostTimeAtLowSpeed, driftBoostTimeAtFullSpeed, _dragBlend);
		if (num <= 0f)
		{
			num = 1f;
		}
		driftBoostProgress += Time.fixedDeltaTime / num;
		driftBoostProgress = Mathf.Clamp01(driftBoostProgress);
		int num2 = driftBoostLevel;
		if (driftBoostProgress >= driftBoostLevel3Threshold)
		{
			driftBoostLevel = 3;
		}
		else if (driftBoostProgress >= driftBoostLevel2Threshold)
		{
			driftBoostLevel = 2;
		}
		else if (driftBoostProgress >= driftBoostLevel1Threshold)
		{
			driftBoostLevel = 1;
		}
		else
		{
			driftBoostLevel = 0;
		}
		if (driftBoostLevel > num2)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("DriftBoostLevelRPC", (RpcTarget)0, new object[1] { driftBoostLevel });
			}
			else
			{
				OnDriftBoostLevelReached(driftBoostLevel);
			}
		}
	}

	[PunRPC]
	private void DriftBoostLevelRPC(int _level, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			driftBoostLevel = _level;
			OnDriftBoostLevelReached(_level);
		}
	}

	private void OnDriftBoostLevelReached(int _level)
	{
		switch (_level)
		{
		case 1:
			driftBoostLevel1ReachedSound.Play(((Component)this).transform, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)driftBoostLevel1ReachedParticles))
			{
				driftBoostLevel1ReachedParticles.Play(true);
			}
			if (Object.op_Implicit((Object)(object)driftParticlesLevel1))
			{
				driftParticlesLevel1.Play(true);
			}
			break;
		case 2:
			driftBoostLevel2ReachedSound.Play(((Component)this).transform, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)driftBoostLevel2ReachedParticles))
			{
				driftBoostLevel2ReachedParticles.Play(true);
			}
			if (Object.op_Implicit((Object)(object)driftParticlesLevel1))
			{
				driftParticlesLevel1.Stop(true);
			}
			if (Object.op_Implicit((Object)(object)driftParticlesLevel2))
			{
				driftParticlesLevel2.Play(true);
			}
			break;
		case 3:
			driftBoostLevel3ReachedSound.Play(((Component)this).transform, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)driftBoostLevel3ReachedParticles))
			{
				driftBoostLevel3ReachedParticles.Play(true);
			}
			if (Object.op_Implicit((Object)(object)driftParticlesLevel2))
			{
				driftParticlesLevel2.Stop(true);
			}
			if (Object.op_Implicit((Object)(object)driftParticlesLevel3))
			{
				driftParticlesLevel3.Play(true);
			}
			break;
		}
	}

	private void UpdateDriftBoostActive()
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (driftBoostActiveTimer <= 0f)
		{
			driftBoostActiveForce = 0f;
			return;
		}
		driftBoostActiveTimer -= Time.fixedDeltaTime;
		float num = 1f;
		float num2 = driftBoostActiveTimer;
		if (num2 <= driftBoostFadeDuration)
		{
			num = num2 / driftBoostFadeDuration;
		}
		if (driftBoostActiveForce > 0f)
		{
			Vector3 force = ((torqueOverrideTimer > 0f) ? driftBoostActiveDirection : ((Component)this).transform.forward) * driftBoostActiveForce * num;
			Vector3 force2 = FilterUpwardForce(force);
			VehicleAddForce(force2, (ForceMode)5);
		}
		float num3 = driftBoostActiveMultiplier * Time.fixedDeltaTime;
		float num4 = (isDrifting ? 0.5f : 1f);
		hyperSpeedOverTime += driftBoostHyperSpeedRate * catchUpMultiplier * num3 * num4 * forceMultiplier;
		speedOverTime += driftBoostSpeedOverTimeRate * catchUpMultiplier * num3 * num4 * forceMultiplier;
		hyperSpeedOverTime = Mathf.Min(hyperSpeedOverTime, hyperMaxSpeed);
		speedOverTime = Mathf.Min(speedOverTime, maxForwardSpeed);
	}

	private void UpdateSmoothDrag()
	{
		_ = smoothDragCurrent;
		if (!isDrifting)
		{
			smoothDragTarget = driftDragDefault;
			smoothAngularDragTarget = ((driftOriginalAngularDrag > 0f) ? driftOriginalAngularDrag : rb.angularDamping);
		}
		smoothDragCurrent = Mathf.SmoothDamp(smoothDragCurrent, smoothDragTarget, ref smoothDragVelocity, driftDragSmoothTime);
		smoothAngularDragCurrent = Mathf.SmoothDamp(smoothAngularDragCurrent, smoothAngularDragTarget, ref smoothAngularDragVelocity, driftDragSmoothTime);
		if (!isDrifting)
		{
			physGrabObject.OverrideDrag(smoothDragCurrent, 0.05f);
			physGrabObject.OverrideAngularDrag(smoothAngularDragCurrent, 0.05f);
		}
	}

	[PunRPC]
	private void ImpactEffectRPC(int _impactTypeInt, Vector3 _position, Vector3 _velocityDirection, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		switch ((ImpactType)_impactTypeInt)
		{
		case ImpactType.Small:
		{
			Color gray = Color.cyan;
			break;
		}
		case ImpactType.Medium:
		{
			Color gray = Color.blue;
			break;
		}
		case ImpactType.Big:
		{
			Color gray = new Color(0.5f, 0f, 1f);
			break;
		}
		default:
		{
			Color gray = Color.gray;
			break;
		}
		}
		if (Object.op_Implicit((Object)(object)impactTransform))
		{
			impactTransform.position = _position;
			if (_velocityDirection.sqrMagnitude > 0.01f)
			{
				impactTransform.rotation = Quaternion.LookRotation(_velocityDirection);
			}
		}
		ImpactEffect((ImpactType)_impactTypeInt);
	}

	[PunRPC]
	private void HonkRPC(int _seatIndex, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (_seatIndex >= 0 && _seatIndex < seats.Length)
		{
			PlayerAvatar seatedPlayer = seats[_seatIndex].seatedPlayer;
			if (Object.op_Implicit((Object)(object)seatedPlayer) && SemiFunc.OwnerOnlyRPC(_info, seatedPlayer.photonView))
			{
				PlayHonk(_seatIndex);
			}
		}
	}

	private void SetSeatGrabber(int _seatIndex, PlayerAvatar _playerAvatar)
	{
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		if (_seatIndex < 0 || _seatIndex >= seats.Length || AIControlActive)
		{
			return;
		}
		if (_seatIndex == 0)
		{
			GamepadInputReset();
		}
		VehicleSeat obj = seats[_seatIndex];
		obj.seatedPlayer = _playerAvatar;
		obj.reachedFirstMount = false;
		if (Object.op_Implicit((Object)(object)_playerAvatar))
		{
			if ((SemiFunc.IsMasterClientOrSingleplayer() || _playerAvatar.isLocal) && Object.op_Implicit((Object)(object)_playerAvatar.tumble))
			{
				_playerAvatar.tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
			}
			if (_playerAvatar.isLocal)
			{
				PlayerController.instance.tumbleInputDisableTimer = 1f;
			}
			if (Object.op_Implicit((Object)(object)_playerAvatar.tumble))
			{
				_playerAvatar.tumble.physGrabObject.impactDetector.ImpactDisable(0.5f);
			}
		}
		if (Object.op_Implicit((Object)(object)physGrabObject) && Object.op_Implicit((Object)(object)physGrabObject.impactDetector))
		{
			physGrabObject.impactDetector.ImpactDisable(0.5f);
		}
		obj.takeASeatTimer = 1f;
		obj.mountFinishTimer = 6f;
		obj.mountSettleTimer = 0f;
		if (_seatIndex == 0 && steeringSpring != null)
		{
			currentSteeringAngle = 0f;
			targetSteeringAngle = 0f;
			steeringSpring.setup = false;
			steeringSpring.springVelocity = Vector3.zero;
		}
	}

	private void ClearSeat(int _seatIndex)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (_seatIndex >= 0 && _seatIndex < seats.Length && !Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ClearSeatRPC", (RpcTarget)0, new object[1] { _seatIndex });
			}
			else
			{
				ClearSeatRPC(_seatIndex);
			}
		}
	}

	private void SetState(State _newState)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		if (_newState != currentState && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SetStateRPC", (RpcTarget)0, new object[1] { (int)_newState });
			}
			else
			{
				SetStateRPC((int)_newState);
			}
		}
	}

	private void InitializeDestructionStates()
	{
		if (destructionStates != null && destructionStates.Length != 0)
		{
			int destructionStateFromBattery = GetDestructionStateFromBattery();
			if (destructionStateFromBattery >= 0)
			{
				SetDestructionStateLocal(destructionStateFromBattery);
			}
		}
	}

	private int GetDestructionStateFromBattery()
	{
		if (destructionStates == null || destructionStates.Length == 0)
		{
			return -1;
		}
		if (!Object.op_Implicit((Object)(object)itemBattery))
		{
			return 0;
		}
		float batteryLife = itemBattery.batteryLife;
		if (batteryLife <= 0f)
		{
			return destructionStates.Length - 1;
		}
		if (batteryLife >= 100f)
		{
			return 0;
		}
		float num = 100f / (float)(destructionStates.Length - 1);
		return Mathf.Clamp(Mathf.FloorToInt((100f - batteryLife) / num), 0, destructionStates.Length - 2);
	}

	private void UpdateDestructionState()
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		if (destructionStates == null || destructionStates.Length == 0 || !Object.op_Implicit((Object)(object)itemBattery))
		{
			return;
		}
		int num = destructionStates.Length - 1;
		bool flag = currentDestructionStateIndex == num;
		if (itemBattery.batteryLife > 0f && itemBattery.batteryLife <= 20f && !flag)
		{
			OverrideDamageSmoke(0.2f);
		}
		if (fireWarningTimer > 0f)
		{
			if (!(itemBattery.batteryLife > 0f))
			{
				OverrideDamageFire(0.2f);
				fireWarningTimer -= Time.deltaTime;
				if (fireWarningTimer <= 0f)
				{
					ChangeDestructionState(num);
				}
				return;
			}
			fireWarningTimer = 0f;
		}
		if (!canTriggerExplosion && itemBattery.batteryLife >= 20f)
		{
			canTriggerExplosion = true;
		}
		if (itemBattery.batteryLife <= 0f && !flag && fireWarningTimer <= 0f && vehicleExistenceTimer >= 3f && canTriggerExplosion)
		{
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				float num2 = Random.Range(4f, 8f);
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("StartFireWarningRPC", (RpcTarget)0, new object[1] { num2 });
				}
				else
				{
					StartFireWarningRPC(num2);
				}
			}
			prevBatteryLife = itemBattery.batteryLife;
		}
		else if (itemBattery.batteryLife != prevBatteryLife)
		{
			int destructionStateFromBattery = GetDestructionStateFromBattery();
			if (destructionStateFromBattery != currentDestructionStateIndex)
			{
				ChangeDestructionState(destructionStateFromBattery);
			}
			prevBatteryLife = itemBattery.batteryLife;
		}
	}

	[PunRPC]
	private void StartFireWarningRPC(float _delay, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			fireWarningTimer = _delay;
		}
	}

	private void SetDestructionStateLocal(int _stateIndex)
	{
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		if (destructionStates == null || destructionStates.Length == 0 || _stateIndex < 0 || _stateIndex >= destructionStates.Length)
		{
			return;
		}
		for (int i = 0; i < destructionStates.Length; i++)
		{
			DestructionState destructionState = destructionStates[i];
			if (destructionState.meshesToActivate == null)
			{
				continue;
			}
			bool active = i == _stateIndex;
			for (int j = 0; j < destructionState.meshesToActivate.Length; j++)
			{
				if ((Object)(object)destructionState.meshesToActivate[j] != (Object)null)
				{
					destructionState.meshesToActivate[j].SetActive(active);
				}
			}
		}
		if (_stateIndex == destructionStates.Length - 1)
		{
			if (Object.op_Implicit((Object)(object)grabMesh))
			{
				((Component)grabMesh).gameObject.SetActive(false);
			}
			isFullyDestroyed = true;
			canTriggerExplosion = false;
			if (Object.op_Implicit((Object)(object)frontLight))
			{
				((Behaviour)frontLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)backLight))
			{
				((Behaviour)backLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)driftLight))
			{
				((Behaviour)driftLight).enabled = false;
			}
			Material val = EnsureFrontLightMaterial();
			if (Object.op_Implicit((Object)(object)val))
			{
				val.SetColor(emissionColorID, Color.black);
			}
			Material val2 = EnsureBackLightMaterial();
			if (Object.op_Implicit((Object)(object)val2))
			{
				val2.SetColor(emissionColorID, Color.black);
			}
			if (Object.op_Implicit((Object)(object)valuableBox))
			{
				((Component)valuableBox).gameObject.SetActive(false);
			}
		}
		else
		{
			if (Object.op_Implicit((Object)(object)grabMesh))
			{
				((Component)grabMesh).gameObject.SetActive(true);
			}
			isFullyDestroyed = false;
			if (Object.op_Implicit((Object)(object)frontLight))
			{
				((Behaviour)frontLight).enabled = true;
			}
			if (Object.op_Implicit((Object)(object)backLight))
			{
				((Behaviour)backLight).enabled = true;
			}
			if (Object.op_Implicit((Object)(object)driftLight))
			{
				((Behaviour)driftLight).enabled = hasDrifting && (driftLightFadingIn || driftLightLerp > 0f);
			}
		}
		currentDestructionStateIndex = _stateIndex;
	}

	private void ChangeDestructionState(int _stateIndex)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		if (destructionStates != null && destructionStates.Length != 0 && _stateIndex >= 0 && _stateIndex < destructionStates.Length && _stateIndex != currentDestructionStateIndex && SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ChangeDestructionStateRPC", (RpcTarget)0, new object[1] { _stateIndex });
			}
			else
			{
				ChangeDestructionStateRPC(_stateIndex);
			}
		}
	}

	[PunRPC]
	private void ChangeDestructionStateRPC(int _stateIndex, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_047b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.MasterOnlyRPC(_info) || destructionStates == null || destructionStates.Length == 0 || _stateIndex < 0 || _stateIndex >= destructionStates.Length)
		{
			return;
		}
		if (currentDestructionStateIndex >= 0 && currentDestructionStateIndex < destructionStates.Length)
		{
			DestructionState destructionState = destructionStates[currentDestructionStateIndex];
			if (destructionState.meshesToActivate != null)
			{
				for (int i = 0; i < destructionState.meshesToActivate.Length; i++)
				{
					if ((Object)(object)destructionState.meshesToActivate[i] != (Object)null)
					{
						destructionState.meshesToActivate[i].SetActive(false);
					}
				}
			}
		}
		DestructionState destructionState2 = destructionStates[_stateIndex];
		if (destructionState2.meshesToActivate != null)
		{
			for (int j = 0; j < destructionState2.meshesToActivate.Length; j++)
			{
				if ((Object)(object)destructionState2.meshesToActivate[j] != (Object)null)
				{
					destructionState2.meshesToActivate[j].SetActive(true);
				}
			}
		}
		bool flag = vehicleExistenceTimer < 3f;
		bool flag2 = currentDestructionStateIndex >= 0 && _stateIndex < currentDestructionStateIndex;
		bool flag3 = currentDestructionStateIndex >= 0 && _stateIndex > currentDestructionStateIndex;
		if (!flag)
		{
			if (flag2)
			{
				if (Object.op_Implicit((Object)(object)vehicleFixParticles))
				{
					vehicleFixParticles.Play(true);
				}
				vehicleFixSound.Play(((Component)this).transform, soundEffectVolume);
			}
			else if (flag3)
			{
				if (Object.op_Implicit((Object)(object)stateChangeParticles))
				{
					stateChangeParticles.Play(true);
				}
				vehicleBreakSound.Play(((Component)this).transform, soundEffectVolume);
				GameDirector.instance.CameraShake.ShakeDistance(2.4f, 3f, 8f, ((Component)this).transform.position, 0.3f);
				GameDirector.instance.CameraImpact.ShakeDistance(4f, 3f, 8f, ((Component)this).transform.position, 0.3f);
			}
		}
		if (_stateIndex == destructionStates.Length - 1)
		{
			if (Object.op_Implicit((Object)(object)grabMesh))
			{
				((Component)grabMesh).gameObject.SetActive(false);
			}
			isFullyDestroyed = true;
			if (Object.op_Implicit((Object)(object)frontLight))
			{
				((Behaviour)frontLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)backLight))
			{
				((Behaviour)backLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)driftLight))
			{
				((Behaviour)driftLight).enabled = false;
			}
			Material val = EnsureFrontLightMaterial();
			if (Object.op_Implicit((Object)(object)val))
			{
				val.SetColor(emissionColorID, Color.black);
			}
			Material val2 = EnsureBackLightMaterial();
			if (Object.op_Implicit((Object)(object)val2))
			{
				val2.SetColor(emissionColorID, Color.black);
			}
			if (flag3 && !flag)
			{
				if (Object.op_Implicit((Object)(object)destroyParticles))
				{
					destroyParticles.Play(true);
				}
				if (Object.op_Implicit((Object)(object)explosionScript) && Object.op_Implicit((Object)(object)explosionPosTransform))
				{
					explosionScript.Spawn(explosionPosTransform.position, 1.5f, 100, 200, 45f);
				}
				if (Object.op_Implicit((Object)(object)valuableBox) && ((Component)valuableBox).gameObject.activeSelf)
				{
					valuableBoxExplosionPending = true;
					valuableBoxExplosionTimerMax = Random.Range(1f, 3f);
					valuableBoxExplosionTimer = valuableBoxExplosionTimerMax;
				}
			}
			else if (Object.op_Implicit((Object)(object)valuableBox))
			{
				((Component)valuableBox).gameObject.SetActive(false);
			}
		}
		else if (isFullyDestroyed)
		{
			if (Object.op_Implicit((Object)(object)grabMesh))
			{
				((Component)grabMesh).gameObject.SetActive(true);
			}
			isFullyDestroyed = false;
			valuableBoxExplosionPending = false;
			canTriggerExplosion = false;
			if (Object.op_Implicit((Object)(object)frontLight))
			{
				((Behaviour)frontLight).enabled = true;
			}
			if (Object.op_Implicit((Object)(object)backLight))
			{
				((Behaviour)backLight).enabled = true;
			}
			if (Object.op_Implicit((Object)(object)driftLight))
			{
				((Behaviour)driftLight).enabled = hasDrifting && (driftLightFadingIn || driftLightLerp > 0f);
			}
			Material val3 = EnsureFrontLightMaterial();
			if (Object.op_Implicit((Object)(object)val3))
			{
				val3.SetColor(emissionColorID, frontLightEmissionColor);
			}
			Material val4 = EnsureBackLightMaterial();
			if (Object.op_Implicit((Object)(object)val4))
			{
				val4.SetColor(emissionColorID, backLightEmissionColor);
			}
			if (Object.op_Implicit((Object)(object)valuableBox) && !((Component)valuableBox).gameObject.activeSelf)
			{
				((Component)valuableBox).gameObject.SetActive(true);
				if (Object.op_Implicit((Object)(object)valuableBoxComebackParticle))
				{
					valuableBoxComebackParticle.Play(true);
				}
			}
		}
		currentDestructionStateIndex = _stateIndex;
	}

	private void UpdateValuableBoxExplosion()
	{
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		float num = ((valuableBoxExplosionTimerMax > 0f) ? (1f - valuableBoxExplosionTimer / valuableBoxExplosionTimerMax) : 0f);
		float pitchMultiplier = Mathf.Lerp(1f, 2f, num);
		valuableBoxBreakBuildup.PlayLoop(valuableBoxExplosionPending, 2f, 2f, pitchMultiplier, soundEffectVolume);
		valuableBoxBreakBuildup.Source.dopplerLevel = (localPlayerIsMounted ? 0f : valuableBoxBreakBuildup.Doppler);
		if (Object.op_Implicit((Object)(object)valuableBoxBreakBuildupParticle))
		{
			if (valuableBoxExplosionPending && !valuableBoxBreakBuildupParticle.isPlaying)
			{
				valuableBoxBreakBuildupParticle.Play(true);
			}
			else if (!valuableBoxExplosionPending && valuableBoxBreakBuildupParticle.isPlaying)
			{
				valuableBoxBreakBuildupParticle.Stop(true);
			}
		}
		if (!valuableBoxExplosionPending)
		{
			return;
		}
		valuableBoxExplosionTimer -= Time.deltaTime;
		if (!(valuableBoxExplosionTimer <= 0f))
		{
			return;
		}
		valuableBoxExplosionPending = false;
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ValuableBoxExplodeRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				ValuableBoxExplodeRPC();
			}
		}
	}

	[PunRPC]
	private void ValuableBoxExplodeRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && Object.op_Implicit((Object)(object)valuableBox) && ((Component)valuableBox).gameObject.activeSelf)
		{
			Vector3 position = ((Component)valuableBox).transform.position;
			if (Object.op_Implicit((Object)(object)explosionScript))
			{
				explosionScript.Spawn(position, 1f, 50, 100);
			}
			((Component)valuableBox).gameObject.SetActive(false);
		}
	}

	public void OverrideDamageSmoke(float _time)
	{
		if (damageSmokeOverrideTimer <= 0f && Object.op_Implicit((Object)(object)damageSmoke))
		{
			damageSmoke.Play(true);
		}
		damageSmokeOverrideTimer = _time;
	}

	public void OverrideDamageFire(float _time)
	{
		if (damageFireOverrideTimer <= 0f && Object.op_Implicit((Object)(object)damageFire))
		{
			damageFire.Play(true);
		}
		damageFireOverrideTimer = _time;
	}

	private void DamageParticlesTick()
	{
		if (damageSmokeOverrideTimer > 0f)
		{
			damageSmokeOverrideTimer -= Time.deltaTime;
			if (damageSmokeOverrideTimer <= 0f && Object.op_Implicit((Object)(object)damageSmoke) && damageSmoke.isPlaying)
			{
				damageSmoke.Stop(true);
			}
		}
		if (damageFireOverrideTimer > 0f)
		{
			damageFireOverrideTimer -= Time.deltaTime;
			if (damageFireOverrideTimer <= 0f && Object.op_Implicit((Object)(object)damageFire) && damageFire.isPlaying)
			{
				damageFire.Stop(true);
			}
		}
		bool playing = damageSmokeOverrideTimer > 0f;
		bool flag = damageFireOverrideTimer > 0f;
		damageSmokeLoopSound.PlayLoop(playing, 1f, 3f, 1f, soundEffectVolume);
		if (Object.op_Implicit((Object)(object)damageSmokeLoopSound.Source))
		{
			damageSmokeLoopSound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : damageSmokeLoopSound.Doppler);
		}
		damageFireLoopSound.PlayLoop(flag, 1f, 3f, 1f, soundEffectVolume);
		if (Object.op_Implicit((Object)(object)damageFireLoopSound.Source))
		{
			damageFireLoopSound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : damageFireLoopSound.Doppler);
		}
		if (!Object.op_Implicit((Object)(object)fireLight))
		{
			return;
		}
		if (flag)
		{
			if (!((Behaviour)fireLight).enabled)
			{
				((Behaviour)fireLight).enabled = true;
			}
			fireLightLerp = Mathf.MoveTowards(fireLightLerp, 1f, Time.deltaTime / 0.3f);
		}
		else
		{
			fireLightLerp = Mathf.MoveTowards(fireLightLerp, 0f, Time.deltaTime / 0.5f);
			if (fireLightLerp <= 0f && ((Behaviour)fireLight).enabled)
			{
				((Behaviour)fireLight).enabled = false;
			}
		}
		if (((Behaviour)fireLight).enabled)
		{
			float num = Mathf.PerlinNoise(Time.time * 15f, 0f) * fireLightFlickerAmount;
			float num2 = Mathf.PerlinNoise(Time.time * 23f, 100f) * fireLightFlickerAmount * 0.5f;
			float num3 = fireLightIntensity + num + num2;
			fireLight.intensity = num3 * fireLightLerp;
		}
	}

	private bool SparkCheckSide(BoxCollider _collider)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Component)_collider).transform.TransformPoint(_collider.center);
		Vector3 val2 = Vector3.Scale(_collider.size * 0.5f, ((Component)_collider).transform.lossyScale);
		val2.x = Mathf.Abs(val2.x);
		val2.y = Mathf.Abs(val2.y);
		val2.z = Mathf.Abs(val2.z);
		Collider[] array = Physics.OverlapBox(val, val2, ((Component)_collider).transform.rotation, LayerMask.op_Implicit(sparkLayerMask), (QueryTriggerInteraction)1);
		foreach (Collider val3 in array)
		{
			if (((Component)val3).transform.IsChildOf(((Component)this).transform))
			{
				continue;
			}
			bool flag = false;
			for (int j = 0; j < seats.Length; j++)
			{
				if (Object.op_Implicit((Object)(object)seats[j].seatedPlayer) && ((Component)val3).transform.IsChildOf(((Component)seats[j].seatedPlayer).transform))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return true;
			}
		}
		return false;
	}

	private void OverrideSparkLeft()
	{
		if (sparkLeftCooldownTimer > 0f)
		{
			return;
		}
		if (!sparkLeftActive)
		{
			sparkLeftActive = true;
			sparkLeftStartBurstTimer = 0.3f;
			if (Object.op_Implicit((Object)(object)sparkParticlesLeft))
			{
				sparkParticlesLeft.Play(true);
			}
			if (Object.op_Implicit((Object)(object)sparkLightLeft))
			{
				((Behaviour)sparkLightLeft).enabled = true;
			}
			if (Object.op_Implicit((Object)(object)sparkTransformLeft))
			{
				sparkStartSound.Play(sparkTransformLeft, SparkSpeedVolume() * soundEffectVolume);
			}
		}
		sparkLeftOverrideTimer = 0.05f;
	}

	private void OverrideSparkRight()
	{
		if (sparkRightCooldownTimer > 0f)
		{
			return;
		}
		if (!sparkRightActive)
		{
			sparkRightActive = true;
			sparkRightStartBurstTimer = 0.3f;
			if (Object.op_Implicit((Object)(object)sparkParticlesRight))
			{
				sparkParticlesRight.Play(true);
			}
			if (Object.op_Implicit((Object)(object)sparkLightRight))
			{
				((Behaviour)sparkLightRight).enabled = true;
			}
			if (Object.op_Implicit((Object)(object)sparkTransformRight))
			{
				sparkStartSound.Play(sparkTransformRight, SparkSpeedVolume() * soundEffectVolume);
			}
		}
		sparkRightOverrideTimer = 0.05f;
	}

	private void OverrideSparkLeftTick()
	{
		if (sparkLeftCooldownTimer > 0f)
		{
			sparkLeftCooldownTimer -= Time.deltaTime;
		}
		if (sparkLeftOverrideTimer <= 0f && sparkLeftActive)
		{
			sparkLeftActive = false;
			sparkLeftCooldownTimer = Random.Range(0.1f, 0.2f);
			sparkLeftStopBurstTimer = 0.1f;
			SparkStopShake();
			if (Object.op_Implicit((Object)(object)sparkTransformLeft))
			{
				sparkEndSound.Play(sparkTransformLeft, SparkSpeedVolume() * soundEffectVolume);
			}
		}
		if (sparkLeftOverrideTimer > 0f)
		{
			sparkLeftOverrideTimer -= Time.deltaTime;
		}
		if (sparkLeftStopBurstTimer > 0f)
		{
			sparkLeftStopBurstTimer -= Time.deltaTime;
			if (sparkLeftStopBurstTimer <= 0f && Object.op_Implicit((Object)(object)sparkParticlesLeft) && sparkParticlesLeft.isPlaying)
			{
				sparkParticlesLeft.Stop(true);
			}
		}
		if (sparkLeftStartBurstTimer > 0f)
		{
			sparkLeftStartBurstTimer -= Time.deltaTime;
		}
	}

	private void OverrideSparkRightTick()
	{
		if (sparkRightCooldownTimer > 0f)
		{
			sparkRightCooldownTimer -= Time.deltaTime;
		}
		if (sparkRightOverrideTimer <= 0f && sparkRightActive)
		{
			sparkRightActive = false;
			sparkRightCooldownTimer = Random.Range(0.1f, 0.2f);
			sparkRightStopBurstTimer = 0.1f;
			SparkStopShake();
			if (Object.op_Implicit((Object)(object)sparkTransformRight))
			{
				sparkEndSound.Play(sparkTransformRight, SparkSpeedVolume() * soundEffectVolume);
			}
		}
		if (sparkRightOverrideTimer > 0f)
		{
			sparkRightOverrideTimer -= Time.deltaTime;
		}
		if (sparkRightStopBurstTimer > 0f)
		{
			sparkRightStopBurstTimer -= Time.deltaTime;
			if (sparkRightStopBurstTimer <= 0f && Object.op_Implicit((Object)(object)sparkParticlesRight) && sparkParticlesRight.isPlaying)
			{
				sparkParticlesRight.Stop(true);
			}
		}
		if (sparkRightStartBurstTimer > 0f)
		{
			sparkRightStartBurstTimer -= Time.deltaTime;
		}
	}

	private float SparkBurstMultiplier(float _startTimer, float _stopTimer)
	{
		if (_startTimer > 0f)
		{
			return Mathf.Lerp(1f, 4f, _startTimer / 0.3f);
		}
		if (_stopTimer > 0f)
		{
			return Mathf.Lerp(1f, 4f, _stopTimer / 0.1f);
		}
		return 1f;
	}

	private float SparkSpeedVolume()
	{
		float num = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
		float num2 = Mathf.InverseLerp(sparkMinSpeed, sparkMaxSpeed, num);
		return Mathf.Lerp(0.25f, 0.75f, num2);
	}

	private void UpdateSparks()
	{
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_065a: Unknown result type (might be due to invalid IL or missing references)
		float num = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity.magnitude : 0f);
		float num2 = maxForwardSpeed + hyperMaxSpeed;
		int num3;
		if (num >= sparkMinSpeed && num >= num2 * 0.7f && currentState == State.Active)
		{
			num3 = ((!isDrifting) ? 1 : 0);
			if (num3 != 0 && Object.op_Implicit((Object)(object)sparkColliderLeft) && SparkCheckSide(sparkColliderLeft))
			{
				OverrideSparkRight();
			}
		}
		else
		{
			num3 = 0;
		}
		if (num3 != 0 && Object.op_Implicit((Object)(object)sparkColliderRight) && SparkCheckSide(sparkColliderRight))
		{
			OverrideSparkLeft();
		}
		OverrideSparkLeftTick();
		OverrideSparkRightTick();
		float num4 = Mathf.InverseLerp(sparkMinSpeed, sparkMaxSpeed, num);
		float pitchMultiplier = Mathf.Lerp(sparkMinPitch, sparkMaxPitch, num4);
		float num5 = SparkSpeedVolume();
		float num6 = Mathf.Lerp(0.5f, 1f, num4);
		float num7 = SparkBurstMultiplier(sparkLeftStartBurstTimer, sparkLeftStopBurstTimer);
		float num8 = SparkBurstMultiplier(sparkRightStartBurstTimer, sparkRightStopBurstTimer);
		float num9 = num6 * num7;
		float num10 = num6 * num8;
		bool flag = sparkLeftActive || sparkLeftStopBurstTimer > 0f;
		bool flag2 = sparkRightActive || sparkRightStopBurstTimer > 0f;
		if (flag && sparkLeftAllParticles != null)
		{
			for (int i = 0; i < sparkLeftAllParticles.Length; i++)
			{
				EmissionModule emission = sparkLeftAllParticles[i].emission;
				emission.rateOverTime = new MinMaxCurve(sparkLeftDefaultEmissions[i] * num9);
				MainModule main = sparkLeftAllParticles[i].main;
				main.maxParticles = Mathf.Max(1, Mathf.CeilToInt((float)sparkLeftDefaultMaxParticles[i] * num9));
			}
			sparkLeftLastMultiplier = num9;
		}
		sparkLoopLeft.PlayLoop(sparkLeftActive, 2f, 10f, pitchMultiplier, num5 * soundEffectVolume);
		sparkLoopLeft.Source.dopplerLevel = (localPlayerIsMounted ? 0f : sparkLoopLeft.Doppler);
		if (flag2 && sparkRightAllParticles != null)
		{
			for (int j = 0; j < sparkRightAllParticles.Length; j++)
			{
				EmissionModule emission2 = sparkRightAllParticles[j].emission;
				emission2.rateOverTime = new MinMaxCurve(sparkRightDefaultEmissions[j] * num10);
				MainModule main2 = sparkRightAllParticles[j].main;
				main2.maxParticles = Mathf.Max(1, Mathf.CeilToInt((float)sparkRightDefaultMaxParticles[j] * num10));
			}
			sparkRightLastMultiplier = num10;
		}
		sparkLoopRight.PlayLoop(sparkRightActive, 2f, 10f, pitchMultiplier, num5 * soundEffectVolume);
		sparkLoopRight.Source.dopplerLevel = (localPlayerIsMounted ? 0f : sparkLoopRight.Doppler);
		if (Object.op_Implicit((Object)(object)sparkLightLeft))
		{
			if (flag)
			{
				sparkLightLeftLerp = Mathf.MoveTowards(sparkLightLeftLerp, 1f, Time.deltaTime / 0.2f);
			}
			else
			{
				sparkLightLeftLerp = Mathf.MoveTowards(sparkLightLeftLerp, 0f, Time.deltaTime / 0.1f);
				if (sparkLightLeftLerp <= 0f && ((Behaviour)sparkLightLeft).enabled)
				{
					((Behaviour)sparkLightLeft).enabled = false;
				}
			}
			if (((Behaviour)sparkLightLeft).enabled)
			{
				float num11 = Mathf.PerlinNoise(Time.time * 8f, 42f) * 0.2f - 0.1f;
				float num12 = Mathf.Lerp(0.5f, 2f, sparkLeftLastMultiplier);
				sparkLightLeft.intensity = (num12 + num11) * sparkLightLeftLerp;
			}
		}
		if (Object.op_Implicit((Object)(object)sparkLightRight))
		{
			if (flag2)
			{
				sparkLightRightLerp = Mathf.MoveTowards(sparkLightRightLerp, 1f, Time.deltaTime / 0.2f);
			}
			else
			{
				sparkLightRightLerp = Mathf.MoveTowards(sparkLightRightLerp, 0f, Time.deltaTime / 0.1f);
				if (sparkLightRightLerp <= 0f && ((Behaviour)sparkLightRight).enabled)
				{
					((Behaviour)sparkLightRight).enabled = false;
				}
			}
			if (((Behaviour)sparkLightRight).enabled)
			{
				float num13 = Mathf.PerlinNoise(Time.time * 8f, 137f) * 0.2f - 0.1f;
				float num14 = Mathf.Lerp(0.5f, 2f, sparkRightLastMultiplier);
				sparkLightRight.intensity = (num14 + num13) * sparkLightRightLerp;
			}
		}
		float num15 = ((sparkLeftActive || sparkRightActive) ? Mathf.Max(sparkLeftLastMultiplier, sparkRightLastMultiplier) : 0f);
		float num16 = ((num15 > sparkShakeIntensity) ? (Time.deltaTime / 0.6f) : (Time.deltaTime / 0.6f));
		sparkShakeIntensity = Mathf.MoveTowards(sparkShakeIntensity, num15, num16);
		if (sparkShakeIntensity > 0f)
		{
			sparkShakeUpdateTimer -= Time.deltaTime;
			if (sparkShakeUpdateTimer <= 0f)
			{
				sparkShakeUpdateTimer = 1f / 30f;
				float num17 = Time.time * 30f;
				float num18 = (Mathf.PerlinNoise(num17, 200f) - 0.5f) * 2f + (Mathf.PerlinNoise(num17 * 2.3f, 210f) - 0.5f);
				float num19 = (Mathf.PerlinNoise(220f, num17) - 0.5f) * 2f + (Mathf.PerlinNoise(230f, num17 * 1.7f) - 0.5f);
				sparkShakePositionTarget = num18 * 0.0275f * sparkShakeIntensity;
				sparkShakeRotationTarget = num19 * 1.32f * sparkShakeIntensity;
			}
			float num20 = 1f - Mathf.Exp(-60f * Time.deltaTime);
			sparkShakePositionOffset = Mathf.Lerp(sparkShakePositionOffset, sparkShakePositionTarget, num20);
			sparkShakeRotationOffset = Mathf.Lerp(sparkShakeRotationOffset, sparkShakeRotationTarget, num20);
			GameDirector.instance.CameraShake.ShakeDistance(0.39f * sparkShakeIntensity, 0.5f, 8f, ((Component)this).transform.position, 0.075f);
		}
		else
		{
			sparkShakePositionOffset = 0f;
			sparkShakeRotationOffset = 0f;
			sparkShakePositionTarget = 0f;
			sparkShakeRotationTarget = 0f;
			sparkShakeUpdateTimer = 0f;
		}
	}

	private void SparkStopShake()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		sparkImpactShakeIntensity = Mathf.Max(sparkImpactShakeIntensity, 0.195f);
		GameDirector.instance.CameraShake.ShakeDistance(0.975f, 3f, 8f, ((Component)this).transform.position, 0.15f);
		GameDirector.instance.CameraImpact.ShakeDistance(1.625f, 3f, 8f, ((Component)this).transform.position, 0.2f);
	}

	private void UpdateSparkImpactShake()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (sparkImpactShakeIntensity <= 0.001f)
		{
			sparkImpactShakePositionOffset = Vector3.zero;
			sparkImpactShakeRotationOffset = Vector3.zero;
			sparkImpactShakeIntensity = 0f;
		}
		else
		{
			sparkImpactShakePositionOffset = Random.insideUnitSphere * sparkImpactShakeIntensity * impactShakePositionStrength;
			sparkImpactShakeRotationOffset = Random.insideUnitSphere * sparkImpactShakeIntensity * impactShakeRotationStrength;
			sparkImpactShakeIntensity -= sparkImpactShakeIntensity * (Time.deltaTime / 0.08f);
		}
	}

	private void UpdateHurtColliderTimer()
	{
		if (AITumbleImmune)
		{
			hurtColliderTimerSmall = 0f;
			hurtColliderTimerMedium = 0f;
			hurtColliderTimerBig = 0f;
			if (Object.op_Implicit((Object)(object)hurtColliderSmall) && ((Component)hurtColliderSmall).gameObject.activeSelf)
			{
				((Component)hurtColliderSmall).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)hurtColliderMedium) && ((Component)hurtColliderMedium).gameObject.activeSelf)
			{
				((Component)hurtColliderMedium).gameObject.SetActive(false);
			}
			if (Object.op_Implicit((Object)(object)hurtColliderBig) && ((Component)hurtColliderBig).gameObject.activeSelf)
			{
				((Component)hurtColliderBig).gameObject.SetActive(false);
			}
			return;
		}
		if (hurtColliderTimerSmall <= 0f && Object.op_Implicit((Object)(object)hurtColliderSmall) && ((Component)hurtColliderSmall).gameObject.activeSelf)
		{
			((Component)hurtColliderSmall).gameObject.SetActive(false);
		}
		if (hurtColliderTimerSmall > 0f)
		{
			hurtColliderTimerSmall -= Time.deltaTime;
		}
		if (hurtColliderTimerMedium <= 0f && Object.op_Implicit((Object)(object)hurtColliderMedium) && ((Component)hurtColliderMedium).gameObject.activeSelf)
		{
			((Component)hurtColliderMedium).gameObject.SetActive(false);
		}
		if (hurtColliderTimerMedium > 0f)
		{
			hurtColliderTimerMedium -= Time.deltaTime;
		}
		if (hurtColliderTimerBig <= 0f && Object.op_Implicit((Object)(object)hurtColliderBig) && ((Component)hurtColliderBig).gameObject.activeSelf)
		{
			((Component)hurtColliderBig).gameObject.SetActive(false);
		}
		if (hurtColliderTimerBig > 0f)
		{
			hurtColliderTimerBig -= Time.deltaTime;
		}
	}

	private void SetupHurtColliderIgnoreList(HurtCollider _hurtCollider)
	{
		_hurtCollider.ignoreObjects.Clear();
		_hurtCollider.ignorePlayers.Clear();
		_hurtCollider.ignoreObjects.Add(physGrabObject);
		_hurtCollider.addRuckusOnVehicleImpactPlayerHit = true;
		PlayerAvatar playerAvatar = null;
		for (int i = 0; i < seats.Length; i++)
		{
			if ((Object)(object)seats[i].seatedPlayer != (Object)null)
			{
				if (!Object.op_Implicit((Object)(object)playerAvatar))
				{
					playerAvatar = seats[i].seatedPlayer;
				}
				_hurtCollider.ignoreObjects.Add(seats[i].seatedPlayer.tumble.physGrabObject);
				_hurtCollider.ignorePlayers.Add(seats[i].seatedPlayer);
			}
		}
		_hurtCollider.playerCausingHurtOverride = playerAvatar;
		if (!Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			return;
		}
		for (int j = 0; j < allActiveVehicles.Count; j++)
		{
			ItemVehicle itemVehicle = allActiveVehicles[j];
			if ((Object)(object)itemVehicle == (Object)(object)this)
			{
				continue;
			}
			if (Object.op_Implicit((Object)(object)itemVehicle.physGrabObject))
			{
				_hurtCollider.ignoreObjects.Add(itemVehicle.physGrabObject);
			}
			for (int k = 0; k < itemVehicle.seats.Length; k++)
			{
				PlayerAvatar seatedPlayer = itemVehicle.seats[k].seatedPlayer;
				if ((Object)(object)seatedPlayer != (Object)null)
				{
					_hurtCollider.ignoreObjects.Add(seatedPlayer.tumble.physGrabObject);
					_hurtCollider.ignorePlayers.Add(seatedPlayer);
				}
			}
		}
		List<PlayerAvatar> list = SemiFunc.PlayerGetList();
		for (int l = 0; l < list.Count; l++)
		{
			PlayerAvatar playerAvatar2 = list[l];
			if (playerAvatar2.tumble.isTumbling)
			{
				if (!_hurtCollider.ignorePlayers.Contains(playerAvatar2))
				{
					_hurtCollider.ignorePlayers.Add(playerAvatar2);
				}
				if (!_hurtCollider.ignoreObjects.Contains(playerAvatar2.tumble.physGrabObject))
				{
					_hurtCollider.ignoreObjects.Add(playerAvatar2.tumble.physGrabObject);
				}
			}
		}
	}

	private void UpdateEnemyImpactBoxCheck()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0415: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Unknown result type (might be due to invalid IL or missing references)
		//IL_051d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0546: Unknown result type (might be due to invalid IL or missing references)
		//IL_0574: Unknown result type (might be due to invalid IL or missing references)
		//IL_0594: Unknown result type (might be due to invalid IL or missing references)
		//IL_0599: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_036a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_061d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0622: Unknown result type (might be due to invalid IL or missing references)
		//IL_064b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0679: Unknown result type (might be due to invalid IL or missing references)
		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
		//IL_069e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0748: Unknown result type (might be due to invalid IL or missing references)
		//IL_074d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c9: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)enemyImpactBoxTransform) || !SemiFunc.IsMasterClientOrSingleplayer() || AITumbleImmune || (physGrabObject.grabbed && !hasAnySeatedPlayerCached))
		{
			return;
		}
		if (enemyImpactFreezeCooldown > 0f)
		{
			enemyImpactFreezeCooldown -= Time.deltaTime;
		}
		else
		{
			if (!SemiFunc.PerSecond(20f, (Object)(object)this) || !Object.op_Implicit((Object)(object)fakeVelocities))
			{
				return;
			}
			Vector3 velocity = fakeVelocities.velocity;
			velocity.y = 0f;
			if (velocity.magnitude < smallImpactThreshold || GetAllowedImpactType(velocity.magnitude) == ImpactType.None)
			{
				return;
			}
			Vector3 val = enemyImpactBoxTransform.localScale * 0.5f;
			val.x = Mathf.Abs(val.x);
			val.y = Mathf.Abs(val.y);
			val.z = Mathf.Abs(val.z);
			int num = LayerMask.op_Implicit(SemiFunc.LayerMaskGetPhysGrabObject()) + layerMaskDefaultEnemyPlayer;
			Collider[] array = Physics.OverlapBox(enemyImpactBoxTransform.position, val, enemyImpactBoxTransform.rotation, num, (QueryTriggerInteraction)2);
			Vector3 normalized = velocity.normalized;
			float time = Mathf.Lerp(0.05f, 0.14f, Mathf.InverseLerp(smallImpactThreshold, bigImpactThreshold, velocity.magnitude));
			for (int i = 0; i < array.Length; i++)
			{
				if (((Component)array[i]).transform.IsChildOf(((Component)this).transform) || AIRiderHasCollider(array[i]))
				{
					continue;
				}
				Vector3 val2 = ((Component)array[i]).transform.position - physGrabObject.centerPoint;
				Vector3 normalized2 = val2.normalized;
				if (Vector3.Dot(normalized, normalized2) < 0.3f)
				{
					continue;
				}
				CachedComponents cachedComponents = GetCachedComponents(array[i]);
				if (Object.op_Implicit((Object)(object)cachedComponents.vehicle) && (Object)(object)cachedComponents.vehicle != (Object)(object)this)
				{
					if ((hasAnySeatedPlayerCached && cachedComponents.vehicle.hasAnySeatedPlayerCached) || !Object.op_Implicit((Object)(object)cachedComponents.vehicle.fakeVelocities))
					{
						continue;
					}
					Vector3 velocity2 = cachedComponents.vehicle.fakeVelocities.velocity;
					velocity2.y = 0f;
					Vector3 val3 = velocity - velocity2;
					float magnitude = val3.magnitude;
					float num2 = Vector3.Dot(velocity.normalized, velocity2.normalized);
					float num3 = Mathf.Lerp(smallImpactThreshold, smallImpactThreshold * 3f, Mathf.Clamp01(num2));
					if (magnitude >= num3)
					{
						float time2 = Mathf.Lerp(0.05f, 0.14f, Mathf.InverseLerp(smallImpactThreshold, bigImpactThreshold, magnitude));
						physGrabObject.FreezeForces(time2, Vector3.zero, Vector3.zero);
						if (Object.op_Implicit((Object)(object)cachedComponents.vehicle.physGrabObject))
						{
							cachedComponents.vehicle.physGrabObject.FreezeForces(time2, Vector3.zero, Vector3.zero);
						}
						GameDirector.instance.CameraShake.ShakeDistance(0.4f, 3f, 8f, ((Component)this).transform.position, 0.1f);
						GameDirector.instance.CameraImpact.ShakeDistance(0.8f, 3f, 8f, ((Component)this).transform.position, 0.1f);
						TriggerEnemyImpact(magnitude, ((Component)array[i]).transform.position, val3.normalized);
						enemyImpactFreezeCooldown = 0.5f;
						break;
					}
					continue;
				}
				if (Object.op_Implicit((Object)(object)cachedComponents.enemy))
				{
					physGrabObject.FreezeForces(time, Vector3.zero, Vector3.zero);
					GameDirector.instance.CameraShake.ShakeDistance(0.4f, 3f, 8f, ((Component)this).transform.position, 0.1f);
					GameDirector.instance.CameraImpact.ShakeDistance(0.8f, 3f, 8f, ((Component)this).transform.position, 0.1f);
					TriggerEnemyImpact(velocity.magnitude, ((Component)array[i]).transform.position, normalized);
					enemyImpactFreezeCooldown = 0.5f;
					break;
				}
				if (Object.op_Implicit((Object)(object)cachedComponents.enemyRigidbody))
				{
					physGrabObject.FreezeForces(time, Vector3.zero, Vector3.zero);
					GameDirector.instance.CameraShake.ShakeDistance(0.4f, 3f, 8f, ((Component)this).transform.position, 0.1f);
					GameDirector.instance.CameraImpact.ShakeDistance(0.8f, 3f, 8f, ((Component)this).transform.position, 0.1f);
					TriggerEnemyImpact(velocity.magnitude, ((Component)array[i]).transform.position, normalized);
					enemyImpactFreezeCooldown = 0.5f;
					break;
				}
				if (Object.op_Implicit((Object)(object)cachedComponents.valuable))
				{
					ValuableVolume.Type volumeType = cachedComponents.valuable.volumeType;
					if (volumeType == ValuableVolume.Type.Big || volumeType == ValuableVolume.Type.Wide || volumeType == ValuableVolume.Type.Tall || volumeType == ValuableVolume.Type.VeryTall)
					{
						physGrabObject.FreezeForces(time, Vector3.zero, Vector3.zero);
						GameDirector.instance.CameraShake.ShakeDistance(0.4f, 3f, 8f, ((Component)this).transform.position, 0.1f);
						GameDirector.instance.CameraImpact.ShakeDistance(0.8f, 3f, 8f, ((Component)this).transform.position, 0.1f);
						TriggerEnemyImpact(velocity.magnitude, ((Component)array[i]).transform.position, normalized);
						enemyImpactFreezeCooldown = 0.5f;
						break;
					}
				}
				if (Object.op_Implicit((Object)(object)cachedComponents.playerTumble) && cachedComponents.playerTumble.isTumbling)
				{
					bool flag = false;
					for (int j = 0; j < seats.Length; j++)
					{
						if ((Object)(object)seats[j].seatedPlayer == (Object)(object)cachedComponents.playerTumble.playerAvatar)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						physGrabObject.FreezeForces(time, Vector3.zero, Vector3.zero);
						GameDirector.instance.CameraShake.ShakeDistance(0.4f, 3f, 8f, ((Component)this).transform.position, 0.1f);
						GameDirector.instance.CameraImpact.ShakeDistance(0.8f, 3f, 8f, ((Component)this).transform.position, 0.1f);
						TriggerEnemyImpact(velocity.magnitude, ((Component)array[i]).transform.position, normalized);
						enemyImpactFreezeCooldown = 0.5f;
						break;
					}
				}
				if (!((Component)array[i]).gameObject.CompareTag("Player"))
				{
					continue;
				}
				PlayerAvatar playerAvatar = cachedComponents.playerAvatar;
				if (!Object.op_Implicit((Object)(object)playerAvatar) && Object.op_Implicit((Object)(object)cachedComponents.playerController))
				{
					playerAvatar = cachedComponents.playerController.playerAvatarScript;
				}
				if (!Object.op_Implicit((Object)(object)playerAvatar))
				{
					continue;
				}
				bool flag2 = false;
				for (int k = 0; k < seats.Length; k++)
				{
					if ((Object)(object)seats[k].seatedPlayer == (Object)(object)playerAvatar)
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					physGrabObject.FreezeForces(time, Vector3.zero, Vector3.zero);
					GameDirector.instance.CameraShake.ShakeDistance(0.4f, 3f, 8f, ((Component)this).transform.position, 0.1f);
					GameDirector.instance.CameraImpact.ShakeDistance(0.8f, 3f, 8f, ((Component)this).transform.position, 0.1f);
					TriggerEnemyImpact(velocity.magnitude, ((Component)array[i]).transform.position, normalized);
					enemyImpactFreezeCooldown = 0.5f;
					break;
				}
			}
		}
	}

	private void TriggerEnemyImpact(float _speed, Vector3 _position, Vector3 _direction)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		ImpactType allowedImpactType = GetAllowedImpactType(_speed);
		if (allowedImpactType != ImpactType.None)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ImpactEffectRPC", (RpcTarget)0, new object[3]
				{
					(int)allowedImpactType,
					_position,
					_direction
				});
			}
			else
			{
				ImpactEffectRPC((int)allowedImpactType, _position, _direction);
			}
		}
	}

	private void UpdateBumperCarCollisions()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !hasAnySeatedPlayerCached || !Object.op_Implicit((Object)(object)fakeVelocities) || !SemiFunc.PerSecond(20f, (Object)(object)this))
		{
			return;
		}
		int num = (Object.op_Implicit((Object)(object)photonView) ? photonView.ViewID : ((Object)this).GetInstanceID());
		Vector3 velocity = fakeVelocities.velocity;
		velocity.y = 0f;
		Vector3 centerPoint = physGrabObject.centerPoint;
		for (int i = 0; i < allActiveVehicles.Count; i++)
		{
			ItemVehicle itemVehicle = allActiveVehicles[i];
			if ((Object)(object)itemVehicle == (Object)(object)this || !Object.op_Implicit((Object)(object)itemVehicle.fakeVelocities) || !Object.op_Implicit((Object)(object)itemVehicle.physGrabObject))
			{
				continue;
			}
			int num2 = (Object.op_Implicit((Object)(object)itemVehicle.photonView) ? itemVehicle.photonView.ViewID : ((Object)itemVehicle).GetInstanceID());
			if (num <= num2)
			{
				Vector3 centerPoint2 = itemVehicle.physGrabObject.centerPoint;
				Vector3 val = centerPoint - centerPoint2;
				if (!(val.sqrMagnitude > 400f) && (!bumperCarPairCooldowns.TryGetValue(num2, out var value) || !(Time.time < value)) && CheckBumperCarOverlap(itemVehicle, centerPoint, out var _contactPoint))
				{
					ProcessBumperCarImpact(itemVehicle, velocity, centerPoint, centerPoint2, _contactPoint);
				}
			}
		}
	}

	private bool CheckBumperCarOverlap(ItemVehicle _other, Vector3 _myPosition, out Vector3 _contactPoint)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		_contactPoint = Vector3.zero;
		if (!Object.op_Implicit((Object)(object)bumperCarBoxTransform))
		{
			return false;
		}
		Vector3 val = bumperCarBoxTransform.localScale * 0.5f;
		val.x = Mathf.Abs(val.x);
		val.y = Mathf.Abs(val.y);
		val.z = Mathf.Abs(val.z);
		Collider[] array = Physics.OverlapBox(bumperCarBoxTransform.position, val, bumperCarBoxTransform.rotation, layerMaskPhysGrabObject, (QueryTriggerInteraction)2);
		for (int i = 0; i < array.Length; i++)
		{
			if (((Component)array[i]).transform.IsChildOf(((Component)_other).transform))
			{
				_contactPoint = array[i].ClosestPoint(_myPosition);
				return true;
			}
		}
		return false;
	}

	private void ProcessBumperCarImpact(ItemVehicle _other, Vector3 _myVelocity, Vector3 _myPosition, Vector3 _otherPosition, Vector3 _contactPoint)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		Vector3 velocity = _other.fakeVelocities.velocity;
		velocity.y = 0f;
		Vector3 val = _myVelocity - velocity;
		float magnitude = val.magnitude;
		Vector3 val2 = _otherPosition - _myPosition;
		val2.y = 0f;
		if (val2.sqrMagnitude < 0.001f)
		{
			val2 = Vector3.forward;
		}
		val2.Normalize();
		float num = 0f;
		float num2 = 0f;
		if (_myVelocity.sqrMagnitude > 0.01f && velocity.sqrMagnitude > 0.01f)
		{
			num = Vector3.Dot(_myVelocity.normalized, velocity.normalized);
			if (num > 0.3f)
			{
				float num3 = Mathf.Abs(Vector3.Dot(_myVelocity.normalized, val2));
				float num4 = Mathf.Abs(Vector3.Dot(velocity.normalized, val2));
				num2 = Mathf.Clamp01(1f - (num3 + num4) * 0.5f) * Mathf.Clamp01(num);
			}
		}
		float num5 = Mathf.Lerp(1f, bumperCarSideTackleMultiplier, num2);
		float num6 = magnitude * num5;
		float num7 = ((_myVelocity.sqrMagnitude > 0.01f && velocity.sqrMagnitude > 0.01f) ? Vector3.Dot(_myVelocity.normalized, velocity.normalized) : (-1f));
		float num8 = Mathf.Lerp(smallImpactThreshold, smallImpactThreshold * 2f, Mathf.Clamp01(num7));
		if (!(num6 < num8))
		{
			ImpactType bumperCarImpactType = GetBumperCarImpactType(num6);
			float num9;
			switch (bumperCarImpactType)
			{
			case ImpactType.None:
				return;
			case ImpactType.Small:
				num9 = 8f;
				break;
			case ImpactType.Medium:
				num9 = 16f;
				break;
			case ImpactType.Big:
				num9 = 28f;
				break;
			default:
				num9 = 0f;
				break;
			}
			float num10 = num9;
			float magnitude2 = _myVelocity.magnitude;
			float magnitude3 = velocity.magnitude;
			float num11 = magnitude2 + magnitude3;
			float num12 = ((num11 > 0.01f) ? (magnitude3 / num11 + 0.5f) : 1f);
			float num13 = ((num11 > 0.01f) ? (magnitude2 / num11 + 0.5f) : 1f);
			Vector3 val3 = -val2 * num10 * num12;
			Vector3 val4 = val2 * num10 * num13;
			float time = Mathf.Lerp(0.05f, 0.14f, Mathf.InverseLerp(smallImpactThreshold, bigImpactThreshold, num6));
			physGrabObject.FreezeForces(time, val3 * forceMultiplier * bumperCarForceMultiplier, Vector3.zero);
			if (Object.op_Implicit((Object)(object)_other.physGrabObject))
			{
				_other.physGrabObject.FreezeForces(time, val4 * _other.forceMultiplier * _other.bumperCarForceMultiplier, Vector3.zero);
			}
			Vector3 normalized = val.normalized;
			TriggerBumperCarImpact(bumperCarImpactType, _contactPoint, normalized);
			_other.TriggerBumperCarImpact(bumperCarImpactType, _contactPoint, -normalized);
			int key = (Object.op_Implicit((Object)(object)_other.photonView) ? _other.photonView.ViewID : ((Object)_other).GetInstanceID());
			int key2 = (Object.op_Implicit((Object)(object)photonView) ? photonView.ViewID : ((Object)this).GetInstanceID());
			bumperCarPairCooldowns[key] = Time.time + 0.5f;
			_other.bumperCarPairCooldowns[key2] = Time.time + 0.5f;
			enemyImpactFreezeCooldown = 0.5f;
			_other.enemyImpactFreezeCooldown = 0.5f;
		}
	}

	private void TriggerBumperCarImpact(ImpactType _impactType, Vector3 _position, Vector3 _direction)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (_impactType != ImpactType.None)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("ImpactEffectRPC", (RpcTarget)0, new object[3]
				{
					(int)_impactType,
					_position,
					_direction
				});
			}
			else
			{
				ImpactEffectRPC((int)_impactType, _position, _direction);
			}
		}
	}

	private ImpactType GetBumperCarImpactType(float _speed)
	{
		if (_speed >= bigImpactThreshold)
		{
			return ImpactType.Big;
		}
		if (_speed >= mediumImpactThreshold)
		{
			return ImpactType.Medium;
		}
		if (_speed >= smallImpactThreshold)
		{
			return ImpactType.Small;
		}
		return ImpactType.None;
	}

	private void PerformGroundCheck()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_0476: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Unknown result type (might be due to invalid IL or missing references)
		//IL_0497: Unknown result type (might be due to invalid IL or missing references)
		//IL_049c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		_ = isFrontColliding;
		_ = isBackColliding;
		int num = layerMaskGroundCheck;
		bool flag = false;
		isFrontColliding = false;
		if (Object.op_Implicit((Object)(object)frontGroundedTransform))
		{
			Vector3 val = frontGroundedTransform.localScale * 0.5f;
			val.x = Mathf.Abs(val.x);
			val.y = Mathf.Abs(val.y);
			val.z = Mathf.Abs(val.z);
			Collider[] array = Physics.OverlapBox(frontGroundedTransform.position, val, frontGroundedTransform.rotation, num, (QueryTriggerInteraction)1);
			RaycastHit val2 = default;
			for (int i = 0; i < array.Length; i++)
			{
				if (!((Component)array[i]).transform.IsChildOf(((Component)this).transform))
				{
					isFrontColliding = true;
					float num2 = frontGroundedTransform.localScale.y + 0.5f;
					if (Physics.Raycast(frontGroundedTransform.position, VehicleDown, ref val2, num2, num, (QueryTriggerInteraction)1) && Vector3.Angle(val2.normal, VehicleUp) <= maxGroundedAngle)
					{
						flag = true;
					}
					break;
				}
			}
		}
		bool flag2 = false;
		isBackColliding = false;
		if (Object.op_Implicit((Object)(object)backGroundedTransform))
		{
			Vector3 val3 = backGroundedTransform.localScale * 0.5f;
			val3.x = Mathf.Abs(val3.x);
			val3.y = Mathf.Abs(val3.y);
			val3.z = Mathf.Abs(val3.z);
			Collider[] array2 = Physics.OverlapBox(backGroundedTransform.position, val3, backGroundedTransform.rotation, num, (QueryTriggerInteraction)1);
			RaycastHit val4 = default;
			for (int j = 0; j < array2.Length; j++)
			{
				if (!((Component)array2[j]).transform.IsChildOf(((Component)this).transform))
				{
					isBackColliding = true;
					float num3 = backGroundedTransform.localScale.y + 0.5f;
					if (Physics.Raycast(backGroundedTransform.position, VehicleDown, ref val4, num3, num, (QueryTriggerInteraction)1) && Vector3.Angle(val4.normal, VehicleUp) <= maxGroundedAngle)
					{
						flag2 = true;
					}
					break;
				}
			}
		}
		float num4;
		if (groundSlopeValid)
		{
			num4 = Mathf.Abs(groundSlopePitchAngle);
		}
		else
		{
			num4 = ((lastValidSlopeTimer > 0f) ? lastValidSlopeAngle : 0f);
		}
		float num5 = ((num4 > 10f) ? Mathf.Lerp(0.1f, 0.25f, Mathf.Clamp01((num4 - 10f) / 30f)) : 0.08f);
		if (flag)
		{
			isFrontGrounded = true;
			frontUngroundedTimer = num5;
		}
		else
		{
			isFrontGrounded = frontUngroundedTimer > 0f;
		}
		if (flag2)
		{
			isBackGrounded = true;
			backUngroundedTimer = num5;
		}
		else
		{
			isBackGrounded = backUngroundedTimer > 0f;
		}
		if (frontUngroundedTimer > 0f)
		{
			frontUngroundedTimer -= Time.fixedDeltaTime;
		}
		if (backUngroundedTimer > 0f)
		{
			backUngroundedTimer -= Time.fixedDeltaTime;
		}
		groundSlopeValid = false;
		if (!Object.op_Implicit((Object)(object)wheelFront) || !Object.op_Implicit((Object)(object)wheelBack))
		{
			return;
		}
		float num6 = ((isVehicleAirborne && lastValidSlopeTimer > 0f) ? 4f : 2f);
		RaycastHit val5 = default;
		bool flag3 = Physics.Raycast(wheelFront.position, Vector3.down, ref val5, num6, num, (QueryTriggerInteraction)1);
		RaycastHit val6 = default;
		bool flag4 = Physics.Raycast(wheelBack.position, Vector3.down, ref val6, num6, num, (QueryTriggerInteraction)1);
		if (!(flag3 & flag4))
		{
			return;
		}
		Vector3 val7 = val5.point - val6.point;
		Vector3 val8 = val7;
		val8.y = 0f;
		float magnitude = val8.magnitude;
		if (!(magnitude > 0.01f))
		{
			return;
		}
		groundSlopePitchAngle = Mathf.Atan2(val7.y, magnitude) * 57.29578f;
		groundSlopeValid = true;
		bool flag5 = previousGroundSlopePitchAngle < -10f;
		bool flag6 = groundSlopePitchAngle > 5f;
		if ((flag5 & flag6) && groundSlopePitchAngle < 45f)
		{
			Vector3 linearVelocity = rb.linearVelocity;
			float magnitude2 = linearVelocity.magnitude;
			if (magnitude2 > 2f)
			{
				Vector3 normalized = val7.normalized;
				if (Vector3.Dot(rb.linearVelocity, -val5.normal) > magnitude2 * 0.3f)
				{
					Vector3 val9 = Vector3.ProjectOnPlane(rb.linearVelocity, val5.normal);
					if (val9.magnitude > 0.1f)
					{
						rb.linearVelocity = val9.normalized * magnitude2 * 0.9f;
					}
					else
					{
						rb.linearVelocity = normalized * magnitude2 * 0.8f;
					}
				}
			}
		}
		previousGroundSlopePitchAngle = groundSlopePitchAngle;
	}

	private void UpdateWallAheadDetection()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		wallAheadScale = 1f;
		if (!(currentSpeed < 2f))
		{
			float num = Mathf.Max(0f, Mathf.Asin(Mathf.Clamp(((Component)this).transform.forward.y, -1f, 1f)) * 57.29578f);
			float num2 = (groundSlopeValid ? Mathf.Max(0f, groundSlopePitchAngle) : 0f);
			float num3 = Mathf.Max(0f, num - num2 - 8f);
			if (num3 > 0f)
			{
				float num4 = Mathf.Clamp01((currentSpeed - 3f) / 7f);
				wallAheadScale = 1f - Mathf.Clamp01(num3 / 20f) * num4;
			}
			float num5 = Mathf.Abs(Vector3.SignedAngle(((Component)this).transform.up, Vector3.up, ((Component)this).transform.forward));
			float num6 = Mathf.Max(0f, num5 - 15f);
			if (num6 > 0f && isFrontColliding)
			{
				float num7 = Mathf.Clamp01((currentSpeed - 3f) / 7f);
				float num8 = 1f - Mathf.Clamp01(num6 / 30f) * num7;
				wallAheadScale = Mathf.Min(wallAheadScale, num8);
			}
		}
	}

	private void DrivingRumbleReset()
	{
		rumbleEnginePhase = 0f;
		rumbleTravel = 0f;
		rumbleGroundedTimer = 0f;
		rumbleLowPower = 0f;
		rumbleHighPower = 0f;
	}

	private void DrivingRumbleUpdate()
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Active || !Object.op_Implicit((Object)(object)LocalRiderGet()) || !Object.op_Implicit((Object)(object)InputManager.instance) || !InputManager.instance.VehicleGamepadInputAllowed() || Time.timeScale <= 0f)
		{
			DrivingRumbleReset();
			return;
		}
		Vector3 val = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity : rb.linearVelocity);
		Vector3 val2 = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.angularVelocity : rb.angularVelocity);
		Vector3 val3 = Vector3.ProjectOnPlane(val, ((Component)this).transform.up);
		float magnitude = val3.magnitude;
		float num = Mathf.Clamp01(magnitude / Mathf.Max(1f, maxForwardSpeed + hyperMaxSpeed));
		float num2 = Mathf.InverseLerp(0.2f, 1.5f, magnitude);
		float num3 = ControlThrottle;
		if (GamepadControlActive)
		{
			num3 = gamepad.throttle;
		}
		float num4 = Mathf.Clamp01(Mathf.Abs(num3));
		if (RumbleWheelGrounded(frontGroundedTransform) || RumbleWheelGrounded(backGroundedTransform))
		{
			rumbleGroundedTimer = 0.06f;
		}
		float num5 = ((rumbleGroundedTimer > 0f) ? num2 : 0f);
		if (rumbleGroundedTimer > 0f)
		{
			rumbleGroundedTimer = Mathf.Max(0f, rumbleGroundedTimer - Time.deltaTime);
		}
		rumbleEnginePhase = Mathf.Repeat(rumbleEnginePhase + Mathf.Lerp(8f, 18f, num) * Time.deltaTime * MathF.PI * 2f, MathF.PI * 2f);
		rumbleTravel = Mathf.Repeat(rumbleTravel + magnitude * Time.deltaTime, 1000f);
		float num6 = 0.85f + Mathf.Sin(rumbleEnginePhase) * 0.15f;
		float num7 = (num2 * Mathf.Lerp(0.3f, 1f, num) * 0.12f + num4 * 0.045f) * num6;
		float num8 = num5 * num * 0.035f * Mathf.Lerp(0.4f, 1f, Mathf.PerlinNoise(rumbleTravel * 1.5f, 0f));
		float num9 = Mathf.Abs(Vector3.Dot(val3, ((Component)this).transform.right));
		float num10 = Mathf.Abs(Vector3.Dot(val2, ((Component)this).transform.up)) * magnitude;
		float num11 = Mathf.Max(Mathf.InverseLerp(2f, 25f, num10) * 0.35f, Mathf.InverseLerp(0.75f, 6f, num9));
		if (isDrifting)
		{
			num11 = Mathf.Max(num11, Mathf.Lerp(0.35f, 1f, (float)driftBoostLevel / 3f));
		}
		float num12 = num5 * num11 * 0.18f * (0.75f + 0.25f * Mathf.Sin(rumbleEnginePhase * 0.5f));
		float num13 = 0f;
		if (Vector3.Dot(val3, ((Component)this).transform.forward) * num3 < -0.5f)
		{
			num13 = num5 * num4 * 0.14f;
		}
		Vector3 val4 = Vector3.ProjectOnPlane(val2, ((Component)this).transform.up);
		float magnitude2 = val4.magnitude;
		float num14 = num5 * Mathf.InverseLerp(0.5f, 4f, magnitude2) * 0.07f;
		float num15 = Mathf.Min(0.24f, num7 + num13 * 0.6f + num14);
		float num16 = Mathf.Min(0.35f, num7 * 0.3f + num8 + num12 + num13 + num14);
		rumbleLowPower = Mathf.MoveTowards(rumbleLowPower, num15, 1.5f * Time.deltaTime);
		rumbleHighPower = Mathf.MoveTowards(rumbleHighPower, num16, 3f * Time.deltaTime);
		InputManager.instance.GamepadRumbleContinuous(rumbleLowPower, rumbleHighPower);
	}

	private bool RumbleWheelGrounded(Transform _wheel)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)_wheel))
		{
			return false;
		}
		float num = Mathf.Abs(_wheel.localScale.y) + 0.5f;
		int num2 = Physics.RaycastNonAlloc(_wheel.position, -((Component)this).transform.up, rumbleGroundHits, num, layerMaskGroundCheck, (QueryTriggerInteraction)1);
		for (int i = 0; i < num2; i++)
		{
			RaycastHit val = rumbleGroundHits[i];
			if (!val.transform.IsChildOf(((Component)this).transform) && Vector3.Angle(val.normal, ((Component)this).transform.up) <= maxGroundedAngle)
			{
				return true;
			}
		}
		return false;
	}

	private PlayerAvatar LocalRiderGet()
	{
		for (int i = 0; i < seats.Length; i++)
		{
			VehicleSeat vehicleSeat = seats[i];
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (Object.op_Implicit((Object)(object)seatedPlayer) && seatedPlayer.isLocal && !seatedPlayer.isDisabled && !seatedPlayer.deadSet && (vehicleSeat.reachedFirstMount || !Object.op_Implicit((Object)(object)vehicleSeat.firstMountTransform)) && !(vehicleSeat.mountFinishTimer > 0f))
			{
				return seatedPlayer;
			}
		}
		return null;
	}

	private void RiderCameraShake(float _shake, float _impact, float _time)
	{
		if (Object.op_Implicit((Object)(object)LocalRiderGet()))
		{
			GameDirector.instance.CameraShake.Shake(_shake, _time);
			GameDirector.instance.CameraImpact.Shake(_impact, _time);
		}
	}

	[PunRPC]
	private void TakeoffCameraShakeRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			RiderCameraShake(1f, 2f, 0.08f);
		}
	}

	[PunRPC]
	private void LandingCameraShakeRPC(float _speed, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && float.IsFinite(_speed))
		{
			float num = Mathf.InverseLerp(4f, 25f, _speed);
			float strength = Mathf.Lerp(0.3f, 2.5f, num);
			float strength2 = Mathf.Lerp(0.6f, 5f, num);
			wobbleImpactSound.Play(meshTransform, soundEffectVolume);
			GameDirector.instance.CameraShake.ShakeDistance(strength, 3f, 8f, ((Component)this).transform.position, 0.12f);
			GameDirector.instance.CameraImpact.ShakeDistance(strength2, 3f, 8f, ((Component)this).transform.position, 0.08f);
		}
	}

	private void TriggerFallDamageLanding()
	{
		TumbleVehicle(fallDamageTumbleDuration);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("FallDamageLandingRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			FallDamageLandingEffects();
		}
	}

	[PunRPC]
	private void FallDamageLandingRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			FallDamageLandingEffects();
		}
	}

	private void FallDamageLandingEffects()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Component)this).transform.position;
		if (Object.op_Implicit((Object)(object)fallDamageParticles))
		{
			fallDamageParticles.Play(true);
		}
		fallDamageSound.Play(position, soundEffectVolume);
		entityHitSoundBigGlobal.Play(position, soundEffectVolume);
		impactSoundBig.Play(position, soundEffectVolume);
		GameDirector.instance.CameraShake.ShakeDistance(3.2f, 3f, 12f, position, 0.3f);
		GameDirector.instance.CameraImpact.ShakeDistance(6.4f, 3f, 12f, position, 0.3f);
		if (Object.op_Implicit((Object)(object)fallDamageHurtCollider))
		{
			SetupHurtColliderIgnoreList(fallDamageHurtCollider);
			((Component)fallDamageHurtCollider).gameObject.SetActive(true);
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			ApplyEnemyHitDurabilityLoss(mediumImpactDurabilityLoss);
		}
	}

	private void NosediveLandingEffects(bool _heavy)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = meshTransform.position;
		float num = (_heavy ? 3f : 1.5f);
		wobbleDirection = ((Component)this).transform.forward;
		wobblePositionSpringVelocity += num * wobblePositionStrength * 0.5f * 60f;
		wobblePositionSpringVelocity = Mathf.Clamp(wobblePositionSpringVelocity, (0f - wobblePositionMax) * 30f, wobblePositionMax * 30f);
		wobbleRotationSpringVelocity += num * wobbleRotationStrength * 60f;
		wobbleRotationSpringVelocity = Mathf.Clamp(wobbleRotationSpringVelocity, (0f - wobbleRotationMax) * 30f, wobbleRotationMax * 30f);
		wobbleCooldownTimer = Mathf.Max(wobbleCooldownTimer, 0.5f);
		if (_heavy)
		{
			sparkEndSound.Play(position, soundEffectVolume);
			nosediveSoundHeavy.Play(position, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)nosediveParticlesHeavy))
			{
				nosediveParticlesHeavy.Play(true);
			}
			GameDirector.instance.CameraShake.ShakeDistance(3.2f, 3f, 12f, position, 0.3f);
			GameDirector.instance.CameraImpact.ShakeDistance(6.4f, 3f, 12f, position, 0.3f);
			TriggerImpactShake(ImpactType.Big);
		}
		else
		{
			sparkEndSound.Play(position, 0.3f * soundEffectVolume);
			nosediveSound.Play(position, soundEffectVolume);
			if (Object.op_Implicit((Object)(object)nosediveParticles))
			{
				nosediveParticles.Play(true);
			}
			GameDirector.instance.CameraShake.ShakeDistance(1.2f, 3f, 8f, position, 0.2f);
			GameDirector.instance.CameraImpact.ShakeDistance(2.4f, 3f, 8f, position, 0.2f);
			TriggerImpactShake(ImpactType.Medium);
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			VehicleAddTorque(-((Component)this).transform.right * 15f, (ForceMode)1);
		}
	}

	[PunRPC]
	private void NosediveLandingEffectsRPC(bool _heavy, PhotonMessageInfo _info)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.OwnerOnlyRPC(_info, photonView))
		{
			NosediveLandingEffects(_heavy);
		}
	}

	private void DeparentMesh()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)meshTransform) && !meshDeparented)
		{
			meshPreviousPosition = meshOriginalParent.position;
			meshPreviousRotation = meshOriginalParent.rotation;
			meshTransform.SetParent((Transform)null, true);
			meshDeparented = true;
		}
	}

	private void ReparentMesh()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)meshTransform) && meshDeparented)
		{
			meshTransform.SetParent(meshOriginalParent, true);
			meshTransform.localPosition = meshOriginalLocalPosition;
			meshTransform.localRotation = meshOriginalLocalRotation;
			meshDeparented = false;
		}
	}

	private void CalculateMeshVelocity()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)meshOriginalParent))
		{
			return;
		}
		Vector3 position = meshOriginalParent.position;
		Quaternion rotation = meshOriginalParent.rotation;
		if (Time.deltaTime > 0f)
		{
			meshCalculatedVelocity = (position - meshPreviousPosition) / Time.deltaTime;
			Quaternion val = rotation * Quaternion.Inverse(meshPreviousRotation);
			float num = default;
			Vector3 val2 = default;
			val.ToAngleAxis(ref num, ref val2);
			if (num > 180f)
			{
				num -= 360f;
			}
			meshCalculatedAngularVelocity = val2 * (num * (MathF.PI / 180f) / Time.deltaTime);
		}
		meshPreviousPosition = position;
		meshPreviousRotation = rotation;
	}

	private void UpdateMeshWobble()
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0444: Unknown result type (might be due to invalid IL or missing references)
		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0485: Unknown result type (might be due to invalid IL or missing references)
		//IL_0452: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		int num = Mathf.CeilToInt(Time.deltaTime / 0.02f);
		if (num < 1)
		{
			num = 1;
		}
		float num2 = Time.deltaTime / (float)num;
		if (wobbleCooldownTimer > 0f)
		{
			wobbleCooldownTimer -= Time.deltaTime;
		}
		Vector3 val = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity : Vector3.zero);
		bool flag = val.y > -0.5f && wobblePreviousFakeVelocity.y < 0f - wobbleMinVelocity;
		Vector3 val2 = wobblePreviousFakeVelocity;
		val2.y = 0f;
		Vector3 val3 = val;
		val3.y = 0f;
		float num3 = val2.magnitude - val3.magnitude;
		bool flag2 = num3 >= wobbleMinDeceleration && val2.magnitude >= wobbleMinVelocity;
		if (SemiFunc.IsMasterClientOrSingleplayer() && wobbleCooldownTimer <= 0f && (flag2 | flag))
		{
			float num4;
			if (flag2)
			{
				wobbleDirection = val2.normalized;
				num4 = Mathf.InverseLerp(wobbleMinDeceleration, wobbleMaxDeceleration, num3);
			}
			else
			{
				wobbleDirection = -((Component)this).transform.up;
				float num5 = Mathf.Abs(wobblePreviousFakeVelocity.y) - Mathf.Abs(val.y);
				num4 = Mathf.InverseLerp(wobbleMinDeceleration, wobbleMaxDeceleration, num5);
			}
			if (num4 < wobbleForceDeadzone)
			{
				num4 = 0f;
			}
			if (num4 > 0f)
			{
				ApplyWobbleTrigger(num4, wobbleDirection);
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("WobbleTriggerRPC", (RpcTarget)1, new object[2] { num4, wobbleDirection });
				}
			}
		}
		wobblePreviousFakeVelocity = val;
		for (int i = 0; i < num; i++)
		{
			float num6 = 0f - wobblePositionOffset;
			wobblePositionSpringVelocity += (num6 * wobbleSpringStiffness - wobblePositionSpringVelocity * wobbleSpringDamping) * num2;
			wobblePositionOffset += wobblePositionSpringVelocity * num2;
			wobblePositionOffset = Mathf.Clamp(wobblePositionOffset, 0f - wobblePositionMax, wobblePositionMax);
			if (wobblePositionOffset >= wobblePositionMax && wobblePositionSpringVelocity > 0f)
			{
				wobblePositionSpringVelocity = 0f;
			}
			if (wobblePositionOffset <= 0f - wobblePositionMax && wobblePositionSpringVelocity < 0f)
			{
				wobblePositionSpringVelocity = 0f;
			}
			float num7 = 0f - wobbleRotationOffset;
			wobbleRotationSpringVelocity += (num7 * wobbleSpringStiffness - wobbleRotationSpringVelocity * wobbleSpringDamping) * num2;
			wobbleRotationOffset += wobbleRotationSpringVelocity * num2;
			wobbleRotationOffset = Mathf.Clamp(wobbleRotationOffset, 0f - wobbleRotationMax, wobbleRotationMax);
			if (wobbleRotationOffset >= wobbleRotationMax && wobbleRotationSpringVelocity > 0f)
			{
				wobbleRotationSpringVelocity = 0f;
			}
			if (wobbleRotationOffset <= 0f - wobbleRotationMax && wobbleRotationSpringVelocity < 0f)
			{
				wobbleRotationSpringVelocity = 0f;
			}
		}
		float num8 = wobblePositionMax * 0.5f;
		float num9 = wobbleRotationMax * 0.5f;
		wobblePositionOffset = Mathf.Clamp(wobblePositionOffset, previousWobblePositionOffset - num8, previousWobblePositionOffset + num8);
		wobbleRotationOffset = Mathf.Clamp(wobbleRotationOffset, previousWobbleRotationOffset - num9, previousWobbleRotationOffset + num9);
		previousWobblePositionOffset = wobblePositionOffset;
		previousWobbleRotationOffset = wobbleRotationOffset;
		if (Mathf.Abs(wobblePositionOffset) < 0.001f && Mathf.Abs(wobblePositionSpringVelocity) < 0.1f)
		{
			wobblePositionOffset = 0f;
			wobblePositionSpringVelocity = 0f;
		}
		if (Mathf.Abs(wobbleRotationOffset) < 0.01f && Mathf.Abs(wobbleRotationSpringVelocity) < 0.1f)
		{
			wobbleRotationOffset = 0f;
			wobbleRotationSpringVelocity = 0f;
		}
		if (wobbleDirectionSmoothed == Vector3.zero)
		{
			wobbleDirectionSmoothed = wobbleDirection;
		}
		else
		{
			wobbleDirectionSmoothed = Vector3.Lerp(wobbleDirectionSmoothed, wobbleDirection, 1f - Mathf.Exp(-10f * Time.deltaTime));
		}
	}

	private void ApplyWobbleTrigger(float _force, Vector3 _direction)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		wobbleDirection = _direction;
		wobblePositionSpringVelocity += _force * wobblePositionStrength * 0.5f * 60f;
		wobblePositionSpringVelocity = Mathf.Clamp(wobblePositionSpringVelocity, (0f - wobblePositionMax) * 30f, wobblePositionMax * 30f);
		wobbleRotationSpringVelocity += _force * wobbleRotationStrength * 60f;
		wobbleRotationSpringVelocity = Mathf.Clamp(wobbleRotationSpringVelocity, (0f - wobbleRotationMax) * 30f, wobbleRotationMax * 30f);
		meshRotationXVelocity = 0f;
		wobbleCooldownTimer = wobbleCooldown;
		wobbleImpactSound.Play(meshTransform, soundEffectVolume * _force);
	}

	[PunRPC]
	private void WobbleTriggerRPC(float _force, Vector3 _direction, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			ApplyWobbleTrigger(_force, _direction);
		}
	}

	private void UpdateDriftRumble()
	{
		float num = (isDrifting ? 1f : 0f);
		driftRumbleIntensity = Mathf.MoveTowards(driftRumbleIntensity, num, Time.deltaTime / 0.15f);
		if (driftRumbleIntensity <= 0f)
		{
			driftRumblePositionOffset = 0f;
			driftRumbleRotationOffset = 0f;
			return;
		}
		float num2 = Mathf.Lerp(1f, driftRumbleBoostMultiplier, (float)driftBoostLevel / 3f);
		float num3 = Time.time * driftRumbleSpeed;
		float num4 = (Mathf.PerlinNoise(num3, 0f) - 0.5f) * 2f + (Mathf.PerlinNoise(num3 * 2.7f, 5f) - 0.5f);
		float num5 = (Mathf.PerlinNoise(0f, num3) - 0.5f) * 2f + (Mathf.PerlinNoise(10f, num3 * 2.3f) - 0.5f);
		driftRumblePositionOffset = num4 * driftRumblePositionStrength * driftRumbleIntensity * num2;
		driftRumbleRotationOffset = num5 * driftRumbleRotationStrength * driftRumbleIntensity * num2;
	}

	private void UpdateImpactShake()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (impactShakeIntensity <= 0.001f)
		{
			impactShakePositionOffset = Vector3.zero;
			impactShakeRotationOffset = Vector3.zero;
			impactShakeIntensity = 0f;
		}
		else
		{
			impactShakePositionOffset = Random.insideUnitSphere * impactShakeIntensity * impactShakePositionStrength;
			impactShakeRotationOffset = Random.insideUnitSphere * impactShakeIntensity * impactShakeRotationStrength;
			impactShakeIntensity *= Mathf.Exp((0f - impactShakeDecayRate) * Time.deltaTime);
		}
	}

	private void TriggerImpactShake(ImpactType _impactType)
	{
		float num = _impactType switch
		{
			ImpactType.Small => impactShakeSmallIntensity, 
			ImpactType.Medium => impactShakeMediumIntensity, 
			ImpactType.Big => impactShakeBigIntensity, 
			_ => 0f, 
		};
		if (!(num <= impactShakeIntensity))
		{
			impactShakeIntensity = num;
			impactShakeDecaySpeed = impactShakeDecayRate;
		}
	}

	private void LocalMeshUpdate()
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ecd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ed2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ed7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0edf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ee5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0eef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ef7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0efc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f0f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f14: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f49: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0db9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dbe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dc3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dcb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dd6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ddb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fcf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fdb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fe0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fe5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ffc: Unknown result type (might be due to invalid IL or missing references)
		//IL_1002: Unknown result type (might be due to invalid IL or missing references)
		//IL_1007: Unknown result type (might be due to invalid IL or missing references)
		//IL_100c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e4f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0deb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e0a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e20: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
		//IL_038d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0256: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
		//IL_10d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_10e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_10eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_10f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_10fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_1101: Unknown result type (might be due to invalid IL or missing references)
		//IL_1106: Unknown result type (might be due to invalid IL or missing references)
		//IL_110c: Unknown result type (might be due to invalid IL or missing references)
		//IL_1111: Unknown result type (might be due to invalid IL or missing references)
		//IL_1116: Unknown result type (might be due to invalid IL or missing references)
		//IL_1133: Unknown result type (might be due to invalid IL or missing references)
		//IL_1139: Unknown result type (might be due to invalid IL or missing references)
		//IL_113e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1144: Unknown result type (might be due to invalid IL or missing references)
		//IL_1149: Unknown result type (might be due to invalid IL or missing references)
		//IL_114e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1154: Unknown result type (might be due to invalid IL or missing references)
		//IL_1159: Unknown result type (might be due to invalid IL or missing references)
		//IL_115e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1166: Unknown result type (might be due to invalid IL or missing references)
		//IL_116b: Unknown result type (might be due to invalid IL or missing references)
		//IL_116d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1178: Unknown result type (might be due to invalid IL or missing references)
		//IL_117d: Unknown result type (might be due to invalid IL or missing references)
		//IL_1182: Unknown result type (might be due to invalid IL or missing references)
		//IL_1187: Unknown result type (might be due to invalid IL or missing references)
		//IL_1039: Unknown result type (might be due to invalid IL or missing references)
		//IL_1044: Unknown result type (might be due to invalid IL or missing references)
		//IL_104f: Unknown result type (might be due to invalid IL or missing references)
		//IL_105a: Unknown result type (might be due to invalid IL or missing references)
		//IL_105f: Unknown result type (might be due to invalid IL or missing references)
		//IL_1064: Unknown result type (might be due to invalid IL or missing references)
		//IL_1081: Unknown result type (might be due to invalid IL or missing references)
		//IL_1086: Unknown result type (might be due to invalid IL or missing references)
		//IL_1092: Unknown result type (might be due to invalid IL or missing references)
		//IL_1098: Unknown result type (might be due to invalid IL or missing references)
		//IL_109d: Unknown result type (might be due to invalid IL or missing references)
		//IL_10ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_10b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_10b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_041e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0423: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Unknown result type (might be due to invalid IL or missing references)
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_11b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_11bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_11c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0541: Unknown result type (might be due to invalid IL or missing references)
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0584: Unknown result type (might be due to invalid IL or missing references)
		//IL_0589: Unknown result type (might be due to invalid IL or missing references)
		//IL_058b: Unknown result type (might be due to invalid IL or missing references)
		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0548: Unknown result type (might be due to invalid IL or missing references)
		//IL_0552: Unknown result type (might be due to invalid IL or missing references)
		//IL_0557: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
		//IL_056e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0577: Unknown result type (might be due to invalid IL or missing references)
		//IL_057c: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0662: Unknown result type (might be due to invalid IL or missing references)
		//IL_0664: Unknown result type (might be due to invalid IL or missing references)
		//IL_066a: Unknown result type (might be due to invalid IL or missing references)
		//IL_066f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0808: Unknown result type (might be due to invalid IL or missing references)
		//IL_0849: Unknown result type (might be due to invalid IL or missing references)
		//IL_084e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0859: Unknown result type (might be due to invalid IL or missing references)
		//IL_0864: Unknown result type (might be due to invalid IL or missing references)
		//IL_0869: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0615: Unknown result type (might be due to invalid IL or missing references)
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0620: Unknown result type (might be due to invalid IL or missing references)
		//IL_0625: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0722: Unknown result type (might be due to invalid IL or missing references)
		//IL_0727: Unknown result type (might be due to invalid IL or missing references)
		//IL_073b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0740: Unknown result type (might be due to invalid IL or missing references)
		//IL_0745: Unknown result type (might be due to invalid IL or missing references)
		//IL_0748: Unknown result type (might be due to invalid IL or missing references)
		//IL_074d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0752: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_07fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0764: Unknown result type (might be due to invalid IL or missing references)
		//IL_0769: Unknown result type (might be due to invalid IL or missing references)
		//IL_076c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0771: Unknown result type (might be due to invalid IL or missing references)
		//IL_077b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0782: Unknown result type (might be due to invalid IL or missing references)
		//IL_0784: Unknown result type (might be due to invalid IL or missing references)
		//IL_0789: Unknown result type (might be due to invalid IL or missing references)
		//IL_078e: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_07bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_090d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0917: Unknown result type (might be due to invalid IL or missing references)
		//IL_09be: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_09ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_09d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a13: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a28: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a30: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a6c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a71: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a86: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a93: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ac3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ace: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ade: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0af9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b04: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b09: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b0e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b10: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b13: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b18: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b1f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b24: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b26: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b34: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b39: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b44: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b76: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bde: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bf8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bfd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c02: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c09: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c0d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c12: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c1a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c20: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c25: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b57: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b64: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b69: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c64: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c68: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c35: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c37: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c73: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c78: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c89: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c8e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cb9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cda: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d54: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d59: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d61: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d69: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d73: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)meshTransform))
		{
			return;
		}
		if (meshDeparented)
		{
			Transform val = meshTransform;
			val.position -= impactShakePositionOffset + sparkImpactShakePositionOffset;
			Transform val2 = meshTransform;
			val2.rotation *= Quaternion.Inverse(Quaternion.Euler(impactShakeRotationOffset + sparkImpactShakeRotationOffset));
			Transform val3 = meshTransform;
			val3.position -= nonLocalMeshPositionApplied;
			Transform val4 = meshTransform;
			val4.rotation *= Quaternion.Inverse(nonLocalMeshRotationApplied);
		}
		else
		{
			meshTransform.localPosition = meshOriginalLocalPosition;
			meshTransform.localRotation = meshOriginalLocalRotation;
		}
		nonLocalMeshPositionApplied = Vector3.zero;
		nonLocalMeshRotationApplied = Quaternion.identity;
		CalculateMeshVelocity();
		localSeatIndex = -1;
		for (int i = 0; i < seats.Length; i++)
		{
			VehicleSeat vehicleSeat = seats[i];
			PlayerAvatar seatedPlayer = vehicleSeat.seatedPlayer;
			if (PlayerPhysicsReady(seatedPlayer) && seatedPlayer.isLocal && Object.op_Implicit((Object)(object)seatedPlayer.localCamera) && Object.op_Implicit((Object)(object)vehicleSeat.seatTransform))
			{
				localSeatIndex = i;
				break;
			}
		}
		UpdateMeshWobble();
		UpdateDriftRumble();
		if (cameraLookForwardOverrideTimer > 0f)
		{
			cameraLookForwardOverrideTimer -= Time.deltaTime;
		}
		if (localSeatIndex >= 0)
		{
			VehicleSeat vehicleSeat2 = seats[localSeatIndex];
			PlayerAvatar seatedPlayer2 = vehicleSeat2.seatedPlayer;
			if (Vector3.Distance(seatedPlayer2.tumble.rb.position, vehicleSeat2.seatTransform.position) < 2f)
			{
				if (!meshDeparented)
				{
					DeparentMesh();
				}
				if (localMeshFollowResetPending)
				{
					Vector3 position = meshOriginalParent.TransformPoint(meshOriginalLocalPosition);
					Quaternion rotation = ((Component)this).transform.rotation * meshOriginalLocalRotation;
					meshTransform.position = position;
					meshTransform.rotation = rotation;
					smoothedCameraOffset = Vector3.zero;
					smoothedCameraOffsetVelocity = Vector3.zero;
					smoothedCameraOffsetInitialized = false;
					forwardClampExcess = 0f;
					forwardClampExcessVelocity = 0f;
					previousFrameOffset = Vector3.zero;
					previousFrameOffsetValid = false;
					offsetBufferCount = 0;
					offsetBufferIndex = 0;
					smoothedTargetRotation = rotation;
					smoothedTargetRotationValid = false;
					meshRotationXVelocity = 0f;
					meshRotationYVelocity = 0f;
					meshRotationZVelocity = 0f;
					meshTiltPushCurrent = 0f;
					meshTiltPushVelocity = 0f;
					meshDriftPushCurrent = 0f;
					meshDriftPushVelocity = 0f;
					meshDriftHorizontalPushCurrent = 0f;
					meshDriftHorizontalPushVelocity = 0f;
					meshDriftForwardPushCurrent = 0f;
					meshDriftForwardPushVelocity = 0f;
					meshDriftYRotationCurrent = 0f;
					meshDriftYRotationVelocity = 0f;
					meshUptiltCurrentForward = 0f;
					meshUptiltForwardVelocity = 0f;
					meshUptiltCurrentDown = 0f;
					meshUptiltDownVelocity = 0f;
					meshAppliedBaseOffsetX = 0f;
					meshAppliedBaseOffsetY = 0f;
					driftMeshOffsetBlend = 0f;
					localMeshFollowResetPending = false;
				}
				Vector3 position2 = seatedPlayer2.localCamera.GetOverrideTransform().position;
				Vector3 val5 = meshOriginalParent.TransformPoint(meshOriginalLocalPosition) - position2;
				Vector3 forward = seatedPlayer2.localCamera.GetOverrideTransform().forward;
				forward.y = 0f;
				forward.Normalize();
				float num = Vector3.Dot(val5, forward);
				float num2 = Mathf.Max(0f, num - meshMaxForwardOffset);
				forwardClampExcess = Mathf.SmoothDamp(forwardClampExcess, num2, ref forwardClampExcessVelocity, 0.02f, float.PositiveInfinity, Time.smoothDeltaTime);
				if (forwardClampExcess > 0.001f)
				{
					val5 -= forward * forwardClampExcess;
				}
				Vector3 val6 = val5;
				if (!previousFrameOffsetValid)
				{
					previousFrameOffset = val5;
					previousFrameOffsetValid = true;
					for (int j = 0; j < offsetBuffer.Length; j++)
					{
						offsetBuffer[j] = val5;
					}
					offsetBufferCount = offsetBuffer.Length;
					offsetBufferIndex = 0;
				}
				else
				{
					float num3 = 1f - Mathf.Exp(-60f * Time.smoothDeltaTime);
					Vector3 val7 = Vector3.Lerp(previousFrameOffset, val5, num3);
					Vector3 val8 = val7 - previousFrameOffset;
					float num4 = 0.15f;
					if (val8.sqrMagnitude > num4 * num4)
					{
						val7 = previousFrameOffset + val8.normalized * num4;
					}
					previousFrameOffset = val7;
					offsetBuffer[offsetBufferIndex] = previousFrameOffset;
					offsetBufferIndex = (offsetBufferIndex + 1) % offsetBuffer.Length;
					if (offsetBufferCount < offsetBuffer.Length)
					{
						offsetBufferCount++;
					}
					Vector3 val9 = Vector3.zero;
					for (int k = 0; k < offsetBufferCount; k++)
					{
						val9 += offsetBuffer[k];
					}
					val6 = val9 / (float)offsetBufferCount;
				}
				if (Vector3.Distance(meshTransform.position, position2 + val6) > meshTeleportDistance)
				{
					smoothedCameraOffset = val6;
					smoothedCameraOffsetVelocity = Vector3.zero;
					forwardClampExcess = 0f;
					forwardClampExcessVelocity = 0f;
					previousFrameOffset = val5;
					for (int l = 0; l < offsetBuffer.Length; l++)
					{
						offsetBuffer[l] = val5;
					}
					offsetBufferCount = offsetBuffer.Length;
					offsetBufferIndex = 0;
					smoothedTargetRotation = ((Component)this).transform.rotation * meshOriginalLocalRotation;
					smoothedTargetRotationValid = true;
					meshRotationXVelocity = 0f;
					meshRotationYVelocity = 0f;
					meshRotationZVelocity = 0f;
					smoothedCameraOffsetInitialized = true;
				}
				if (!smoothedCameraOffsetInitialized)
				{
					smoothedCameraOffset = val6;
					smoothedCameraOffsetVelocity = Vector3.zero;
					meshRotationXVelocity = 0f;
					meshRotationYVelocity = 0f;
					meshRotationZVelocity = 0f;
					smoothedCameraOffsetInitialized = true;
				}
				else
				{
					float num5 = ((driftBoostActiveTimer > 0f) ? (meshOffsetSmoothTime * 0.5f) : meshOffsetSmoothTime);
					float num6 = Mathf.Clamp01(Mathf.Abs(((Component)this).transform.forward.y) / 0.7f);
					Vector3 linearVelocity = rb.linearVelocity;
					float num7 = Mathf.Clamp01(linearVelocity.magnitude / 10f);
					num5 = Mathf.Lerp(num5, Time.smoothDeltaTime * 2f, num6 * num7);
					smoothedCameraOffset = Vector3.SmoothDamp(smoothedCameraOffset, val6, ref smoothedCameraOffsetVelocity, num5, float.PositiveInfinity, Time.smoothDeltaTime);
					Vector3 val10 = val6 - smoothedCameraOffset;
					if (val10.sqrMagnitude > 0.0001f)
					{
						Vector3 normalized = val10.normalized;
						float num8 = Vector3.Dot(smoothedCameraOffsetVelocity, normalized);
						Vector3 val11 = smoothedCameraOffsetVelocity - num8 * normalized;
						float num9 = 1f - Mathf.Exp(-8f * Time.smoothDeltaTime);
						smoothedCameraOffsetVelocity = num8 * normalized + val11 * (1f - num9);
					}
					float num10 = 1f - Mathf.Exp(-4f * Time.smoothDeltaTime);
					smoothedCameraOffsetVelocity *= 1f - num10 * 0.3f;
				}
				float num11 = Mathf.Clamp01(Mathf.Abs(((Component)this).transform.forward.y) / 0.94f);
				meshTiltPushCurrent = Mathf.SmoothDamp(meshTiltPushCurrent, num11, ref meshTiltPushVelocity, meshTiltPushSmoothTime);
				Vector3 val12 = -meshTransform.up * meshTiltPushCurrent * meshTiltPushDistance;
				float num12 = driftLowSpeedEaseProgress * driftLowSpeedEaseProgress * (3f - 2f * driftLowSpeedEaseProgress);
				float num13 = Mathf.Lerp(0f, 1f, num12);
				driftMeshOffsetBlend = Mathf.MoveTowards(driftMeshOffsetBlend, isDrifting ? 1f : 0f, Time.deltaTime / 0.2f);
				float num14 = driftMeshOffsetBlend * driftMeshOffsetBlend * (3f - 2f * driftMeshOffsetBlend);
				Vector3 val13 = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
				float num15 = Mathf.Clamp01(val13.magnitude / (maxForwardSpeed + hyperMaxSpeed));
				float num16 = Mathf.Lerp(0.1f, 1f, num15);
				float num17 = num13 * num14 * num16;
				float num18 = driftDirection;
				if (!SemiFunc.IsMasterClientOrSingleplayer() && (Object)(object)currentDriver != (Object)null)
				{
					float controlInputX = ControlInputX;
					if (Mathf.Abs(controlInputX) > 0.1f)
					{
						num18 = Mathf.Sign(controlInputX);
					}
				}
				float num19 = driftCurrentMeshDownOffset * num17;
				meshDriftPushCurrent = Mathf.SmoothDamp(meshDriftPushCurrent, num19, ref meshDriftPushVelocity, 0.03f);
				Vector3 val14 = -meshTransform.up * meshDriftPushCurrent;
				float num20 = driftCurrentMeshHorizontalOffset * num17 * (0f - num18);
				meshDriftHorizontalPushCurrent = Mathf.SmoothDamp(meshDriftHorizontalPushCurrent, num20, ref meshDriftHorizontalPushVelocity, 0.03f);
				Vector3 right = seatedPlayer2.localCamera.GetOverrideTransform().right;
				right.y = 0f;
				right.Normalize();
				Vector3 val15 = right * meshDriftHorizontalPushCurrent;
				float num21 = driftCurrentMeshForwardOffset * num17;
				meshDriftForwardPushCurrent = Mathf.SmoothDamp(meshDriftForwardPushCurrent, num21, ref meshDriftForwardPushVelocity, 0.03f);
				Vector3 forward2 = seatedPlayer2.localCamera.GetOverrideTransform().forward;
				forward2.y = 0f;
				forward2.Normalize();
				Vector3 val16 = forward2 * meshDriftForwardPushCurrent;
				float num22 = driftCurrentMeshYRotationOffset * num17 * (0f - num18);
				meshDriftYRotationCurrent = Mathf.SmoothDamp(meshDriftYRotationCurrent, num22, ref meshDriftYRotationVelocity, 0.03f);
				Vector3 val17 = wobbleDirectionSmoothed * wobblePositionOffset + -meshTransform.up * driftRumblePositionOffset + meshTransform.right * sparkShakePositionOffset;
				Vector3 val18 = position2 + smoothedCameraOffset + val12 + val14 + val15 + val16;
				float num23 = Vector3.Dot(val18 - position2, forward);
				if (num23 > meshMaxForwardOffset)
				{
					val18 -= forward * (num23 - meshMaxForwardOffset);
				}
				float num24 = Mathf.Clamp01(Mathf.Abs(((Component)this).transform.forward.y) / 0.7071f);
				meshUptiltCurrentForward = Mathf.SmoothDamp(meshUptiltCurrentForward, num24 * meshUptiltForwardOffset, ref meshUptiltForwardVelocity, meshTiltPushSmoothTime);
				meshUptiltCurrentDown = Mathf.SmoothDamp(meshUptiltCurrentDown, num24 * meshUptiltDownOffset, ref meshUptiltDownVelocity, meshTiltPushSmoothTime);
				val18 += forward * meshUptiltCurrentForward - Vector3.up * meshUptiltCurrentDown;
				val18 += val17;
				Quaternion val19 = ((Component)this).transform.rotation * meshOriginalLocalRotation;
				if (!smoothedTargetRotationValid)
				{
					smoothedTargetRotation = val19;
					smoothedTargetRotationValid = true;
				}
				else
				{
					float num25 = 1f - Mathf.Exp(-15f * Time.smoothDeltaTime);
					smoothedTargetRotation = Quaternion.Slerp(smoothedTargetRotation, val19, num25);
				}
				Quaternion val20 = smoothedTargetRotation;
				Quaternion rotation2 = meshTransform.rotation;
				Vector3 eulerAngles = rotation2.eulerAngles;
				eulerAngles.x -= meshAppliedBaseOffsetX;
				eulerAngles.y -= meshAppliedBaseOffsetY;
				Vector3 eulerAngles2 = val20.eulerAngles;
				float num26 = 1f - Mathf.Exp(-120f * Time.smoothDeltaTime);
				float num27 = Mathf.LerpAngle(eulerAngles.x, eulerAngles2.x, num26);
				float num28 = Mathf.LerpAngle(eulerAngles.y, eulerAngles2.y, num26);
				float num29 = Mathf.LerpAngle(eulerAngles.z, eulerAngles2.z, num26);
				meshAppliedBaseOffsetX = driftRumbleRotationOffset + sparkShakeRotationOffset;
				meshAppliedBaseOffsetY = meshDriftYRotationCurrent;
				meshTransform.position = val18;
				Quaternion val21 = Quaternion.Euler(num27 + meshAppliedBaseOffsetX, num28 + meshAppliedBaseOffsetY, num29);
				meshTransform.rotation = val21 * Quaternion.AngleAxis(wobbleRotationOffset, Vector3.right);
			}
			else
			{
				smoothedCameraOffsetInitialized = false;
				previousFrameOffsetValid = false;
				smoothedTargetRotationValid = false;
				localMeshFollowResetPending = true;
				bool flag = meshDeparented;
				if (!meshDeparented)
				{
					DeparentMesh();
				}
				Vector3 val22 = meshOriginalParent.TransformPoint(meshOriginalLocalPosition);
				Quaternion val23 = meshOriginalParent.rotation * meshOriginalLocalRotation;
				if (flag)
				{
					meshTransform.position = Vector3.Lerp(meshTransform.position, val22, 1f - Mathf.Exp((0f - meshDismountSpeed) * Time.deltaTime));
					meshTransform.rotation = Quaternion.Slerp(meshTransform.rotation, val23, 1f - Mathf.Exp(-5f * Time.deltaTime));
				}
				else
				{
					meshTransform.position = val22;
					meshTransform.rotation = val23;
				}
			}
			if (Object.op_Implicit((Object)(object)ArenaRace.instance) && seatedPlayer2.isLocal)
			{
				seatedPlayer2.physGrabber.OverrideAlwaysGrabbable(0.5f);
			}
		}
		else
		{
			smoothedCameraOffsetInitialized = false;
			previousFrameOffsetValid = false;
			smoothedTargetRotationValid = false;
			localMeshFollowResetPending = true;
			if (meshDeparented && !HasNonLocalSeatedPlayer())
			{
				Vector3 val24 = meshOriginalParent.TransformPoint(meshOriginalLocalPosition);
				Quaternion val25 = meshOriginalParent.rotation * meshOriginalLocalRotation;
				float num30 = Vector3.Distance(meshTransform.position, val24);
				meshTransform.position = Vector3.Lerp(meshTransform.position, val24, 1f - Mathf.Exp((0f - meshDismountSpeed) * Time.deltaTime));
				meshTransform.rotation = Quaternion.Slerp(meshTransform.rotation, val25, 1f - Mathf.Exp(-5f * Time.deltaTime));
				float num31 = Quaternion.Angle(meshTransform.rotation, val25);
				if (num30 < 0.01f && num31 < 1f)
				{
					ReparentMesh();
				}
			}
		}
		UpdateImpactShake();
		UpdateSparkImpactShake();
		if (Object.op_Implicit((Object)(object)meshTransform) && (impactShakeIntensity > 0f || sparkImpactShakeIntensity > 0f))
		{
			Transform val26 = meshTransform;
			val26.position += impactShakePositionOffset + sparkImpactShakePositionOffset;
			Transform val27 = meshTransform;
			val27.rotation *= Quaternion.Euler(impactShakeRotationOffset + sparkImpactShakeRotationOffset);
		}
		if (localSeatIndex < 0 && Object.op_Implicit((Object)(object)meshTransform))
		{
			nonLocalMeshPositionApplied = wobbleDirectionSmoothed * wobblePositionOffset + meshTransform.right * sparkShakePositionOffset;
			nonLocalMeshRotationApplied = Quaternion.Euler(wobbleRotationOffset + sparkShakeRotationOffset, 0f, 0f);
			Transform val28 = meshTransform;
			val28.position += nonLocalMeshPositionApplied;
			Transform val29 = meshTransform;
			val29.rotation *= nonLocalMeshRotationApplied;
		}
		if (!Object.op_Implicit((Object)(object)meshTransform))
		{
			return;
		}
		meshVisualPositionOffset = wobbleDirectionSmoothed * wobblePositionOffset + meshTransform.right * sparkShakePositionOffset + impactShakePositionOffset + sparkImpactShakePositionOffset;
		meshVisualRotationEulerOffset = new Vector3(wobbleRotationOffset + sparkShakeRotationOffset, 0f, 0f) + impactShakeRotationOffset + sparkImpactShakeRotationOffset;
		Quaternion val30 = Quaternion.Euler(meshVisualRotationEulerOffset);
		Quaternion rotationOffset = ((Component)this).transform.rotation * val30 * Quaternion.Inverse(((Component)this).transform.rotation);
		for (int m = 0; m < seats.Length; m++)
		{
			if ((Object)(object)seats[m].seatedPlayer != (Object)null)
			{
				seats[m].seatedPlayer.playerAvatarVisuals.MountOffsetOverride(meshVisualPositionOffset, rotationOffset, ((Component)this).transform.position);
			}
		}
	}

	private bool HasNonLocalSeatedPlayer()
	{
		if (seats == null)
		{
			return false;
		}
		for (int i = 0; i < seats.Length; i++)
		{
			if ((Object)(object)seats[i].seatedPlayer != (Object)null && !seats[i].seatedPlayer.isLocal)
			{
				return true;
			}
		}
		return false;
	}

	private void NonLocalMeshPredictionReset()
	{
		nonLocalMeshPredictionInitialized = false;
		nonLocalSmoothedVelocityInitialized = false;
	}

	private void NonLocalMeshPredictionWallClamp(Vector3 _actualMeshPos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = nonLocalPredictedMeshWorldPosition - _actualMeshPos;
		float magnitude = val.magnitude;
		if (magnitude < 0.02f)
		{
			return;
		}
		Vector3 val2 = val / magnitude;
		RaycastHit[] array = rb.SweepTestAll(val2, magnitude, (QueryTriggerInteraction)1);
		float num = float.MaxValue;
		RaycastHit[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit val3 = array2[i];
			if (!(val3.distance <= 0f) && ((Component)val3.collider).gameObject.layer == nonLocalMeshPredictionWallLayer)
			{
				bool flag = val3.normal.y > 0.6f;
				bool flag2 = Vector3.Dot(val2, val3.normal) < -0.5f;
				if ((!flag || flag2) && val3.distance < num)
				{
					num = val3.distance;
				}
			}
		}
		if (num < magnitude)
		{
			nonLocalPredictedMeshWorldPosition = _actualMeshPos + val2 * Mathf.Max(0f, num - 0.05f);
		}
	}

	private void NetworkExtrapolateTick()
	{
		if (!networkRigidbodyExtrapolate || SemiFunc.IsMasterClientOrSingleplayer() || !Object.op_Implicit((Object)(object)nonLocalPhotonTransformView))
		{
			return;
		}
		nonLocalPhotonTransformView.OverrideExtrapolate(nonLocalMeshPredictionMaxExtrapolation, 0.2f);
		for (int i = 0; i < seats.Length; i++)
		{
			PlayerAvatar seatedPlayer = seats[i].seatedPlayer;
			if (PlayerPhysicsReady(seatedPlayer) && Object.op_Implicit((Object)(object)seatedPlayer.tumble.physGrabObject) && Object.op_Implicit((Object)(object)seatedPlayer.tumble.physGrabObject.photonTransformView))
			{
				seatedPlayer.tumble.physGrabObject.photonTransformView.OverrideExtrapolate(nonLocalMeshPredictionMaxExtrapolation, 0.2f);
			}
		}
	}

	private void NonLocalMeshPredictionLookaheadTargetGet(Vector3 _actualMeshPosition, Quaternion _actualMeshRotation, out Vector3 _targetPosition, out Quaternion _targetRotation, out Vector3 _linearVelocity, out Vector3 _angularVelocity)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		Vector3 velocity = fakeVelocities.velocity;
		Vector3 angularVelocity = fakeVelocities.angularVelocity;
		if (!nonLocalSmoothedVelocityInitialized)
		{
			nonLocalSmoothedLinearVelocity = velocity;
			nonLocalSmoothedAngularVelocity = angularVelocity;
			nonLocalSmoothedVelocityInitialized = true;
		}
		else
		{
			float num = 1f - Mathf.Exp((0f - Time.smoothDeltaTime) / Mathf.Max(0.001f, nonLocalMeshPredictionVelocitySmoothTime));
			nonLocalSmoothedLinearVelocity = Vector3.Lerp(nonLocalSmoothedLinearVelocity, velocity, num);
			nonLocalSmoothedAngularVelocity = Vector3.Lerp(nonLocalSmoothedAngularVelocity, angularVelocity, num);
		}
		_linearVelocity = nonLocalSmoothedLinearVelocity;
		float magnitude = _linearVelocity.magnitude;
		float num2 = Mathf.InverseLerp(nonLocalMeshPredictionMinSpeed, nonLocalMeshPredictionMinSpeed + nonLocalMeshPredictionMinSpeedRampRange, magnitude);
		Vector3 val = _linearVelocity * nonLocalMeshPredictionLookahead * num2;
		float magnitude2 = val.magnitude;
		if (magnitude2 > nonLocalMeshPredictionMaxPositionOffset)
		{
			val *= nonLocalMeshPredictionMaxPositionOffset / magnitude2;
		}
		_angularVelocity = nonLocalSmoothedAngularVelocity;
		float magnitude3 = _angularVelocity.magnitude;
		Quaternion val2 = Quaternion.identity;
		if (magnitude3 > 0.001f)
		{
			float num3 = magnitude3 * 57.29578f * nonLocalMeshPredictionLookahead * num2;
			if (num3 > nonLocalMeshPredictionMaxRotationOffset)
			{
				num3 = nonLocalMeshPredictionMaxRotationOffset;
			}
			val2 = Quaternion.AngleAxis(num3, _angularVelocity / magnitude3);
		}
		_targetPosition = _actualMeshPosition + val;
		_targetRotation = val2 * _actualMeshRotation;
	}

	private void NonLocalMeshPredictionNetworkTargetGet(Vector3 _actualMeshPosition, Quaternion _actualMeshRotation, out Vector3 _targetPosition, out Quaternion _targetRotation, out Vector3 _linearVelocity, out Vector3 _angularVelocity)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		Vector3 _position = ((Component)this).transform.position;
		Quaternion _rotation = ((Component)this).transform.rotation;
		_linearVelocity = rb.linearVelocity;
		_angularVelocity = rb.angularVelocity;
		if (!SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)nonLocalPhotonTransformView) && nonLocalPhotonTransformView.receivedAny)
		{
			float num = NonLocalPacketAgeGet() + nonLocalMeshPredictionExtraLead;
			num = Mathf.Clamp(num, 0f, nonLocalMeshPredictionMaxExtrapolation);
			_position = nonLocalPhotonTransformView.NetworkPositionGet();
			_rotation = nonLocalPhotonTransformView.NetworkRotationGet();
			_linearVelocity = nonLocalPhotonTransformView.receivedVelocity;
			_angularVelocity = NonLocalSmoothedAngularVelocityGet(nonLocalPhotonTransformView.receivedAngularVelocity);
			NonLocalMeshPredictionExtrapolate(ref _position, ref _rotation, ref _linearVelocity, _angularVelocity, num);
		}
		Quaternion val = Quaternion.Inverse(((Component)this).transform.rotation);
		Vector3 val2 = val * (_actualMeshPosition - ((Component)this).transform.position);
		Quaternion val3 = val * _actualMeshRotation;
		_targetPosition = _position + _rotation * val2;
		_targetRotation = _rotation * val3;
	}

	private float NonLocalPacketAgeGet()
	{
		float result = (float)(PhotonNetwork.ServerTimestamp - nonLocalPhotonTransformView.receivedServerTimestamp) * 0.001f;
		if (!nonLocalMeshPredictionPacketClock)
		{
			return result;
		}
		if (nonLocalPacketTimestamp != nonLocalPhotonTransformView.receivedServerTimestamp)
		{
			nonLocalPacketTimestamp = nonLocalPhotonTransformView.receivedServerTimestamp;
			nonLocalPacketAge = result;
			nonLocalPacketLocalTime = Time.time;
		}
		return nonLocalPacketAge + (Time.time - nonLocalPacketLocalTime);
	}

	private Vector3 NonLocalSmoothedAngularVelocityGet(Vector3 _rawAngularVelocity)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (!nonLocalSmoothedVelocityInitialized)
		{
			nonLocalSmoothedAngularVelocity = _rawAngularVelocity;
			nonLocalSmoothedVelocityInitialized = true;
		}
		else
		{
			float num = 1f - Mathf.Exp((0f - Time.deltaTime) / Mathf.Max(0.001f, nonLocalMeshPredictionAngularSmoothTime));
			nonLocalSmoothedAngularVelocity = Vector3.Lerp(nonLocalSmoothedAngularVelocity, _rawAngularVelocity, num);
		}
		return nonLocalSmoothedAngularVelocity;
	}

	private void NonLocalMeshPredictionExtrapolate(ref Vector3 _position, ref Quaternion _rotation, ref Vector3 _velocity, Vector3 _angularVelocity, float _time)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		if (!(_time <= 0f))
		{
			int num = Mathf.Clamp(Mathf.CeilToInt(_time / 0.02f), 1, 30);
			float num2 = _time / (float)num;
			Quaternion val = Quaternion.AngleAxis(_angularVelocity.y * nonLocalMeshPredictionTurnFollow * 57.29578f * num2 * 0.5f, Vector3.up);
			for (int i = 0; i < num; i++)
			{
				_velocity = val * _velocity;
				_position += _velocity * num2;
				_velocity = val * _velocity;
			}
			float num3 = Mathf.Min(_angularVelocity.magnitude * 57.29578f * _time, nonLocalMeshPredictionTimedMaxRotation);
			if (num3 > 0.01f)
			{
				_rotation = Quaternion.AngleAxis(num3, _angularVelocity.normalized) * _rotation;
			}
		}
	}

	private void NonLocalMeshPredictionSplitCorrect(Vector3 _targetPosition, Vector3 _linearVelocity, float _sideLerp, float _catchUp, float _deltaTime)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = _targetPosition - nonLocalPredictedMeshWorldPosition;
		float magnitude = _linearVelocity.magnitude;
		if (magnitude < 1f)
		{
			nonLocalPredictedMeshWorldPosition += val * _sideLerp;
			return;
		}
		Vector3 val2 = _linearVelocity / magnitude;
		Vector3 val3 = val2 * Vector3.Dot(val, val2);
		Vector3 val4 = val - val3;
		float num = Mathf.Lerp(nonLocalMeshPredictionAlongSmoothTime, nonLocalMeshPredictionSmoothTime, _catchUp);
		float num2 = 1f - Mathf.Exp((0f - _deltaTime) / Mathf.Max(0.001f, num));
		float num3 = Vector3.Dot(val3, val2) * num2;
		if (_catchUp < 1f)
		{
			float num4 = (0f - magnitude) * _deltaTime * (1f - nonLocalMeshPredictionMinForwardStep);
			num3 = Mathf.Max(num3, num4);
		}
		nonLocalPredictedMeshWorldPosition += val4 * _sideLerp + val2 * num3;
	}

	private void NonLocalPredictionCollidersCache()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (nonLocalPredictionColliders != null)
		{
			return;
		}
		List<Collider> list = new List<Collider>();
		Bounds val = new Bounds(((Component)this).transform.position, Vector3.zero);
		Collider[] componentsInChildren = ((Component)this).GetComponentsInChildren<Collider>(true);
		foreach (Collider val2 in componentsInChildren)
		{
			if (!val2.isTrigger && !((Object)(object)val2.attachedRigidbody != (Object)(object)rb))
			{
				list.Add(val2);
				val.Encapsulate(val2.bounds);
			}
		}
		nonLocalPredictionColliders = list.ToArray();
		float num = Vector3.Distance(((Component)this).transform.position, val.center);
		Vector3 extents = val.extents;
		nonLocalPredictionColliderRadius = num + extents.magnitude + 0.2f;
	}

	private void NonLocalMeshPredictionDepenetrate(Vector3 _actualMeshPosition, Quaternion _actualMeshRotation)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		NonLocalPredictionCollidersCache();
		if (nonLocalPredictionColliders.Length == 0)
		{
			return;
		}
		Quaternion val = Quaternion.Inverse(((Component)this).transform.rotation);
		Quaternion val2 = val * _actualMeshRotation;
		Vector3 val3 = val * (_actualMeshPosition - ((Component)this).transform.position);
		Quaternion val4 = nonLocalPredictedMeshWorldRotation * Quaternion.Inverse(val2);
		Vector3 val5 = nonLocalPredictedMeshWorldPosition - val4 * val3;
		int num = Physics.OverlapSphereNonAlloc(val5, nonLocalPredictionColliderRadius, nonLocalPredictionOverlap, 1 << nonLocalMeshPredictionWallLayer, (QueryTriggerInteraction)1);
		if (num == 0)
		{
			return;
		}
		Vector3 val6 = Vector3.zero;
		Collider[] array = nonLocalPredictionColliders;
		Vector3 val11 = default;
		float num2 = default;
		foreach (Collider val7 in array)
		{
			if (!Object.op_Implicit((Object)(object)val7) || !val7.enabled)
			{
				continue;
			}
			Transform transform = ((Component)val7).transform;
			Vector3 val8 = val5 + val4 * (val * (transform.position - ((Component)this).transform.position));
			Quaternion val9 = val4 * (val * transform.rotation);
			for (int j = 0; j < num; j++)
			{
				Collider val10 = nonLocalPredictionOverlap[j];
				if (Object.op_Implicit((Object)(object)val10) && !((Object)(object)val10.attachedRigidbody == (Object)(object)rb) && Physics.ComputePenetration(val7, val8 + val6, val9, val10, ((Component)val10).transform.position, ((Component)val10).transform.rotation, ref val11, ref num2) && (!(val11.y > 0.6f) || !(num2 < 0.1f)))
				{
					val6 += val11 * num2;
				}
			}
		}
		nonLocalPredictedMeshWorldPosition += val6;
	}

	private void NonLocalMeshPredictionUpdate()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0320: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Unknown result type (might be due to invalid IL or missing references)
		//IL_0327: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_0387: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_039e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		//IL_0507: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_0511: Unknown result type (might be due to invalid IL or missing references)
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_0469: Unknown result type (might be due to invalid IL or missing references)
		//IL_046e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Unknown result type (might be due to invalid IL or missing references)
		//IL_047a: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0493: Unknown result type (might be due to invalid IL or missing references)
		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0524: Unknown result type (might be due to invalid IL or missing references)
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_0539: Unknown result type (might be due to invalid IL or missing references)
		//IL_054c: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		if (!enableNonLocalMeshPrediction || !Object.op_Implicit((Object)(object)meshTransform) || !Object.op_Implicit((Object)(object)meshOriginalParent) || !Object.op_Implicit((Object)(object)fakeVelocities))
		{
			return;
		}
		if (localSeatIndex >= 0 || !HasNonLocalSeatedPlayer())
		{
			NonLocalMeshPredictionReset();
			return;
		}
		if (!meshDeparented)
		{
			DeparentMesh();
		}
		bool flag = nonLocalMeshPredictionNetworkTimed || Object.op_Implicit((Object)(object)ArenaRace.instance);
		float num = (flag ? Time.deltaTime : Time.smoothDeltaTime);
		Vector3 val = meshOriginalParent.TransformPoint(meshOriginalLocalPosition);
		Quaternion val2 = meshOriginalParent.rotation * meshOriginalLocalRotation;
		Vector3 _targetPosition;
		Quaternion _targetRotation;
		Vector3 _linearVelocity;
		Vector3 _angularVelocity;
		if (flag)
		{
			NonLocalMeshPredictionNetworkTargetGet(val, val2, out _targetPosition, out _targetRotation, out _linearVelocity, out _angularVelocity);
		}
		else
		{
			NonLocalMeshPredictionLookaheadTargetGet(val, val2, out _targetPosition, out _targetRotation, out _linearVelocity, out _angularVelocity);
		}
		float magnitude = _angularVelocity.magnitude;
		Vector3 val3 = Vector3.up;
		if (magnitude > 0.001f)
		{
			val3 = _angularVelocity / magnitude;
		}
		Vector3 val4 = (flag ? _targetPosition : val);
		Quaternion val5 = (flag ? _targetRotation : val2);
		if (!nonLocalMeshPredictionInitialized || (flag && SemiFunc.IsMasterClientOrSingleplayer()))
		{
			nonLocalPredictedMeshWorldPosition = _targetPosition;
			nonLocalPredictedMeshWorldRotation = _targetRotation;
			nonLocalMeshPredictionInitialized = true;
		}
		if (!flag || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			nonLocalPredictedMeshWorldPosition += _linearVelocity * num;
		}
		if (magnitude > 0.001f && (!flag || !SemiFunc.IsMasterClientOrSingleplayer()))
		{
			nonLocalPredictedMeshWorldRotation = Quaternion.AngleAxis(magnitude * 57.29578f * num, val3) * nonLocalPredictedMeshWorldRotation;
		}
		float num2 = Vector3.Distance(nonLocalPredictedMeshWorldPosition, _targetPosition);
		float num3 = Mathf.InverseLerp(nonLocalMeshPredictionCalmGapDistance, nonLocalMeshPredictionCatchUpGapDistance, num2);
		float num4 = Mathf.Lerp(flag ? nonLocalMeshPredictionTimedCalmSmoothTime : nonLocalMeshPredictionCalmSmoothTime, nonLocalMeshPredictionSmoothTime, num3);
		float num5 = 1f - Mathf.Exp((0f - num) / Mathf.Max(0.001f, num4));
		if (flag && nonLocalMeshPredictionAlongSmoothTime > 0f)
		{
			NonLocalMeshPredictionSplitCorrect(_targetPosition, _linearVelocity, num5, num3, num);
		}
		else
		{
			nonLocalPredictedMeshWorldPosition = Vector3.Lerp(nonLocalPredictedMeshWorldPosition, _targetPosition, num5);
		}
		if (Vector3.Distance(nonLocalPredictedMeshWorldPosition, val4) > nonLocalMeshPredictionSnapDistance)
		{
			nonLocalPredictedMeshWorldPosition = val4;
		}
		if (flag)
		{
			NonLocalMeshPredictionDepenetrate(val, val2);
		}
		else
		{
			NonLocalMeshPredictionWallClamp(val);
		}
		float num6 = Quaternion.Angle(nonLocalPredictedMeshWorldRotation, _targetRotation);
		float num7 = Mathf.InverseLerp(3f, 30f, num6);
		float num8 = Mathf.Lerp(flag ? nonLocalMeshPredictionRotationCalmSmoothTime : nonLocalMeshPredictionCalmSmoothTime, nonLocalMeshPredictionSmoothTime, num7);
		float num9 = 1f - Mathf.Exp((0f - num) / Mathf.Max(0.001f, num8));
		nonLocalPredictedMeshWorldRotation = Quaternion.Slerp(nonLocalPredictedMeshWorldRotation, _targetRotation, num9);
		if (Quaternion.Angle(nonLocalPredictedMeshWorldRotation, val5) > nonLocalMeshPredictionSnapAngle)
		{
			nonLocalPredictedMeshWorldRotation = val5;
		}
		Vector3 val6 = nonLocalPredictedMeshWorldPosition - val;
		Quaternion val7 = nonLocalPredictedMeshWorldRotation * Quaternion.Inverse(val2);
		Vector3 val8 = val + nonLocalMeshPositionApplied + impactShakePositionOffset + sparkImpactShakePositionOffset;
		Quaternion val9 = val2 * nonLocalMeshRotationApplied * Quaternion.Euler(impactShakeRotationOffset + sparkImpactShakeRotationOffset);
		Vector3 position = ((Component)this).transform.position;
		meshTransform.position = position + val7 * (val8 - position) + val6;
		meshTransform.rotation = val7 * val9;
		Quaternion val10 = Quaternion.Euler(meshVisualRotationEulerOffset);
		Quaternion val11 = ((Component)this).transform.rotation * val10 * Quaternion.Inverse(((Component)this).transform.rotation);
		Quaternion rotationOffset = val7 * val11;
		Vector3 positionOffset = meshVisualPositionOffset + val6;
		for (int i = 0; i < seats.Length; i++)
		{
			PlayerAvatar seatedPlayer = seats[i].seatedPlayer;
			if (!((Object)(object)seatedPlayer != (Object)null) || seatedPlayer.isLocal)
			{
				continue;
			}
			Vector3 val12 = Vector3.zero;
			Quaternion val13 = Quaternion.identity;
			bool flag2 = (Object)(object)seats[i].seatTransform != (Object)null;
			if (flag2)
			{
				Vector3 val14 = seats[i].seatTransform.position - position + meshVisualPositionOffset;
				val12 = position + val7 * val14 + val6;
				val13 = val7 * val11 * seats[i].seatTransform.rotation;
				if (Object.op_Implicit((Object)(object)seatedPlayer.tumble) && Object.op_Implicit((Object)(object)seatedPlayer.tumble.followPosition))
				{
					val12 += val13 * seatedPlayer.tumble.followPosition.localPosition;
				}
				val12 += val13 * Vector3.up * 0.05f;
			}
			seatedPlayer.playerAvatarVisuals.MountOffsetOverride(positionOffset, rotationOffset, ((Component)this).transform.position);
			if (flag2)
			{
				seatedPlayer.playerAvatarVisuals.MountSeatVisualOverride(val12, val13);
				((Component)seatedPlayer.playerAvatarVisuals).transform.position = val12;
				((Component)seatedPlayer.playerAvatarVisuals).transform.rotation = val13;
			}
			else
			{
				seatedPlayer.playerAvatarVisuals.MountSeatVisualClear();
			}
		}
	}

	private Material EnsureFrontLightMaterial()
	{
		if (!Object.op_Implicit((Object)(object)frontLightRenderer))
		{
			return null;
		}
		if (!Object.op_Implicit((Object)(object)cachedFrontLightMaterial))
		{
			cachedFrontLightMaterial = frontLightRenderer.material;
		}
		return cachedFrontLightMaterial;
	}

	private Material EnsureBackLightMaterial()
	{
		if (!Object.op_Implicit((Object)(object)backLightRenderer))
		{
			return null;
		}
		if (!Object.op_Implicit((Object)(object)cachedBackLightMaterial))
		{
			cachedBackLightMaterial = backLightRenderer.material;
		}
		return cachedBackLightMaterial;
	}

	private void UpdateLights()
	{
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_035e: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Unknown result type (might be due to invalid IL or missing references)
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_038f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_044c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		if (isFullyDestroyed)
		{
			return;
		}
		PlayerAvatar driver = Driver;
		bool flag = Object.op_Implicit((Object)(object)itemBattery) && itemBattery.batteryLife <= 0f;
		bool flag2 = ((Object)(object)driver != (Object)null || AIControlActive) && !flag;
		bool flag3 = ((!AIControlActive) ? (flag2 && (Object)(object)driver != (Object)null && DriverFullyMounted && ControlThrottle < 0f) : (flag2 && aiControlInputThrottle < 0f));
		bool flag4 = SemiFunc.Photosensitivity();
		if (Object.op_Implicit((Object)(object)frontLight) && Object.op_Implicit((Object)(object)frontLightRenderer))
		{
			bool flag5 = flag2 && !overrideFrontLightOffActive;
			float num3;
			if (flag4)
			{
				float num = (flag5 ? 1f : 0f);
				float num2 = 0.5f;
				frontLightPhotosensitiveLerp = Mathf.MoveTowards(frontLightPhotosensitiveLerp, num, Time.deltaTime * num2);
				num3 = frontLightPhotosensitiveLerp;
				frontLightOn = flag5;
				frontLightOnTimer = 0f;
				frontLightFlickerTimer = 0f;
				frontLightFlickerOff = false;
			}
			else
			{
				if (flag5 && !frontLightOn)
				{
					frontLightFlickerTimer = lightFlickerDuration;
					frontLightOnTimer = lightMinOnDuration;
					frontLightOn = true;
					frontLightFlickerOff = false;
				}
				else if (!flag5 && frontLightOn)
				{
					if (overrideFrontLightOffActive)
					{
						frontLightOnTimer = 0f;
					}
					if (frontLightOnTimer <= 0f && !frontLightFlickerOff)
					{
						frontLightFlickerTimer = lightFlickerDuration;
						frontLightFlickerOff = true;
					}
				}
				if (frontLightOnTimer > 0f)
				{
					frontLightOnTimer -= Time.deltaTime;
				}
				num3 = (frontLightOn ? 1f : 0f);
				if (frontLightFlickerTimer > 0f)
				{
					frontLightFlickerTimer -= Time.deltaTime;
					num3 = Random.Range(0.2f, 0.9f);
					if (frontLightFlickerTimer <= 0f && frontLightFlickerOff)
					{
						frontLightOn = false;
						frontLightFlickerOff = false;
						num3 = 0f;
					}
				}
				frontLightPhotosensitiveLerp = num3;
			}
			((Behaviour)frontLight).enabled = num3 > 0.01f;
			frontLight.color = frontLightEmissionColor * num3;
			Color val = ((num3 > 0.01f) ? (frontLightEmissionColor * num3) : Color.black);
			Material val2 = EnsureFrontLightMaterial();
			if (Object.op_Implicit((Object)(object)val2))
			{
				val2.SetColor(emissionColorID, val);
			}
		}
		if (Object.op_Implicit((Object)(object)backLight) && Object.op_Implicit((Object)(object)backLightRenderer))
		{
			bool flag6 = flag3;
			if (flag4)
			{
				float num4 = (flag6 ? 1f : 0f);
				float num5 = 0.5f;
				backLightPhotosensitiveLerp = Mathf.MoveTowards(backLightPhotosensitiveLerp, num4, Time.deltaTime * num5);
				float num6 = backLightPhotosensitiveLerp;
				backLightOn = flag6;
				backLightOnTimer = 0f;
				((Behaviour)backLight).enabled = num6 > 0.01f;
				backLight.color = backLightEmissionColor * num6;
				Color val3 = ((num6 > 0.01f) ? (backLightEmissionColor * num6) : Color.black);
				Material val4 = EnsureBackLightMaterial();
				if (Object.op_Implicit((Object)(object)val4))
				{
					val4.SetColor(emissionColorID, val3);
				}
			}
			else
			{
				if (flag6 && !backLightOn)
				{
					backLightOnTimer = lightMinOnDuration;
					backLightOn = true;
				}
				else if (!flag6 && backLightOn && backLightOnTimer <= 0f)
				{
					backLightOn = false;
				}
				if (backLightOnTimer > 0f)
				{
					backLightOnTimer -= Time.deltaTime;
				}
				((Behaviour)backLight).enabled = backLightOn;
				backLight.color = backLightEmissionColor;
				Color val5 = (backLightOn ? backLightEmissionColor : Color.black);
				Material val6 = EnsureBackLightMaterial();
				if (Object.op_Implicit((Object)(object)val6))
				{
					val6.SetColor(emissionColorID, val5);
				}
				backLightPhotosensitiveLerp = (backLightOn ? 1f : 0f);
			}
		}
		if (hasBackingUpBeeps)
		{
			if (flag3)
			{
				if (backingUpBeepTimer <= 0f)
				{
					backingUpBeepSound.Play(((Component)this).transform, soundEffectVolume);
					backingUpBeepTimer = backingUpBeepInterval;
				}
				backingUpBeepTimer -= Time.deltaTime;
			}
			else
			{
				backingUpBeepTimer = 0f;
			}
		}
		OverrideFrontLightOffTick();
	}

	public void OverrideFrontLightOff()
	{
		overrideFrontLightOffTimer = 0.1f;
		if (!overrideFrontLightOffActive)
		{
			overrideFrontLightOffActive = true;
		}
	}

	private void OverrideFrontLightOffTick()
	{
		if (overrideFrontLightOffActive)
		{
			overrideFrontLightOffTimer -= Time.deltaTime;
			if (overrideFrontLightOffTimer <= 0f)
			{
				overrideFrontLightOffActive = false;
			}
		}
	}

	private void EnforcePhotosensitivity()
	{
		if (SemiFunc.Photosensitivity())
		{
			if (Object.op_Implicit((Object)(object)driftLight) && ((Behaviour)driftLight).enabled)
			{
				((Behaviour)driftLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)boostLight) && ((Behaviour)boostLight).enabled)
			{
				((Behaviour)boostLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)fireLight) && ((Behaviour)fireLight).enabled)
			{
				((Behaviour)fireLight).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)sparkLightLeft) && ((Behaviour)sparkLightLeft).enabled)
			{
				((Behaviour)sparkLightLeft).enabled = false;
			}
			if (Object.op_Implicit((Object)(object)sparkLightRight) && ((Behaviour)sparkLightRight).enabled)
			{
				((Behaviour)sparkLightRight).enabled = false;
			}
			StopParticleForPhotosensitivity(driftParticles);
			StopParticleForPhotosensitivity(driftParticlesLevel1);
			StopParticleForPhotosensitivity(driftParticlesLevel2);
			StopParticleForPhotosensitivity(driftParticlesLevel3);
			StopParticleForPhotosensitivity(driftBoostLevel0ReachedParticles);
			StopParticleForPhotosensitivity(driftBoostLevel1ReachedParticles);
			StopParticleForPhotosensitivity(driftBoostLevel2ReachedParticles);
			StopParticleForPhotosensitivity(driftBoostLevel3ReachedParticles);
			StopParticleForPhotosensitivity(driftBoostLevel1Particles);
			StopParticleForPhotosensitivity(driftBoostLevel2Particles);
			StopParticleForPhotosensitivity(driftBoostLevel3Particles);
			StopParticleForPhotosensitivity(impactParticlesSmall);
			StopParticleForPhotosensitivity(impactParticlesMedium);
			StopParticleForPhotosensitivity(impactParticlesBig);
			StopParticleForPhotosensitivity(fallDamageParticles);
			StopParticleForPhotosensitivity(nosediveParticles);
			StopParticleForPhotosensitivity(nosediveParticlesHeavy);
			StopParticleForPhotosensitivity(particlesForGoingFast);
			StopParticleForPhotosensitivity(tumbleParticles);
			StopParticleForPhotosensitivity(sparkParticlesLeft);
			StopParticleForPhotosensitivity(sparkParticlesRight);
			StopParticleForPhotosensitivity(stateChangeParticles);
			StopParticleForPhotosensitivity(destroyParticles);
			StopParticleForPhotosensitivity(damageSmoke);
			StopParticleForPhotosensitivity(damageFire);
			StopParticleForPhotosensitivity(valuableBoxBreakBuildupParticle);
			StopParticleForPhotosensitivity(valuableBoxComebackParticle);
			StopParticleForPhotosensitivity(vehicleFixParticles);
		}
	}

	private void StopParticleForPhotosensitivity(ParticleSystem _particleSystem)
	{
		if (Object.op_Implicit((Object)(object)_particleSystem) && _particleSystem.isPlaying)
		{
			_particleSystem.Stop(true, (ParticleSystemStopBehavior)0);
		}
	}

	private void TumbleEffects()
	{
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		bool flag = currentState == State.Tumbled;
		tumbleSound.PlayLoop(flag, 2f, 2f, 1f, soundEffectVolume);
		tumbleSound.Source.dopplerLevel = (localPlayerIsMounted ? 0f : tumbleSound.Doppler);
		if (Object.op_Implicit((Object)(object)tumbleParticles))
		{
			if (flag && !tumbleParticles.isPlaying)
			{
				tumbleParticles.Play(true);
			}
			else if (!flag && tumbleParticles.isPlaying)
			{
				tumbleParticles.Stop(true);
			}
		}
		if (flag)
		{
			tumbleBlinkTimer -= Time.deltaTime;
			if (!(tumbleBlinkTimer <= 0f))
			{
				return;
			}
			tumbleBlinkRed = !tumbleBlinkRed;
			if (tumbleBlinkRed)
			{
				tumbleBlinkTimer = 0.25f;
				tumbleWarningBeep.Play(((Component)this).transform, soundEffectVolume);
				if (!Object.op_Implicit((Object)(object)ArenaRace.instance) && Object.op_Implicit((Object)(object)grabMaterial))
				{
					grabMaterial.SetColor(emissionColorID, Color.red);
				}
			}
			else
			{
				tumbleBlinkTimer = 0.45f;
				if (!Object.op_Implicit((Object)(object)ArenaRace.instance) && Object.op_Implicit((Object)(object)grabMaterial))
				{
					grabMaterial.SetColor(emissionColorID, Color.green);
				}
			}
		}
		else
		{
			if (tumbleBlinkTimer != 0f || tumbleBlinkRed)
			{
				grabVisualStatePrevious = -1;
			}
			tumbleBlinkTimer = 0f;
			tumbleBlinkRed = false;
		}
	}

	private void UpdateGrabVisual()
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)grabMaterial))
		{
			return;
		}
		if (Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			if (grabVisualState != 2)
			{
				grabVisualState = 2;
				grabVisualStatePrevious = 2;
				grabMaterial.SetColor(emissionColorID, Color.green);
				grabMaterial.mainTextureOffset = new Vector2(-0.5f, 0f);
			}
		}
		else
		{
			if (!SemiFunc.IsMasterClientOrSingleplayer())
			{
				return;
			}
			bool flag = Object.op_Implicit((Object)(object)grabArea) && grabArea.listOfAllGrabbers.Count > 0;
			if (seats.Length != 0 && (Object)(object)seats[0].seatedPlayer != (Object)null && seats[0].reachedFirstMount)
			{
				grabVisualState = 2;
			}
			else if (flag)
			{
				grabVisualState = 1;
			}
			else
			{
				grabVisualState = 0;
			}
			if (grabVisualState != grabVisualStatePrevious)
			{
				grabVisualStatePrevious = grabVisualState;
				if (SemiFunc.IsMultiplayer())
				{
					photonView.RPC("SetGrabVisualRPC", (RpcTarget)0, new object[1] { grabVisualState });
				}
				else
				{
					SetGrabVisualRPC(grabVisualState);
				}
			}
		}
	}

	[PunRPC]
	private void SetGrabVisualRPC(int _state, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && Object.op_Implicit((Object)(object)grabMaterial))
		{
			switch (_state)
			{
			case 0:
				grabMaterial.SetColor(emissionColorID, Color.red);
				grabMaterial.mainTextureOffset = new Vector2(0f, 0f);
				break;
			case 1:
			{
				Color val = new Color(1f, 0.46f, 0f);
				grabMaterial.SetColor(emissionColorID, val);
				grabMaterial.mainTextureOffset = new Vector2(-0.5f, 0f);
				break;
			}
			default:
				grabMaterial.SetColor(emissionColorID, Color.green);
				grabMaterial.mainTextureOffset = new Vector2(-0.5f, 0f);
				break;
			}
		}
	}

	private void UpdateHoverEffect()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		if (hoverEffectTransforms != null)
		{
			float deltaTime = Time.deltaTime;
			float time = Time.time;
			for (int i = 0; i < hoverEffectTransforms.Length; i++)
			{
				float num = hoverEffectNoiseSeeds[i];
				float num2 = (Mathf.PerlinNoise(num, time * hoverEffectScrollNoiseSpeed) - 0.5f) * 2f * hoverEffectScrollNoiseStrength;
				float num3 = time + (float)i * hoverEffectScrollOffsetPerMesh + num2;
				hoverEffectMaterials[i].mainTextureOffset = num3 * hoverEffectScrollSpeed;
				float num4 = (Mathf.PerlinNoise(num + 100f, time * hoverEffectRotateNoiseSpeed) - 0.5f) * 2f * hoverEffectRotateNoiseStrength;
				float num5 = (Mathf.PerlinNoise(num + 200f, time * hoverEffectRotateNoiseSpeed) - 0.5f) * 2f * hoverEffectRotateNoiseStrength;
				float num6 = hoverEffectRotateSpeedX * (1f + num4) * deltaTime;
				float num7 = hoverEffectRotateSpeedZ * 5f * (1f + num5) * deltaTime;
				hoverEffectTransforms[i].Rotate(num6, 0f, num7, (Space)1);
				float num8 = (Mathf.PerlinNoise(num + 300f, time * hoverEffectScaleNoiseSpeed) - 0.5f) * 2f * hoverEffectScaleNoiseStrength;
				hoverEffectTransforms[i].localScale = hoverEffectOriginalScales[i] * (1f + num8);
			}
		}
	}

	private void UpdateImpactDistanceTracking()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)fakeVelocities) || (physGrabObject.grabbed && !hasAnySeatedPlayerCached))
		{
			return;
		}
		Vector3 velocity = fakeVelocities.velocity;
		velocity.y = 0f;
		float magnitude = velocity.magnitude;
		Vector3 position = ((Component)this).transform.position;
		float num = Vector3.Distance(position, lastPositionForDistanceTracking);
		lastPositionForDistanceTracking = position;
		if (magnitude >= bigImpactThreshold)
		{
			distanceTraveledAtBigSpeed += num;
			distanceTraveledAtMediumSpeed += num;
			distanceTraveledAtSmallSpeed += num;
		}
		else if (magnitude >= mediumImpactThreshold)
		{
			distanceTraveledAtBigSpeed = 0f;
			distanceTraveledAtMediumSpeed += num;
			distanceTraveledAtSmallSpeed += num;
		}
		else if (magnitude >= smallImpactThreshold)
		{
			distanceTraveledAtBigSpeed = 0f;
			distanceTraveledAtMediumSpeed = 0f;
			distanceTraveledAtSmallSpeed += num;
		}
		else
		{
			distanceTraveledAtBigSpeed = 0f;
			distanceTraveledAtMediumSpeed = 0f;
			distanceTraveledAtSmallSpeed = 0f;
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		if (goingFastParticlesCooldown > 0f)
		{
			goingFastParticlesCooldown -= Time.deltaTime;
			return;
		}
		bool flag = magnitude >= bigImpactThreshold && distanceTraveledAtBigSpeed >= bigImpactDistanceRequired;
		if (flag && !goingFastParticlesPlaying)
		{
			goingFastParticlesPlaying = true;
			goingFastParticlesCooldown = 0.5f;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("GoingFastParticlesRPC", (RpcTarget)0, new object[1] { true });
			}
			else
			{
				GoingFastParticlesRPC(_play: true);
			}
		}
		else if (!flag && goingFastParticlesPlaying)
		{
			goingFastParticlesPlaying = false;
			goingFastParticlesCooldown = 0.5f;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("GoingFastParticlesRPC", (RpcTarget)0, new object[1] { false });
			}
			else
			{
				GoingFastParticlesRPC(_play: false);
			}
		}
	}

	[PunRPC]
	private void GoingFastParticlesRPC(bool _play, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && Object.op_Implicit((Object)(object)particlesForGoingFast))
		{
			if (_play)
			{
				particlesForGoingFast.Play(true);
			}
			else
			{
				particlesForGoingFast.Stop(true);
			}
		}
	}

	private ImpactType GetAllowedImpactType(float _speed)
	{
		if (_speed >= bigImpactThreshold)
		{
			return ImpactType.Big;
		}
		if (_speed >= mediumImpactThreshold && distanceTraveledAtMediumSpeed >= mediumImpactDistanceRequired)
		{
			return ImpactType.Medium;
		}
		if (_speed >= smallImpactThreshold && distanceTraveledAtSmallSpeed >= smallImpactDistanceRequired)
		{
			return ImpactType.Small;
		}
		return ImpactType.None;
	}

	private void UpdateImpactDetection()
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)fakeVelocities) || !SemiFunc.IsMasterClientOrSingleplayer() || (physGrabObject.grabbed && !hasAnySeatedPlayerCached) || driftDirectionMismatchTimer > 0f)
		{
			return;
		}
		if (driftSwitchImpactCooldown > 0f)
		{
			driftSwitchImpactCooldown -= Time.deltaTime;
			return;
		}
		impactStartupTimer += Time.deltaTime;
		if (impactStartupTimer < 5f)
		{
			return;
		}
		if (landingImpactCooldown > 0f)
		{
			landingImpactCooldown -= Time.deltaTime;
		}
		else
		{
			if ((groundSlopeValid && Mathf.Abs(groundSlopePitchAngle) > 5f) || (lastValidSlopeTimer > 0f && lastValidSlopeAngle > 5f))
			{
				return;
			}
			lastDetectedImpact = ImpactType.None;
			if (impactCooldownTimer > 0f)
			{
				impactCooldownTimer -= Time.deltaTime;
			}
			if (impactCooldownSmall > 0f)
			{
				impactCooldownSmall -= Time.deltaTime;
			}
			if (impactCooldownMedium > 0f)
			{
				impactCooldownMedium -= Time.deltaTime;
			}
			if (impactCooldownBig > 0f)
			{
				impactCooldownBig -= Time.deltaTime;
			}
			if (!SemiFunc.PerSecond(30f, (Object)(object)this))
			{
				return;
			}
			Vector3 velocity = fakeVelocities.velocity;
			Vector3 val = velocity;
			val.y = 0f;
			float num = (maxForwardSpeed + hyperMaxSpeed) * 1.2f;
			float num2 = Mathf.Min(val.magnitude, num);
			Vector3 normalized = velocity.normalized;
			int num3 = impactHistoryIndex;
			float num4 = impactVelocityHistory[num3];
			if (impactHistorySampleCount >= impactVelocityHistory.Length && num4 > num2)
			{
				velocityBeforeImpact = impactVelocityDirectionHistory[num3] * num4;
			}
			impactVelocityHistory[impactHistoryIndex] = num2;
			impactVelocityDirectionHistory[impactHistoryIndex] = normalized;
			impactHistoryIndex = (impactHistoryIndex + 1) % impactVelocityHistory.Length;
			if (impactHistorySampleCount < impactVelocityHistory.Length)
			{
				impactHistorySampleCount++;
			}
			else
			{
				if (Object.op_Implicit((Object)(object)rb) && Mathf.Abs(rb.angularVelocity.y) > 0.5f)
				{
					return;
				}
				float num5 = num4 - num2;
				ImpactType allowedImpactType = GetAllowedImpactType(num4);
				if (allowedImpactType == ImpactType.None)
				{
					return;
				}
				float num6 = (isDrifting ? 1.5f : 1f);
				if (allowedImpactType == ImpactType.Big && num5 >= bigImpactThreshold * num6 && impactCooldownBig <= 0f)
				{
					lastDetectedImpact = ImpactType.Big;
					impactCooldownTimer = impactCooldown;
					impactCooldownBig = impactTypeCooldown;
				}
				else if (allowedImpactType >= ImpactType.Medium && num5 >= mediumImpactThreshold * num6 && impactCooldownMedium <= 0f && impactCooldownTimer <= 0f)
				{
					lastDetectedImpact = ImpactType.Medium;
					impactCooldownTimer = impactCooldown;
					impactCooldownMedium = impactTypeCooldown;
				}
				else if (num5 >= smallImpactThreshold * num6 && impactCooldownSmall <= 0f && impactCooldownTimer <= 0f)
				{
					lastDetectedImpact = ImpactType.Small;
					impactCooldownTimer = impactCooldown;
					impactCooldownSmall = impactTypeCooldown;
				}
				if (lastDetectedImpact == ImpactType.None)
				{
					return;
				}
				if (velocityBeforeImpact.sqrMagnitude < 0.01f)
				{
					velocityBeforeImpact = impactVelocityDirectionHistory[num3] * num4;
				}
				Vector3 val2 = (Object.op_Implicit((Object)(object)midpointTransform) ? midpointTransform.position : ((Component)this).transform.position);
				Vector3 normalized2 = velocityBeforeImpact.normalized;
				normalized2.y = 0f;
				normalized2.Normalize();
				bool flag = false;
				RaycastHit[] array = Physics.RaycastAll(val2, normalized2, 2f, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()));
				for (int i = 0; i < array.Length; i++)
				{
					if (!array[i].transform.IsChildOf(((Component)this).transform) && !(Vector3.Dot(array[i].normal, Vector3.up) > 0.5f))
					{
						flag = true;
						_ = ((Object)array[i].transform).name;
						break;
					}
				}
				if (!flag)
				{
					lastDetectedImpact = ImpactType.None;
					return;
				}
				PositionOfImpact();
				if (Object.op_Implicit((Object)(object)itemBattery) && !Object.op_Implicit((Object)(object)ArenaRace.instance))
				{
					float num7 = 0f;
					if (lastDetectedImpact == ImpactType.Small)
					{
						num7 = smallImpactDurabilityLoss;
					}
					else if (lastDetectedImpact == ImpactType.Medium)
					{
						num7 = mediumImpactDurabilityLoss;
					}
					else if (lastDetectedImpact == ImpactType.Big)
					{
						num7 = bigImpactDurabilityLoss;
					}
					num7 /= 1.2f;
					num7 *= durabilityLossMultiplier;
					itemBattery.batteryLife -= num7;
					if (itemBattery.batteryLife < 0f)
					{
						itemBattery.batteryLife = 0f;
					}
					if (localPlayerIsMounted)
					{
						itemBattery.TriggerSmallBatteryDeduction();
					}
				}
				distanceTraveledAtSmallSpeed = 0f;
				distanceTraveledAtMediumSpeed = 0f;
				distanceTraveledAtBigSpeed = 0f;
				impactHistorySampleCount = 0;
			}
		}
	}

	public ImpactType GetLastImpact()
	{
		return lastDetectedImpact;
	}

	public bool HasImpact()
	{
		return lastDetectedImpact != ImpactType.None;
	}

	public bool HasImpact(ImpactType _minimumType)
	{
		return lastDetectedImpact >= _minimumType;
	}

	private void PositionOfImpact()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)midpointTransform) || !Object.op_Implicit((Object)(object)impactTransform) || velocityBeforeImpact.sqrMagnitude < 0.01f)
		{
			return;
		}
		Vector3 normalized = velocityBeforeImpact.normalized;
		Vector3 val = midpointTransform.position + normalized * 2f;
		Vector3 val2 = -normalized;
		Quaternion val3 = Quaternion.LookRotation(val2, Vector3.up);
		Vector3 val4 = val3 * Vector3.right;
		Vector3 val5 = val3 * Vector3.up;
		float num = 1f;
		float num2 = 1f;
		int num3 = 4;
		int num4 = 4;
		float num5 = float.MinValue;
		Vector3 val6 = midpointTransform.position;
		bool flag = false;
		RaycastHit val9 = default;
		for (int i = 0; i < num4; i++)
		{
			for (int j = 0; j < num3; j++)
			{
				float num6 = ((float)j / (float)(num3 - 1) - 0.5f) * num;
				float num7 = ((float)i / (float)(num4 - 1) - 0.5f) * num2;
				Vector3 val7 = val + val4 * num6 + val5 * num7;
				Vector3 val8 = val2;
				if (Physics.Raycast(val7, val8, ref val9, 5f, layerMaskPhysGrabObject) && val9.transform.IsChildOf(((Component)this).transform))
				{
					float num8 = Vector3.Dot(val9.point - midpointTransform.position, normalized);
					if (num8 > num5)
					{
						num5 = num8;
						val6 = val9.point;
						flag = true;
					}
				}
			}
		}
		Vector3 val10 = (flag ? val6 : midpointTransform.position);
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("ImpactEffectRPC", (RpcTarget)0, new object[3]
			{
				(int)lastDetectedImpact,
				val10,
				normalized
			});
		}
		else
		{
			ImpactEffectRPC((int)lastDetectedImpact, val10, normalized);
		}
	}

	private void ImpactEffect(ImpactType _impactType)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (_impactType == ImpactType.None)
		{
			return;
		}
		Vector3 position = impactTransform.position;
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			float num = _impactType switch
			{
				ImpactType.Small => 18f, 
				ImpactType.Medium => 22f, 
				ImpactType.Big => 25f, 
				_ => 0f, 
			};
			if (num > 0f)
			{
				SemiFunc.EnemyInvestigate(position, num);
			}
		}
		switch (_impactType)
		{
		case ImpactType.Small:
			if (Object.op_Implicit((Object)(object)impactParticlesSmall))
			{
				impactParticlesSmall.Play(true);
			}
			impactSoundSmall.Play(position, soundEffectVolume);
			GameDirector.instance.CameraShake.ShakeDistance(1f, 3f, 8f, position, 0.1f);
			GameDirector.instance.CameraImpact.ShakeDistance(2f, 3f, 8f, position, 0.1f);
			if (hasAnySeatedPlayerCached && !AITumbleImmune && Object.op_Implicit((Object)(object)hurtColliderSmall) && !((Component)hurtColliderSmall).gameObject.activeSelf)
			{
				SetupHurtColliderIgnoreList(hurtColliderSmall);
				((Component)hurtColliderSmall).gameObject.SetActive(true);
				hurtColliderTimerSmall = 0.2f;
			}
			break;
		case ImpactType.Medium:
			if (Object.op_Implicit((Object)(object)impactParticlesMedium))
			{
				impactParticlesMedium.Play(true);
			}
			impactSoundMedium.Play(position, soundEffectVolume);
			GameDirector.instance.CameraShake.ShakeDistance(2.5f, 3f, 8f, position, 0.15f);
			GameDirector.instance.CameraImpact.ShakeDistance(5f, 3f, 8f, position, 0.15f);
			if (hasAnySeatedPlayerCached && !AITumbleImmune && Object.op_Implicit((Object)(object)hurtColliderMedium) && !((Component)hurtColliderMedium).gameObject.activeSelf)
			{
				SetupHurtColliderIgnoreList(hurtColliderMedium);
				((Component)hurtColliderMedium).gameObject.SetActive(true);
				hurtColliderTimerMedium = 0.2f;
			}
			break;
		case ImpactType.Big:
			if (Object.op_Implicit((Object)(object)impactParticlesBig))
			{
				impactParticlesBig.Play(true);
			}
			impactSoundBig.Play(position, soundEffectVolume);
			GameDirector.instance.CameraShake.ShakeDistance(5f, 3f, 8f, position, 0.2f);
			GameDirector.instance.CameraImpact.ShakeDistance(10f, 3f, 8f, position, 0.2f);
			if (hasAnySeatedPlayerCached && !AITumbleImmune && Object.op_Implicit((Object)(object)hurtColliderBig) && !((Component)hurtColliderBig).gameObject.activeSelf)
			{
				SetupHurtColliderIgnoreList(hurtColliderBig);
				((Component)hurtColliderBig).gameObject.SetActive(true);
				hurtColliderTimerBig = 0.2f;
			}
			break;
		}
		TriggerImpactShake(_impactType);
	}

	public void OnHurtColliderSmallHitEnemy()
	{
		if (enemyHitCooldownTimerSmall > 0f || !Object.op_Implicit((Object)(object)hurtColliderSmall))
		{
			return;
		}
		Enemy onImpactEnemyEnemy = hurtColliderSmall.onImpactEnemyEnemy;
		if (Object.op_Implicit((Object)(object)onImpactEnemyEnemy))
		{
			float damage = onImpactEnemyEnemy.EnemyParent.difficulty switch
			{
				EnemyParent.Difficulty.Difficulty1 => smallImpactEnemyDurabilityLossLevel1, 
				EnemyParent.Difficulty.Difficulty2 => smallImpactEnemyDurabilityLossLevel2, 
				EnemyParent.Difficulty.Difficulty3 => smallImpactEnemyDurabilityLossLevel3, 
				_ => smallImpactEnemyDurabilityLossLevel1, 
			};
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("EntityHitSoundRPC", (RpcTarget)0, new object[1] { 0 });
			}
			else
			{
				EntityHitSoundRPC(0);
			}
			ApplyEnemyHitDurabilityLoss(damage);
			enemyHitCooldownTimerSmall = smallImpactEnemyDurabilityCooldown;
		}
	}

	public void OnHurtColliderMediumHitEnemy()
	{
		if (enemyHitCooldownTimerMedium > 0f || !Object.op_Implicit((Object)(object)hurtColliderMedium))
		{
			return;
		}
		Enemy onImpactEnemyEnemy = hurtColliderMedium.onImpactEnemyEnemy;
		if (Object.op_Implicit((Object)(object)onImpactEnemyEnemy))
		{
			float damage = onImpactEnemyEnemy.EnemyParent.difficulty switch
			{
				EnemyParent.Difficulty.Difficulty1 => mediumImpactEnemyDurabilityLossLevel1, 
				EnemyParent.Difficulty.Difficulty2 => mediumImpactEnemyDurabilityLossLevel2, 
				EnemyParent.Difficulty.Difficulty3 => mediumImpactEnemyDurabilityLossLevel3, 
				_ => mediumImpactEnemyDurabilityLossLevel1, 
			};
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("EntityHitSoundRPC", (RpcTarget)0, new object[1] { 1 });
			}
			else
			{
				EntityHitSoundRPC(1);
			}
			ApplyEnemyHitDurabilityLoss(damage);
			enemyHitCooldownTimerMedium = mediumImpactEnemyDurabilityCooldown;
		}
	}

	public void OnHurtColliderBigHitEnemy()
	{
		if (enemyHitCooldownTimerBig > 0f || !Object.op_Implicit((Object)(object)hurtColliderBig))
		{
			return;
		}
		Enemy onImpactEnemyEnemy = hurtColliderBig.onImpactEnemyEnemy;
		if (Object.op_Implicit((Object)(object)onImpactEnemyEnemy))
		{
			float damage = onImpactEnemyEnemy.EnemyParent.difficulty switch
			{
				EnemyParent.Difficulty.Difficulty1 => bigImpactEnemyDurabilityLossLevel1, 
				EnemyParent.Difficulty.Difficulty2 => bigImpactEnemyDurabilityLossLevel2, 
				EnemyParent.Difficulty.Difficulty3 => bigImpactEnemyDurabilityLossLevel3, 
				_ => bigImpactEnemyDurabilityLossLevel1, 
			};
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("EntityHitSoundRPC", (RpcTarget)0, new object[1] { 2 });
			}
			else
			{
				EntityHitSoundRPC(2);
			}
			ApplyEnemyHitDurabilityLoss(damage);
			enemyHitCooldownTimerBig = bigImpactEnemyDurabilityCooldown;
		}
	}

	[PunRPC]
	private void EntityHitSoundRPC(int _size)
	{
		switch (_size)
		{
		case 0:
			entityHitSoundSmall.Play(((Component)this).transform, soundEffectVolume);
			break;
		case 1:
			entityHitSoundMedium.Play(((Component)this).transform, soundEffectVolume);
			break;
		case 2:
			entityHitSoundBig.Play(((Component)this).transform, soundEffectVolume);
			entityHitSoundBigGlobal.Play(((Component)this).transform, soundEffectVolume);
			break;
		}
	}

	private void ApplyEnemyHitDurabilityLoss(float damage)
	{
		if (Object.op_Implicit((Object)(object)itemBattery) && !Object.op_Implicit((Object)(object)ArenaRace.instance))
		{
			damage /= 1.2f;
			damage *= durabilityLossMultiplier;
			itemBattery.batteryLife -= damage;
			if (itemBattery.batteryLife < 0f)
			{
				itemBattery.batteryLife = 0f;
			}
			if (localPlayerIsMounted)
			{
				itemBattery.TriggerSmallBatteryDeduction();
			}
		}
	}

	private void UpdateEnemyHitCooldownTimers()
	{
		if (enemyHitCooldownTimerSmall > 0f)
		{
			enemyHitCooldownTimerSmall -= Time.deltaTime;
		}
		if (enemyHitCooldownTimerMedium > 0f)
		{
			enemyHitCooldownTimerMedium -= Time.deltaTime;
		}
		if (enemyHitCooldownTimerBig > 0f)
		{
			enemyHitCooldownTimerBig -= Time.deltaTime;
		}
	}

	private void PlayerHitFeedback(HurtCollider _hurtCollider, int _size)
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = (_hurtCollider.playerKill ? PlayerAvatar.instance : _hurtCollider.onImpactPlayerAvatar);
		if (!Object.op_Implicit((Object)(object)playerAvatar))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("EntityHitSoundRPC", (RpcTarget)0, new object[1] { _size });
			}
			else
			{
				EntityHitSoundRPC(_size);
			}
		}
		else if (!SemiFunc.IsMultiplayer() || playerAvatar.photonView.IsMine)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("PlayerHitFeedbackRPC", (RpcTarget)0, new object[2]
				{
					_size,
					playerAvatar.photonView.ViewID
				});
			}
			else
			{
				PlayerHitFeedbackRPC(_size, playerAvatar.photonView.ViewID);
			}
		}
	}

	[PunRPC]
	private void PlayerHitFeedbackRPC(int _size, int _playerViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_playerViewID);
		if (Object.op_Implicit((Object)(object)playerAvatar) && SemiFunc.OwnerOnlyRPC(_info, playerAvatar.photonView) && _size >= 0 && _size <= 2)
		{
			EntityHitSoundRPC(_size);
			if (!playerAvatar.isLocal)
			{
				float num = Mathf.Lerp(1f, 2f, (float)_size * 0.5f);
				RiderCameraShake(num, num * 2f, 0.1f);
			}
		}
	}

	public void OnHurtColliderSmallHitPlayer()
	{
		PlayerHitFeedback(hurtColliderSmall, 0);
	}

	public void OnHurtColliderMediumHitPlayer()
	{
		PlayerHitFeedback(hurtColliderMedium, 1);
	}

	public void OnHurtColliderBigHitPlayer()
	{
		PlayerHitFeedback(hurtColliderBig, 2);
	}

	private void UpdateVehicleUI()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)VehicleUI.instance))
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < seats.Length; i++)
		{
			if ((Object)(object)seats[i].seatedPlayer != (Object)null && seats[i].seatedPlayer.isLocal)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			if (SemiFunc.PerSecond(5f, (Object)(object)this))
			{
				Vector3 val = (Object.op_Implicit((Object)(object)fakeVelocities) ? fakeVelocities.velocity : Vector3.zero);
				val.y = 0f;
				vehicleUIKmh = val.magnitude * 3.6f * 2f;
			}
			VehicleUI.instance.ShowSpeed(vehicleUIKmh);
			if (Object.op_Implicit((Object)(object)valuableBox) && ((Component)valuableBox).gameObject.activeSelf)
			{
				VehicleUI.instance.ShowValuableBoxValue((int)valuableBox.CurrentValue);
			}
			else
			{
				VehicleUI.instance.HideValuableBoxValue();
			}
		}
	}

	private void VehicleAddForce(Vector3 force, ForceMode mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (!float.IsNaN(force.x) && !float.IsNaN(force.y) && !float.IsNaN(force.z))
		{
			rb.AddForce(force * forceMultiplier, mode);
		}
	}

	private void VehicleAddTorque(Vector3 torque, ForceMode mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (!float.IsNaN(torque.x) && !float.IsNaN(torque.y) && !float.IsNaN(torque.z))
		{
			rb.AddTorque(torque * forceMultiplier, mode);
		}
	}

	internal void VehicleAddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		if (!float.IsNaN(force.x) && !float.IsNaN(force.y) && !float.IsNaN(force.z))
		{
			rb.AddForceAtPosition(force * forceMultiplier, position, mode);
		}
	}

	public ItemVehicle()
	{
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0388: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0752: Unknown result type (might be due to invalid IL or missing references)
		//IL_0757: Unknown result type (might be due to invalid IL or missing references)
		//IL_075d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0762: Unknown result type (might be due to invalid IL or missing references)
		//IL_096d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0972: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_09fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a01: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d45: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d4a: Unknown result type (might be due to invalid IL or missing references)
	}
}
