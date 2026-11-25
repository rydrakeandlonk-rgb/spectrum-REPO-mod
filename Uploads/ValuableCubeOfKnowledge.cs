using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class ValuableCubeOfKnowledge : MonoBehaviour
{
	public enum State
	{
		Charging,
		ResetFlicker,
		Finalizing,
		Awakened
	}

	private enum GrowthState
	{
		Idle,
		Growing,
		Settling,
		Confirming,
		Complete
	}

	private class GlyphRay
	{
		public Transform transform;

		public MeshRenderer renderer;

		public Material material;

		public KnowledgeGlyphSide side;

		public Vector3 direction;

		public Vector3 surfacePosition;

		public Vector2 size;

		public float clearLength;

		public float targetLength;

		public float length;

		public float lengthVelocity;
	}

	private enum TalkState
	{
		Waiting,
		Speaking
	}

	private class ItemZap
	{
		public LineBetweenTwoPoints beam;

		public PhysGrabObject target;

		public Vector3 position;

		public float timer;

		public float duration;

		public float seed;

		public int startFrame;
	}

	public Shader cubeShader;

	public Shader glyphRayShader;

	public Texture2D[] glyphTextures;

	public Material sparkParticlesMaterial;

	[ColorUsage(true, true)]
	public Color chargingColor = new Color(0.04f, 4f, 0.24f, 1f);

	[ColorUsage(true, true)]
	public Color activatedColor = new Color(6f, 1.6f, 0.08f, 1f);

	[ColorUsage(true, true)]
	public Color awakenedColor = new Color(10f, 2.4f, 0.08f, 1f);

	public float inactiveEmissionStrength = 0.45f;

	public float activatedEmissionStrength = 4f;

	public float awakenedEmissionStrength = 8f;

	public float glowPulseSpeed = 4f;

	public int glyphRayLayers = 8;

	public float glyphRayGap = 0.002f;

	public float activatedGlyphRayLength = 1.4f;

	[Range(0f, 1f)]
	public float activatedGlyphRayLengthMultiplier = 0.32f;

	public float awakenedGlyphRayLength = 2.5f;

	public float glyphRaySpread = 0.16f;

	[Range(0f, 1f)]
	public float activatedGlyphRaySubtleMultiplier = 0.35f;

	[ColorUsage(true, true)]
	public Color chargingGlyphRayColor = new Color(0.03f, 1f, 0.08f, 1f);

	public float activatedGlyphRayIntensity = 1.8f;

	public float awakenedGlyphRayIntensity = 3.2f;

	[Range(0f, 1f)]
	public float awakenedRestGlyphRayLengthMultiplier = 0.22f;

	public float awakenedRestGlyphRayIntensity = 0.52f;

	public float activationLiftDuration = 2f;

	public float activationProtectionTime = 3f;

	public float awakenedProtectionTime = 6f;

	public float activationLiftDistance = 2.5f;

	public float activationPositionStrength = 18f;

	public float activationPositionDamping = 7f;

	public float activationRotationStrength = 22f;

	public float activationRotationDamping = 6f;

	public float activationLateralStrengthMultiplier = 1.6f;

	public float resetDistance = 15f;

	public float resetDistanceCheckTime = 0.25f;

	public float resetFlickerTime = 1.4f;

	public int awakenedValue = 250000;

	public float floatHeight = 6f;

	public float floatStrength = 16f;

	public float floatDamping = 5f;

	public float floatTorque = 0.7f;

	public float awakenedMoveTime = 3.5f;

	public float finalSpinTime = 5f;

	public float finalPlatformBreakEndLeadTime = 0.5f;

	public float awakenedSpinSpeed = 14f;

	public float awakenedSpinStrength = 8f;

	public float awakenedSizeMultiplier = 1.5f;

	public float growthSmoothTime = 0.12f;

	public float growthSafetyDelay = 1f;

	public float growthCorrectionTime = 0.35f;

	public float awakenedLightRange = 7f;

	public float awakenedRestLightIntensity = 2.4f;

	public float finalBuildLightRange = 16f;

	public float finalBuildLightIntensity = 2f;

	public float finalLightIntensityMultiplier = 2f;

	public float finalFlashLightRange = 30f;

	public float finalFlashIntensity = 10f;

	public int awakenedParticleRate = 12;

	public int awakenedParticleLimit = 28;

	public int finalBurstParticleCount = 24;

	public float finalFlashTime = 0.5f;

	public float finalShakeStrength = 8f;

	public float finalImpactStrength = 12f;

	public float buildupShakeStartStrength = 0.15f;

	public float buildupShakeEndStrength = 3.5f;

	public float finalShakeDistanceMin = 5f;

	public float finalShakeDistanceMax = 24f;

	public float awakenedMassMultiplier = 2.5f;

	public float activationEndFlashTime = 0.22f;

	[Range(0f, 1f)]
	public float activationEndFlashStrength = 0.65f;

	public float activationEndImpactStrength = 1.5f;

	public Sound activationSound = new Sound();

	public Sound activationLayerSound = new Sound();

	public Sound activationLayer2Sound = new Sound();

	public Sound activationLayer3Sound = new Sound();

	public Sound activationLayer4Sound = new Sound();

	public Sound activationDoneSound = new Sound();

	public Sound platformBreakSound = new Sound();

	public Sound activationLiftLoop = new Sound();

	public Sound finalBuildUpLoop = new Sound();

	public Sound finalFlashSound = new Sound();

	public Sound talkLoop = new Sound();

	public Vector2 talkQuietTime = new Vector2(30f, 180f);

	public Vector2 talkActiveTime = new Vector2(5f, 30f);

	public float talkFadeTime = 2.5f;

	public float talkTorque = 1.8f;

	private TalkState talkState;

	private Random talkRandom;

	private double talkNextTime;

	private double talkStartTime;

	private float talkDuration;

	private float talkNoiseSeed;

	private float talkEnvelope;

	private float talkGain;

	private float talkBass;

	private float talkMid;

	private float talkHigh;

	private float talkPitch = 1f;

	private readonly float[] talkSpectrum = new float[256];

	private readonly float[] talkSamples = new float[128];

	private PhotonView photonView;

	private ValuableObject valuableObject;

	private PhysGrabObject physGrabObject;

	private PhysGrabObjectImpactDetector impactDetector;

	private Rigidbody rb;

	private MeshRenderer cubeRenderer;

	private Material cubeMaterial;

	private Light awakenedLight;

	private ParticleSystem awakenedParticles;

	private Particle[] awakenedParticleBuffer;

	private Mesh glyphRayMesh;

	private static readonly List<ValuableCubeOfKnowledge> instances = new List<ValuableCubeOfKnowledge>();

	private readonly KnowledgePlatform[] boundPlatforms = new KnowledgePlatform[6];

	private readonly Vector3[] boundPlatformPositions = new Vector3[6];

	private readonly double[] sideActivationTimes = new double[6];

	private double resetEndTime;

	private double protectionEndTime;

	private readonly List<GlyphRay> glyphRays = new List<GlyphRay>();

	private int glyphRayCheckIndex;

	private readonly List<ParticleSystem> burstParticles = new List<ParticleSystem>();

	private const int ParticleLimit = 60;

	private const int VortexParticleLimit = 28;

	private State currentState;

	private bool stateStart = true;

	private int activatedSideMask;

	private float activationVisualTimer;

	private float activationDoneTimer;

	private float activationEndFlashTimer;

	private float activationPitch = 1f;

	private float finalSequenceTimer;

	private float finalFlashTimer;

	private KnowledgeGlyphSide recentActivatedSide;

	private int pendingAwakenedValue;

	private float targetFloatHeight;

	private Vector3 awakenedStartPosition;

	private Vector3 awakenedCenterPosition;

	private float glowPulse;

	private float destroyedTimer;

	private float platformBindTimer;

	private float platformAlignmentTimer;

	private float resetDistanceCheckTimer;

	private float resetTimer;

	private float platformAlignmentStartHeight;

	private Vector3 platformAlignmentPosition;

	private Vector3 platformAlignmentUp;

	private Quaternion platformAlignmentRotation;

	private Vector3 platformAlignmentVisualCenterLocal;

	private Vector3 previousAwakenedParticleCenter;

	private bool awakenedParticleCenterInitialized;

	public LineBetweenTwoPoints itemRejectBeamPrefab;

	public ParticleSystem itemRejectSparks;

	public ParticleSystem itemRejectImpact;

	public ParticleSystem itemRejectLightning;

	public ParticleSystem itemRejectFlash;

	public Sound itemRejectSound = new Sound();

	public float itemRejectSpeed = 10f;

	public float itemRejectUpSpeed = 5f;

	public float itemRejectZapTime = 0.3f;

	private Collider[] itemRejectColliders;

	private readonly List<PhysGrabObject> rejectedItems = new List<PhysGrabObject>();

	private readonly List<ItemZap> itemZapBeams = new List<ItemZap>();

	private float itemRejectGlowTimer;

	private float itemRejectGlow;

	private GrowthState growthState;

	private Vector3 growthStartScale;

	private Vector3 growthEndScale;

	private Vector3 growthVelocity;

	private Vector3 growthCorrectionStartScale;

	private double growthStartTime;

	private double growthSafetyTime;

	private float growthDuration;

	private float growthCorrectionTimer;

	[RuntimeInitializeOnLoadMethod(/*Could not decode attribute arguments.*/)]
	private static void RuntimeInitializeOnLoad()
	{
		instances.Clear();
	}

	private void Awake()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected Obj, but got Unknown
		photonView = ((Component)this).GetComponent<PhotonView>();
		valuableObject = ((Component)this).GetComponent<ValuableObject>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		impactDetector = ((Component)this).GetComponent<PhysGrabObjectImpactDetector>();
		rb = ((Component)this).GetComponent<Rigidbody>();
		cubeRenderer = ((Component)this).GetComponentInChildren<MeshRenderer>();
		itemRejectColliders = ((Component)this).GetComponentsInChildren<Collider>();
		CubeMaterialSetup();
		GlyphRaysSetup();
		if (Object.op_Implicit((Object)(object)impactDetector))
		{
			impactDetector.onDestroy.AddListener((UnityAction)Destroyed);
		}
	}

	private void OnEnable()
	{
		if (!instances.Contains(this))
		{
			instances.Add(this);
		}
	}

	private void Update()
	{
		if (destroyedTimer > 0f || (Object.op_Implicit((Object)(object)physGrabObject) && physGrabObject.dead))
		{
			AudioCleanup();
			return;
		}
		ItemZapTick();
		StateMachine();
		ProtectionTick();
		ActivationHoldLogic();
		GrowthLogic();
		PlatformBindingsTick();
		DistanceResetLogic();
		activationEndFlashTimer = ((currentState == State.Charging) ? Mathf.Max(0f, activationEndFlashTimer - Time.deltaTime) : 0f);
		TalkLogic();
		GlowLogic();
		FinalBuildShakeLogic();
		AudioLogic();
	}

	private void FixedUpdate()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb) && Object.op_Implicit((Object)(object)physGrabObject) && !physGrabObject.dead && !(destroyedTimer > 0f))
		{
			ItemRejectPhysics();
			TalkPhysics();
			if (currentState == State.Finalizing)
			{
				AwakenedFloatFixedUpdate();
			}
			else
			{
				PlatformAlignmentFixedUpdate();
			}
		}
	}

	private void OnDisable()
	{
		instances.Remove(this);
		ItemZapCleanup();
		rejectedItems.Clear();
		AudioCleanup();
	}

	private void OnDestroy()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected Obj, but got Unknown
		instances.Remove(this);
		if (Object.op_Implicit((Object)(object)impactDetector))
		{
			impactDetector.onDestroy.RemoveListener((UnityAction)Destroyed);
		}
		Destroyed();
		foreach (GlyphRay glyphRay in glyphRays)
		{
			if (Object.op_Implicit((Object)(object)glyphRay.material))
			{
				Object.Destroy((Object)(object)glyphRay.material);
			}
		}
		if (Object.op_Implicit((Object)(object)cubeMaterial))
		{
			Object.Destroy((Object)(object)cubeMaterial);
		}
		if (Object.op_Implicit((Object)(object)glyphRayMesh))
		{
			Object.Destroy((Object)(object)glyphRayMesh);
		}
		AudioCleanup();
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Charging:
			StateCharging();
			break;
		case State.ResetFlicker:
			StateResetFlicker();
			break;
		case State.Finalizing:
			StateFinalizing();
			break;
		case State.Awakened:
			StateAwakened();
			break;
		}
	}

	private void StateCharging()
	{
		if (stateStart)
		{
			stateStart = false;
		}
	}

	private void StateFinalizing()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			finalSequenceTimer = 0f;
			activationVisualTimer = 0f;
			platformAlignmentTimer = 0f;
			awakenedStartPosition = (Object.op_Implicit((Object)(object)rb) ? rb.position : ((Component)this).transform.position);
			awakenedCenterPosition = Vector3.zero;
			int num = 0;
			for (int i = 0; i < boundPlatformPositions.Length; i++)
			{
				awakenedCenterPosition += boundPlatformPositions[i];
				num++;
			}
			if (num > 0)
			{
				awakenedCenterPosition /= (float)num;
			}
			else
			{
				awakenedCenterPosition = awakenedStartPosition;
			}
			targetFloatHeight = awakenedCenterPosition.y + floatHeight;
			AwakenedVisualSetup();
			for (int j = 0; j < boundPlatforms.Length; j++)
			{
				if (Object.op_Implicit((Object)(object)boundPlatforms[j]))
				{
					boundPlatforms[j].BreakBegin(this, PlatformBreakDelayGet(j));
				}
			}
		}
		finalSequenceTimer = Mathf.Max(0f, (float)(GrowthTimeGet() - growthStartTime));
		float num2 = awakenedMoveTime + finalSpinTime;
		if (!(finalSequenceTimer < num2) && SemiFunc.IsMasterClientOrSingleplayer())
		{
			FinalBurst();
		}
	}

	private void StateAwakened()
	{
		if (stateStart)
		{
			stateStart = false;
			TalkScheduleBegin();
		}
	}

	private void StateResetFlicker()
	{
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			resetTimer = Mathf.Max(0f, (float)(resetEndTime - GrowthTimeGet()));
			platformAlignmentTimer = 0f;
			activationVisualTimer = 0f;
			for (int i = 0; i < boundPlatforms.Length; i++)
			{
				if (Object.op_Implicit((Object)(object)boundPlatforms[i]))
				{
					boundPlatforms[i].ResetBegin(this, resetTimer);
				}
			}
		}
		resetTimer = Mathf.Max(0f, (float)(resetEndTime - GrowthTimeGet()));
		if (!(resetTimer > 0f))
		{
			activatedSideMask = 0;
			for (int j = 0; j < boundPlatforms.Length; j++)
			{
				boundPlatforms[j] = null;
				boundPlatformPositions[j] = Vector3.zero;
			}
			StateSet(State.Charging);
		}
	}

	private void StateSet(State _state)
	{
		if (currentState != _state)
		{
			currentState = _state;
			stateStart = true;
		}
	}

	public bool SideActivate(KnowledgeGlyphSide _side, KnowledgePlatform _platform)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return false;
		}
		if (!Object.op_Implicit((Object)(object)_platform) || !Object.op_Implicit((Object)(object)valuableObject) || !valuableObject.dollarValueSet)
		{
			return false;
		}
		if (!CanConnectGet(_side))
		{
			return false;
		}
		Vector3 val = _platform.AlignmentPositionGet();
		if (SemiFunc.IsMultiplayer())
		{
			if (!Object.op_Implicit((Object)(object)photonView) || photonView.ViewID == 0)
			{
				return false;
			}
			photonView.RPC("SideActivateRPC", (RpcTarget)3, new object[4]
			{
				(int)_side,
				val,
				((Component)_platform).transform.rotation,
				GrowthTimeGet()
			});
		}
		else
		{
			SideActivateApply(_side, val, ((Component)_platform).transform.rotation, GrowthTimeGet());
		}
		return true;
	}

	[PunRPC]
	private void SideActivateRPC(int _side, Vector3 _platformPosition, Quaternion _platformRotation, double _startTime, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info) && _side >= 0 && _side < 6)
		{
			SideActivateApply((KnowledgeGlyphSide)_side, _platformPosition, _platformRotation, _startTime);
		}
	}

	private void SideActivateApply(KnowledgeGlyphSide _side, Vector3 _platformPosition, Quaternion _platformRotation, double _startTime)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.ResetFlicker)
		{
			StateResetFlicker();
		}
		if (currentState != State.Charging || SideActivatedGet(_side))
		{
			return;
		}
		activatedSideMask |= 1 << (int)_side;
		recentActivatedSide = _side;
		sideActivationTimes[(int)_side] = _startTime;
		float num = Mathf.Max(0f, (float)(GrowthTimeGet() - _startTime));
		activationVisualTimer = Mathf.Max(0f, activationLiftDuration - num);
		protectionEndTime = _startTime + (double)activationLiftDuration + 0.20000000298023224 + (double)activationProtectionTime;
		boundPlatformPositions[(int)_side] = _platformPosition;
		PlatformBind((int)_side);
		if (activationVisualTimer > 0f)
		{
			physGrabObject.OverrideGrabDisable(0.2f);
			ActivationGrabsRelease();
			PlatformAlignmentBegin(_side, _platformPosition, _platformRotation);
		}
		if (num < activationLiftDuration + 0.2f)
		{
			ActivationSoundPlay(_platformPosition);
			activationDoneTimer = Mathf.Max(0f, activationDoneTimer - num);
		}
		if (activatedSideMask == 63 && SemiFunc.IsMasterClientOrSingleplayer())
		{
			int num2 = awakenedValue;
			Vector3 localScale = ((Component)this).transform.localScale;
			Vector3 val = localScale * awakenedSizeMultiplier;
			double num3 = GrowthTimeGet();
			float num4 = Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime);
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("AwakenRPC", (RpcTarget)3, new object[5] { num2, localScale, val, num3, num4 });
			}
			else
			{
				AwakenApply(num2, localScale, val, num3, num4);
			}
		}
	}

	[PunRPC]
	private void AwakenRPC(int _value, Vector3 _startScale, Vector3 _endScale, double _startTime, float _duration, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			AwakenApply(_value, _startScale, _endScale, _startTime, _duration);
		}
	}

	private void AwakenApply(int _value, Vector3 _startScale, Vector3 _endScale, double _startTime, float _duration)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Finalizing && currentState != State.Awakened)
		{
			pendingAwakenedValue = _value;
			GrowthBeginApply(_startScale, _endScale, _startTime, _duration);
			StateSet(State.Finalizing);
			StateFinalizing();
		}
	}

	private void FinalBurst()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState == State.Finalizing && Object.op_Implicit((Object)(object)valuableObject) && Object.op_Implicit((Object)(object)physGrabObject) && !physGrabObject.dead)
		{
			int num = pendingAwakenedValue;
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("FinalBurstRPC", (RpcTarget)4, new object[2] { num, growthEndScale });
				FinalBurstApply(num, growthEndScale);
			}
			else
			{
				FinalBurstApply(num, growthEndScale);
			}
		}
	}

	[PunRPC]
	private void FinalBurstRPC(int _value, Vector3 _endScale, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			FinalBurstApply(_value, _endScale);
		}
	}

	private void FinalBurstApply(int _value, Vector3 _endScale)
	{
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Finalizing || !Object.op_Implicit((Object)(object)valuableObject) || !Object.op_Implicit((Object)(object)physGrabObject) || physGrabObject.dead)
		{
			return;
		}
		int num = Mathf.Max(0, _value - Mathf.RoundToInt(valuableObject.dollarValueOriginal));
		valuableObject.dollarValueOriginal = _value;
		valuableObject.dollarValueCurrent = _value;
		valuableObject.dollarValueSet = true;
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)RoundDirector.instance))
		{
			RoundDirector.instance.haulGoalMax += num;
		}
		Bounds bounds;
		if (Object.op_Implicit((Object)(object)WorldSpaceUIParent.instance))
		{
			WorldSpaceUIParent instance = WorldSpaceUIParent.instance;
			Vector3 worldPosition;
			if (!Object.op_Implicit((Object)(object)cubeRenderer))
			{
				worldPosition = ((Component)this).transform.position;
			}
			else
			{
				bounds = ((Renderer)cubeRenderer).bounds;
				worldPosition = bounds.center;
			}
			instance.ValueLostCreate(worldPosition, _value, WorldSpaceUIValueLost.Style.ValueSet, 5f);
		}
		GrowthEndApply(_endScale);
		protectionEndTime = GrowthTimeGet() + (double)awakenedProtectionTime;
		ProtectionTick();
		finalFlashTimer = finalFlashTime;
		FinalBurstCreate();
		AwakenedMassApply();
		Vector3 val;
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			val = ((Component)this).transform.position;
		}
		else
		{
			bounds = ((Renderer)cubeRenderer).bounds;
			val = bounds.center;
		}
		Vector3 position = val;
		FinalFlashShake(position);
		SoundPitchPlay(activationLiftLoop, position, activationPitch);
		if (finalFlashSound != null && finalFlashSound.Sounds != null && finalFlashSound.Sounds.Length != 0)
		{
			finalFlashSound.Play(position);
		}
		StateSet(State.Awakened);
		ItemRejectExisting();
	}

	public bool SideActivatedGet(KnowledgeGlyphSide _side)
	{
		return (activatedSideMask & (1 << (int)_side)) != 0;
	}

	private void PlatformBindingsTick()
	{
		if (currentState != State.Charging && currentState != State.Finalizing && currentState != State.Awakened)
		{
			return;
		}
		platformBindTimer -= Time.deltaTime;
		if (platformBindTimer > 0f)
		{
			return;
		}
		platformBindTimer = 0.25f;
		for (int i = 0; i < boundPlatforms.Length; i++)
		{
			if (!Object.op_Implicit((Object)(object)boundPlatforms[i]) && SideActivatedGet((KnowledgeGlyphSide)i))
			{
				PlatformBind(i);
			}
		}
	}

	private void PlatformBind(int _side)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (_side < 0 || _side >= boundPlatforms.Length || Object.op_Implicit((Object)(object)boundPlatforms[_side]))
		{
			return;
		}
		KnowledgePlatform knowledgePlatform = KnowledgePlatform.ClosestGet((KnowledgeGlyphSide)_side, boundPlatformPositions[_side]);
		if (Object.op_Implicit((Object)(object)knowledgePlatform))
		{
			boundPlatforms[_side] = knowledgePlatform;
			float visualTime = Mathf.Max(0f, activationLiftDuration - (float)(GrowthTimeGet() - sideActivationTimes[_side]));
			knowledgePlatform.ActivationApply(this, visualTime);
			if (currentState == State.Finalizing)
			{
				knowledgePlatform.BreakBegin(this, PlatformBreakDelayGet(_side));
			}
			if (currentState == State.Awakened)
			{
				knowledgePlatform.BreakBegin(this);
			}
		}
	}

	public static bool AnyCanConnectGet(KnowledgeGlyphSide _side)
	{
		foreach (ValuableCubeOfKnowledge instance in instances)
		{
			if (Object.op_Implicit((Object)(object)instance) && instance.CanConnectGet(_side))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanConnectGet(KnowledgeGlyphSide _side)
	{
		if (((Behaviour)this).isActiveAndEnabled && destroyedTimer <= 0f && currentState == State.Charging && activationDoneTimer <= 0f && !SideActivatedGet(_side) && Object.op_Implicit((Object)(object)valuableObject) && valuableObject.dollarValueSet && Object.op_Implicit((Object)(object)physGrabObject))
		{
			return !physGrabObject.dead;
		}
		return false;
	}

	public void ConnectionImpactApply(Vector3 _impulse)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)rb))
		{
			rb.AddForce(_impulse, (ForceMode)1);
		}
	}

	private void DistanceResetLogic()
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || currentState != State.Charging || activatedSideMask == 0 || activatedSideMask == 63)
		{
			return;
		}
		resetDistanceCheckTimer -= Time.deltaTime;
		if (resetDistanceCheckTimer > 0f)
		{
			return;
		}
		resetDistanceCheckTimer = resetDistanceCheckTime;
		if (!KnowledgePlatform.AnyWithinDistanceGet(((Component)this).transform.position, resetDistance))
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("DormantResetRPC", (RpcTarget)3, new object[1] { GrowthTimeGet() });
			}
			else
			{
				DormantResetApply(GrowthTimeGet());
			}
		}
	}

	[PunRPC]
	private void DormantResetRPC(double _startTime, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			DormantResetApply(_startTime);
		}
	}

	private void DormantResetApply(double _startTime)
	{
		if (currentState == State.Charging && activatedSideMask != 0 && activatedSideMask != 63)
		{
			resetEndTime = _startTime + (double)resetFlickerTime;
			activationDoneTimer = 0f;
			StateSet(State.ResetFlicker);
			StateResetFlicker();
		}
	}

	public Vector3 SideWorldNormalGet(KnowledgeGlyphSide _side)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = KnowledgeGlyphSideUtility.LocalDirectionGet(_side);
		Vector3 val2;
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			val2 = ((Component)this).transform.TransformDirection(val);
			return val2.normalized;
		}
		val2 = ((Component)cubeRenderer).transform.TransformDirection(val);
		return val2.normalized;
	}

	public Vector3 SideWorldPositionGet(KnowledgeGlyphSide _side)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			return ((Component)this).transform.position;
		}
		Vector3 val = KnowledgeGlyphSideUtility.LocalDirectionGet(_side);
		Bounds localBounds = ((Renderer)cubeRenderer).localBounds;
		float num = Mathf.Abs(val.x) * localBounds.extents.x + Mathf.Abs(val.y) * localBounds.extents.y + Mathf.Abs(val.z) * localBounds.extents.z;
		Vector3 val2 = localBounds.center + val * num;
		return ((Component)cubeRenderer).transform.TransformPoint(val2);
	}

	private void CubeMaterialSetup()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected Obj, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			return;
		}
		Shader val = (Object.op_Implicit((Object)(object)cubeShader) ? cubeShader : Shader.Find("Semiwork/Cube of Knowledge Activated"));
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		Material sharedMaterial = ((Renderer)cubeRenderer).sharedMaterial;
		cubeMaterial = new Material(val);
		if (Object.op_Implicit((Object)(object)sharedMaterial))
		{
			if (sharedMaterial.HasProperty("_MainTex"))
			{
				cubeMaterial.SetTexture("_MainTex", sharedMaterial.GetTexture("_MainTex"));
			}
			if (sharedMaterial.HasProperty("_EmissionMap"))
			{
				cubeMaterial.SetTexture("_EmissionMap", sharedMaterial.GetTexture("_EmissionMap"));
			}
			if (sharedMaterial.HasProperty("_Color"))
			{
				cubeMaterial.SetColor("_Color", sharedMaterial.GetColor("_Color"));
			}
			if (sharedMaterial.HasProperty("_EmissionColor"))
			{
				cubeMaterial.SetColor("_EmissionColor", sharedMaterial.GetColor("_EmissionColor"));
			}
			if (sharedMaterial.HasProperty("_Glossiness"))
			{
				cubeMaterial.SetFloat("_Glossiness", sharedMaterial.GetFloat("_Glossiness"));
			}
		}
		((Renderer)cubeRenderer).sharedMaterial = cubeMaterial;
	}

	private void FinalBuildShakeLogic()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Finalizing && Object.op_Implicit((Object)(object)GameDirector.instance))
		{
			float num = Mathf.Clamp01((float)(GrowthTimeGet() - growthStartTime) / Mathf.Max(0.01f, growthDuration));
			float strength = Mathf.Lerp(buildupShakeStartStrength, buildupShakeEndStrength, num * num);
			Vector3 position;
			if (!Object.op_Implicit((Object)(object)cubeRenderer))
			{
				position = ((Component)this).transform.position;
			}
			else
			{
				Bounds bounds = ((Renderer)cubeRenderer).bounds;
				position = bounds.center;
			}
			SemiFunc.CameraShakeDistance(position, strength, 0.1f, finalShakeDistanceMin, finalShakeDistanceMax);
		}
	}

	private void FinalFlashShake(Vector3 _position)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)GameDirector.instance))
		{
			SemiFunc.CameraShakeDistance(_position, finalShakeStrength, 0.5f, finalShakeDistanceMin, finalShakeDistanceMax);
			SemiFunc.CameraShakeImpactDistance(_position, finalImpactStrength, 0.12f, finalShakeDistanceMin, finalShakeDistanceMax);
		}
	}

	private void GlowLogic()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0369: Unknown result type (might be due to invalid IL or missing references)
		//IL_037d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Unknown result type (might be due to invalid IL or missing references)
		itemRejectGlow = Mathf.SmoothStep(0f, 1f, itemRejectGlowTimer / 0.3f);
		glowPulse = (SemiFunc.Photosensitivity() ? 0.5f : (Mathf.Sin(Time.time * glowPulseSpeed) * 0.5f + 0.5f));
		if (Object.op_Implicit((Object)(object)cubeMaterial))
		{
			float num = ((currentState == State.Finalizing || currentState == State.Awakened) ? 1f : 0f);
			Vector4 val = new Vector4(SideGlowGet(KnowledgeGlyphSide.Right), SideGlowGet(KnowledgeGlyphSide.Left), SideGlowGet(KnowledgeGlyphSide.Top), SideGlowGet(KnowledgeGlyphSide.Bottom));
			Vector4 val2 = new Vector4(SideGlowGet(KnowledgeGlyphSide.Front), SideGlowGet(KnowledgeGlyphSide.Back), glowPulse, num);
			Vector4 val3 = new Vector4(SideHotGet(KnowledgeGlyphSide.Right), SideHotGet(KnowledgeGlyphSide.Left), SideHotGet(KnowledgeGlyphSide.Top), SideHotGet(KnowledgeGlyphSide.Bottom));
			Vector4 val4 = new Vector4(SideHotGet(KnowledgeGlyphSide.Front), SideHotGet(KnowledgeGlyphSide.Back), 0f, 0f);
			cubeMaterial.SetVector("_SideActivationA", val);
			cubeMaterial.SetVector("_SideActivationB", val2);
			cubeMaterial.SetVector("_SideHotA", val3);
			cubeMaterial.SetVector("_SideHotB", val4);
			cubeMaterial.SetColor("_ActiveEmissionColor", chargingColor);
			cubeMaterial.SetColor("_AwakenedEmissionColor", awakenedColor);
			cubeMaterial.SetFloat("_InactiveEmissionStrength", inactiveEmissionStrength);
			cubeMaterial.SetFloat("_ActiveEmissionStrength", activatedEmissionStrength);
			cubeMaterial.SetFloat("_AwakenedEmissionStrength", awakenedEmissionStrength * (1f + AwakenedGlowGet(talkGain) * 0.9f + AwakenedGlowGet(talkMid) * 0.7f));
		}
		if (Object.op_Implicit((Object)(object)awakenedLight))
		{
			float num2 = Mathf.SmoothStep(0f, 1f, finalSequenceTimer / Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime));
			float num3 = (SemiFunc.Photosensitivity() ? 0f : Mathf.Clamp01(finalFlashTimer / Mathf.Max(0.01f, finalFlashTime)));
			if (num3 > 0f)
			{
				awakenedLight.range = Mathf.Lerp(awakenedLightRange, finalFlashLightRange, num3);
				awakenedLight.intensity = Mathf.Lerp(awakenedRestLightIntensity, finalFlashIntensity * finalLightIntensityMultiplier, num3);
			}
			else if (currentState == State.Finalizing)
			{
				awakenedLight.range = Mathf.Lerp(0.25f, finalBuildLightRange, num2);
				awakenedLight.intensity = finalBuildLightIntensity * finalLightIntensityMultiplier * num2;
			}
			else
			{
				awakenedLight.range = awakenedLightRange + AwakenedGlowGet(talkBass) * 3f;
				awakenedLight.intensity = awakenedRestLightIntensity * (Mathf.Lerp(0.92f, 1.03f, glowPulse) + AwakenedGlowGet(talkGain) * 2.5f + AwakenedGlowGet(talkBass) * 1.5f);
				awakenedLight.color = Color.Lerp(new Color(1f, 0.28f, 0.02f), new Color(1f, 0.65f, 0.16f), AwakenedGlowGet(talkHigh) * 0.65f);
			}
		}
		AwakenedParticlesLogic();
		GlyphRaysLogic();
		itemRejectGlowTimer = Mathf.Max(0f, itemRejectGlowTimer - Time.deltaTime);
	}

	private float AwakenedGlowGet(float _voice)
	{
		if (SemiFunc.Photosensitivity())
		{
			return 0f;
		}
		return Mathf.Max(_voice, itemRejectGlow);
	}

	public float ActivationEndFlashGet(KnowledgeGlyphSide _side)
	{
		if (SemiFunc.Photosensitivity())
		{
			return 0f;
		}
		if (currentState != State.Charging || recentActivatedSide != _side)
		{
			return 0f;
		}
		float num = Mathf.Clamp01(activationEndFlashTimer / Mathf.Max(0.01f, activationEndFlashTime));
		return num * num * activationEndFlashStrength;
	}

	private float SideHotGet(KnowledgeGlyphSide _side)
	{
		if (SemiFunc.Photosensitivity())
		{
			return 0f;
		}
		if (currentState != State.Charging || recentActivatedSide != _side)
		{
			return 0f;
		}
		if (activationVisualTimer > 0f)
		{
			return 1f;
		}
		return ActivationEndFlashGet(_side);
	}

	private float SideGlowGet(KnowledgeGlyphSide _side)
	{
		if (currentState == State.Finalizing || currentState == State.Awakened)
		{
			return 1f;
		}
		if (currentState == State.ResetFlicker)
		{
			float num = Mathf.Clamp01(resetTimer / Mathf.Max(0.01f, resetFlickerTime));
			float num2 = ((Mathf.PingPong(Time.time * 22f, 1f) > 0.46f) ? 1f : 0.08f);
			if (SemiFunc.Photosensitivity())
			{
				num2 = 1f;
			}
			if (!SideActivatedGet(_side))
			{
				return 0f;
			}
			return num * num2;
		}
		if (!SideActivatedGet(_side))
		{
			return 0f;
		}
		return 1f;
	}

	private void GlyphRaysSetup()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Expected Obj, but got Unknown
		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Expected Obj, but got Unknown
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)cubeRenderer) || glyphRayLayers <= 0 || glyphTextures == null)
		{
			return;
		}
		Shader val = (Object.op_Implicit((Object)(object)glyphRayShader) ? glyphRayShader : Shader.Find("Semiwork/Knowledge Glyph Ray"));
		if (!Object.op_Implicit((Object)(object)val))
		{
			return;
		}
		glyphRayMesh = KnowledgeGlyphRayMeshUtility.Create(glyphRaySpread);
		Bounds localBounds = ((Renderer)cubeRenderer).localBounds;
		for (int i = 0; i < 6; i++)
		{
			if (i < glyphTextures.Length && Object.op_Implicit((Object)(object)glyphTextures[i]))
			{
				KnowledgeGlyphSide knowledgeGlyphSide = (KnowledgeGlyphSide)i;
				Vector3 val2 = KnowledgeGlyphSideUtility.LocalDirectionGet(knowledgeGlyphSide);
				float num = Mathf.Abs(val2.x) * localBounds.extents.x + Mathf.Abs(val2.y) * localBounds.extents.y + Mathf.Abs(val2.z) * localBounds.extents.z;
				Vector3 val3 = localBounds.center + val2 * num;
				Vector2 val4 = GlyphRaySizeGet(val2, localBounds.size) * 0.9f;
				Rect val5 = KnowledgeGlyphSideUtility.GlyphUvRectGet(knowledgeGlyphSide);
				Vector2 size = Vector2.Scale(val4, val5.size);
				Vector2 val6 = val5.center - Vector2.one * 0.5f;
				Vector3 val7 = GlyphRayUpGet(knowledgeGlyphSide);
				Quaternion val8 = Quaternion.LookRotation(val2, val7);
				val3 += val8 * new Vector3(val6.x * val4.x, val6.y * val4.y, 0f);
				GameObject val9 = new GameObject($"Glyph Ray {knowledgeGlyphSide}");
				val9.transform.SetParent(((Component)cubeRenderer).transform, false);
				val9.transform.localRotation = val8;
				val9.AddComponent<MeshFilter>().sharedMesh = glyphRayMesh;
				MeshRenderer val10 = val9.AddComponent<MeshRenderer>();
				Material val11 = new Material(val);
				val11.SetTexture("_MainTex", (Texture)(object)glyphTextures[i]);
				val11.SetColor("_Color", chargingGlyphRayColor);
				val11.SetFloat("_Fade", 1f);
				val11.SetFloat("_Spread", glyphRaySpread);
				((Renderer)val10).sharedMaterial = val11;
				((Renderer)val10).enabled = false;
				((Renderer)val10).shadowCastingMode = (ShadowCastingMode)0;
				((Renderer)val10).receiveShadows = false;
				val11.SetTextureScale("_MainTex", val5.size);
				val11.SetTextureOffset("_MainTex", val5.position);
				((Renderer)val10).lightProbeUsage = (LightProbeUsage)0;
				((Renderer)val10).reflectionProbeUsage = (ReflectionProbeUsage)0;
				glyphRays.Add(new GlyphRay
				{
					transform = val9.transform,
					renderer = val10,
					material = val11,
					side = knowledgeGlyphSide,
					direction = val2,
					surfacePosition = val3,
					size = size
				});
			}
		}
	}

	private void GlyphRaysLogic()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		GlyphRayObstructionTick();
		float num = activatedGlyphRayIntensity * activatedGlyphRaySubtleMultiplier;
		float num2 = Mathf.Max(1f, activatedColor.maxColorComponent);
		Color val = activatedColor / num2;
		val.a = 1f;
		float num3 = ((currentState == State.Finalizing) ? Mathf.SmoothStep(0f, 1f, finalSequenceTimer / Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime)) : 0f);
		float num4 = ((currentState == State.Awakened) ? 1f : 0f);
		foreach (GlyphRay glyphRay in glyphRays)
		{
			if (!Object.op_Implicit((Object)(object)glyphRay.transform) || !Object.op_Implicit((Object)(object)glyphRay.renderer) || !Object.op_Implicit((Object)(object)glyphRay.material))
			{
				continue;
			}
			float num5 = SideGlowGet(glyphRay.side);
			if (((Renderer)glyphRay.renderer).enabled != num5 > 0f)
			{
				((Renderer)glyphRay.renderer).enabled = num5 > 0f;
			}
			if (num5 <= 0f)
			{
				continue;
			}
			float num6 = SideHotGet(glyphRay.side);
			float num7 = Mathf.Max(num6, Mathf.Max(num3, num4));
			float num8 = activatedGlyphRayLength * activatedGlyphRayLengthMultiplier;
			float num9 = num;
			num8 = Mathf.Lerp(num8, activatedGlyphRayLength, num6);
			if (num3 > 0f)
			{
				num8 = Mathf.Lerp(num8, awakenedGlyphRayLength, num3);
			}
			if (num4 > 0f)
			{
				num8 = awakenedGlyphRayLength * awakenedRestGlyphRayLengthMultiplier;
			}
			num9 = Mathf.Lerp(num9, activatedGlyphRayIntensity, num6);
			if (num3 > 0f)
			{
				num9 = Mathf.Lerp(num9, awakenedGlyphRayIntensity, num3);
			}
			if (num4 > 0f)
			{
				int num10 = (int)glyphRay.side % 3;
				float voice = talkBass;
				if (num10 == 1)
				{
					voice = talkMid;
				}
				if (num10 == 2)
				{
					voice = talkHigh;
				}
				voice = AwakenedGlowGet(voice);
				num8 += awakenedGlyphRayLength * (voice * 0.45f + AwakenedGlowGet(talkGain) * 0.12f);
				num9 = awakenedRestGlyphRayIntensity + voice * 2.2f + AwakenedGlowGet(talkGain) * 0.6f;
			}
			num9 *= Mathf.Lerp(0.92f, 1.04f, glowPulse);
			float num11 = Mathf.Min(num8, glyphRay.targetLength);
			glyphRay.length = Mathf.SmoothDamp(glyphRay.length, num11, ref glyphRay.lengthVelocity, 0.6f, float.PositiveInfinity, Time.deltaTime);
			if (glyphRay.length > glyphRay.clearLength)
			{
				glyphRay.length = glyphRay.clearLength;
				glyphRay.lengthVelocity = Mathf.Min(0f, glyphRay.lengthVelocity);
			}
			glyphRay.transform.localPosition = glyphRay.surfacePosition + glyphRay.direction * glyphRayGap;
			glyphRay.transform.localScale = new Vector3(glyphRay.size.x, glyphRay.size.y, glyphRay.length);
			glyphRay.material.SetFloat("_Intensity", num9 * num5);
			glyphRay.material.SetColor("_Color", Color.Lerp(chargingGlyphRayColor, val, num7));
		}
	}

	private void GlyphRayObstructionTick()
	{
		if (glyphRays.Count == 0 || !SemiFunc.PerSecond(30f, (Object)(object)this))
		{
			return;
		}
		for (int i = 0; i < glyphRays.Count; i++)
		{
			GlyphRay glyphRay = glyphRays[glyphRayCheckIndex];
			glyphRayCheckIndex = (glyphRayCheckIndex + 1) % glyphRays.Count;
			if (Object.op_Implicit((Object)(object)glyphRay.transform) && !(SideGlowGet(glyphRay.side) <= 0f))
			{
				GlyphRayObstructionCheck(glyphRay);
				break;
			}
		}
	}

	private void GlyphRayObstructionCheck(GlyphRay _ray)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		Transform transform = ((Component)cubeRenderer).transform;
		Vector3 val = transform.TransformVector(_ray.direction);
		float num = Mathf.Max(0.01f, val.magnitude);
		val /= num;
		Vector3 val2 = transform.TransformPoint(_ray.surfacePosition + _ray.direction * glyphRayGap);
		float num2 = Mathf.Max(activatedGlyphRayLength, awakenedGlyphRayLength * Mathf.Max(1f, awakenedRestGlyphRayLengthMultiplier + 0.57f));
		Vector3 surfacePosition = _ray.surfacePosition;
		Bounds localBounds = ((Renderer)cubeRenderer).localBounds;
		float num3 = Mathf.Abs(Vector3.Dot(transform.TransformVector(surfacePosition - localBounds.center), val)) + 0.02f;
		val2 -= val * num3;
		float num4 = Mathf.Max(0f, num2) * num + num3 + 0.08f;
		float num5 = 0.5f * (1f + Mathf.Max(0f, glyphRaySpread));
		Vector3 val3 = transform.TransformVector(_ray.transform.localRotation * Vector3.right) * (_ray.size.x * num5);
		Vector3 val4 = transform.TransformVector(_ray.transform.localRotation * Vector3.up) * (_ray.size.y * num5);
		_ray.clearLength = num2 + 0.08f / num;
		RaycastHit val6 = default;
		for (int i = 0; i < 5; i++)
		{
			Vector3 val5 = Vector3.zero;
			if (i > 0)
			{
				val5 = val3 * (((i & 1) == 0) ? (-1f) : 1f) + val4 * ((i < 3) ? (-1f) : 1f);
			}
			if (Physics.Raycast(val2 + val5, val, ref val6, num4, 1, (QueryTriggerInteraction)1))
			{
				_ray.clearLength = Mathf.Min(_ray.clearLength, Mathf.Max(0f, (val6.distance - num3) / num));
			}
		}
		_ray.targetLength = Mathf.Max(0f, _ray.clearLength - 0.08f / num);
	}

	private Vector2 GlyphRaySizeGet(Vector3 _direction, Vector3 _size)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		if (Mathf.Abs(_direction.x) > 0.5f)
		{
			return new Vector2(_size.z, _size.y);
		}
		if (Mathf.Abs(_direction.y) > 0.5f)
		{
			return new Vector2(_size.x, _size.z);
		}
		return new Vector2(_size.x, _size.y);
	}

	private Vector3 GlyphRayUpGet(KnowledgeGlyphSide _side)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return _side switch
		{
			KnowledgeGlyphSide.Top => Vector3.back, 
			KnowledgeGlyphSide.Bottom => Vector3.forward, 
			_ => Vector3.up, 
		};
	}

	private void AwakenedVisualSetup()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected Obj, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_030d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		//IL_037c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)awakenedLight))
		{
			return;
		}
		GameObject val = new GameObject("Knowledge Glow");
		val.transform.SetParent(((Component)this).transform, false);
		Transform transform = val.transform;
		Vector3 position;
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			position = ((Component)this).transform.position;
		}
		else
		{
			Bounds bounds = ((Renderer)cubeRenderer).bounds;
			position = bounds.center;
		}
		transform.position = position;
		awakenedLight = val.AddComponent<Light>();
		awakenedLight.type = (LightType)2;
		awakenedLight.color = new Color(1f, 0.28f, 0.02f);
		awakenedLight.range = 0.25f;
		awakenedLight.intensity = 0f;
		awakenedLight.shadows = (LightShadows)0;
		awakenedParticles = val.AddComponent<ParticleSystem>();
		awakenedParticles.Stop(true, (ParticleSystemStopBehavior)0);
		ParticleSystemRenderer component = ((Component)awakenedParticles).GetComponent<ParticleSystemRenderer>();
		if (Object.op_Implicit((Object)(object)component))
		{
			if (Object.op_Implicit((Object)(object)sparkParticlesMaterial))
			{
				((Renderer)component).sharedMaterial = sparkParticlesMaterial;
			}
			if (Object.op_Implicit((Object)(object)sparkParticlesMaterial))
			{
				component.trailMaterial = sparkParticlesMaterial;
			}
			component.renderMode = (ParticleSystemRenderMode)0;
		}
		MainModule main = awakenedParticles.main;
		main.loop = true;
		main.playOnAwake = false;
		main.startLifetime = new MinMaxCurve(1.2f, 2.4f);
		main.startSpeed = new MinMaxCurve(0.45f, 1.1f);
		main.startSize = new MinMaxCurve(0.0225f, 0.07f);
		main.startColor = new MinMaxGradient(new Color(1f, 0.18f, 0.01f), new Color(1f, 0.75f, 0.08f));
		main.maxParticles = Mathf.Clamp(awakenedParticleLimit, 1, 28);
		main.simulationSpace = (ParticleSystemSimulationSpace)1;
		EmissionModule emission = awakenedParticles.emission;
		emission.rateOverTime = MinMaxCurve.op_Implicit((float)awakenedParticleRate);
		ShapeModule shape = awakenedParticles.shape;
		shape.shapeType = (ParticleSystemShapeType)0;
		shape.radius = 1.5f;
		shape.radiusThickness = 0.35f;
		VelocityOverLifetimeModule velocityOverLifetime = awakenedParticles.velocityOverLifetime;
		velocityOverLifetime.enabled = true;
		velocityOverLifetime.space = (ParticleSystemSimulationSpace)1;
		velocityOverLifetime.orbitalX = new MinMaxCurve(0.3f);
		velocityOverLifetime.orbitalY = new MinMaxCurve(1.2f);
		velocityOverLifetime.radial = new MinMaxCurve(-0.18f);
		NoiseModule noise = awakenedParticles.noise;
		noise.enabled = true;
		noise.strength = MinMaxCurve.op_Implicit(0.35f);
		noise.frequency = 0.6f;
		noise.scrollSpeed = MinMaxCurve.op_Implicit(0.2f);
		CollisionModule collision = awakenedParticles.collision;
		collision.enabled = true;
		collision.type = (ParticleSystemCollisionType)1;
		collision.mode = (ParticleSystemCollisionMode)0;
		collision.quality = (ParticleSystemCollisionQuality)2;
		collision.collidesWith = LayerMask.op_Implicit(1);
		collision.bounce = new MinMaxCurve(0.35f);
		collision.dampen = new MinMaxCurve(0.12f);
		collision.lifetimeLoss = new MinMaxCurve(0f);
		collision.radiusScale = 0.25f;
		collision.maxCollisionShapes = 12;
		collision.enableDynamicColliders = false;
		ParticleSizeEndZeroApply(awakenedParticles);
		ParticleTrailsApply(awakenedParticles, _final: false);
		awakenedParticleBuffer = new Particle[main.maxParticles];
		previousAwakenedParticleCenter = ((Component)awakenedParticles).transform.position;
		awakenedParticleCenterInitialized = true;
		awakenedParticles.Play();
	}

	private void AwakenedParticlesLogic()
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)awakenedParticles))
		{
			AwakenedParticlesFollowLogic();
			float num = ((currentState == State.Finalizing) ? Mathf.Clamp01(finalSequenceTimer / Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime)) : 0f);
			EmissionModule emission = awakenedParticles.emission;
			int num2 = Mathf.Clamp(awakenedParticleRate, 1, 12);
			emission.rateOverTime = MinMaxCurve.op_Implicit((currentState == State.Finalizing) ? Mathf.Lerp((float)num2 * 0.75f, (float)num2 * 1.6f, num) : ((float)num2 * (0.5f + AwakenedGlowGet(talkHigh) * 1.5f + AwakenedGlowGet(talkGain) * 0.5f)));
			num = Mathf.Max(num, AwakenedGlowGet(talkMid) * 0.5f + AwakenedGlowGet(talkGain) * 0.25f);
			VelocityOverLifetimeModule velocityOverLifetime = awakenedParticles.velocityOverLifetime;
			velocityOverLifetime.orbitalX = new MinMaxCurve(Mathf.Lerp(0.3f, 1.15f, num));
			velocityOverLifetime.orbitalY = new MinMaxCurve(Mathf.Lerp(1.2f, 6f, num * num));
			velocityOverLifetime.orbitalZ = new MinMaxCurve(Mathf.Lerp(0.18f, 0.9f, num));
			velocityOverLifetime.radial = new MinMaxCurve(Mathf.Lerp(-0.18f, -0.9f, num));
			NoiseModule noise = awakenedParticles.noise;
			noise.strength = MinMaxCurve.op_Implicit(Mathf.Lerp(0.55f, 2f, num));
			noise.frequency = Mathf.Lerp(0.7f, 2f, num);
			noise.scrollSpeed = MinMaxCurve.op_Implicit(Mathf.Lerp(0.4f, 3f, num));
		}
	}

	private void AwakenedParticlesFollowLogic()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = ((Component)awakenedParticles).transform.position;
		if (!awakenedParticleCenterInitialized)
		{
			previousAwakenedParticleCenter = position;
			awakenedParticleCenterInitialized = true;
			return;
		}
		Vector3 val = position - previousAwakenedParticleCenter;
		previousAwakenedParticleCenter = position;
		if (!(val.sqrMagnitude <= 1E-07f) && awakenedParticleBuffer != null)
		{
			int particles = awakenedParticles.GetParticles(awakenedParticleBuffer);
			for (int i = 0; i < particles; i++)
			{
				ref Particle reference = ref awakenedParticleBuffer[i];
				reference.position += val;
			}
			awakenedParticles.SetParticles(awakenedParticleBuffer, particles);
		}
	}

	public int ParticleBurstCountGet(int _requested)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		int num = 32 - boundPlatforms.Length;
		for (int num2 = burstParticles.Count - 1; num2 >= 0; num2--)
		{
			if (!Object.op_Implicit((Object)(object)burstParticles[num2]))
			{
				burstParticles.RemoveAt(num2);
			}
			else
			{
				int num3 = num;
				MainModule main = burstParticles[num2].main;
				num = num3 - main.maxParticles;
			}
		}
		return Mathf.Clamp(_requested, 0, Mathf.Max(0, num));
	}

	public void ParticleBurstRegister(ParticleSystem _particles)
	{
		if (Object.op_Implicit((Object)(object)_particles) && !burstParticles.Contains(_particles))
		{
			burstParticles.Add(_particles);
		}
	}

	private void FinalBurstCreate()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.Photosensitivity())
		{
			return;
		}
		int num = ParticleBurstCountGet(Mathf.Clamp(finalBurstParticleCount, 1, 24));
		if (num <= 0)
		{
			return;
		}
		GameObject val = new GameObject("Knowledge Final Burst");
		Transform transform = val.transform;
		Vector3 position;
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			position = ((Component)this).transform.position;
		}
		else
		{
			Bounds bounds = ((Renderer)cubeRenderer).bounds;
			position = bounds.center;
		}
		transform.position = position;
		ParticleSystem val2 = val.AddComponent<ParticleSystem>();
		val2.Stop(true, (ParticleSystemStopBehavior)0);
		ParticleSystemRenderer component = ((Component)val2).GetComponent<ParticleSystemRenderer>();
		if (Object.op_Implicit((Object)(object)component))
		{
			if (Object.op_Implicit((Object)(object)sparkParticlesMaterial))
			{
				((Renderer)component).sharedMaterial = sparkParticlesMaterial;
			}
			if (Object.op_Implicit((Object)(object)sparkParticlesMaterial))
			{
				component.trailMaterial = sparkParticlesMaterial;
			}
			component.renderMode = (ParticleSystemRenderMode)5;
			component.enableGPUInstancing = true;
		}
		MainModule main = val2.main;
		main.loop = false;
		main.playOnAwake = false;
		main.duration = 0.01f;
		main.startLifetime = new MinMaxCurve(0.5f, 1.15f);
		main.startSpeed = new MinMaxCurve(8f, 18f);
		main.startSize = new MinMaxCurve(0.08f, 0.22f);
		main.startColor = new MinMaxGradient(new Color(1f, 0.16f, 0.01f), new Color(1f, 0.9f, 0.16f));
		main.maxParticles = num;
		main.stopAction = (ParticleSystemStopAction)2;
		main.simulationSpace = (ParticleSystemSimulationSpace)1;
		ShapeModule shape = val2.shape;
		shape.shapeType = (ParticleSystemShapeType)0;
		shape.radius = 0.32f;
		shape.radiusThickness = 1f;
		NoiseModule noise = val2.noise;
		noise.enabled = true;
		noise.strength = MinMaxCurve.op_Implicit(2.2f);
		noise.frequency = 1.3f;
		noise.scrollSpeed = MinMaxCurve.op_Implicit(4f);
		EmissionModule emission = val2.emission;
		emission.rateOverTime = MinMaxCurve.op_Implicit(0f);
		emission.rateOverDistance = MinMaxCurve.op_Implicit(0f);
		emission.SetBursts(new Burst[1]
		{
			new Burst(0f, (short)num)
		});
		ParticleSizeEndZeroApply(val2);
		ParticleTrailsApply(val2, _final: true);
		ParticleBurstRegister(val2);
		val2.Play();
	}

	private void ParticleSizeEndZeroApply(ParticleSystem _particles)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected Obj, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		SizeOverLifetimeModule sizeOverLifetime = _particles.sizeOverLifetime;
		sizeOverLifetime.enabled = true;
		sizeOverLifetime.size = new MinMaxCurve(1f, new AnimationCurve(new Keyframe[3]
		{
			new Keyframe(0f, 1f, 0f, 0f),
			new Keyframe(0.75f, 0.82f, -0.35f, -0.35f),
			new Keyframe(1f, 0f, -4f, -4f)
		}));
	}

	private void ParticleTrailsApply(ParticleSystem _particles, bool _final)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Expected Obj, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected Obj, but got Unknown
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		TrailModule trails = _particles.trails;
		trails.enabled = true;
		trails.ratio = 1f;
		trails.lifetime = new MinMaxCurve(_final ? 0.72f : 0.32f);
		trails.minVertexDistance = (_final ? 0.18f : 0.12f);
		trails.worldSpace = true;
		trails.dieWithParticles = false;
		trails.sizeAffectsWidth = true;
		trails.inheritParticleColor = false;
		trails.widthOverTrail = new MinMaxCurve(1f, new AnimationCurve(new Keyframe[3]
		{
			new Keyframe(0f, 0f, 0f, 8f),
			new Keyframe(_final ? 0.08f : 0.16f, 1f, 0f, 0f),
			new Keyframe(1f, 0f, -2f, 0f)
		}));
		Gradient val = new Gradient();
		val.SetKeys(new GradientColorKey[2]
		{
			new GradientColorKey(new Color(1f, 0.88f, 0.18f), 0f),
			new GradientColorKey(new Color(1f, 0.18f, 0.01f), 1f)
		}, new GradientAlphaKey[4]
		{
			new GradientAlphaKey(0f, 0f),
			new GradientAlphaKey(1f, 0.08f),
			new GradientAlphaKey(0.75f, 0.72f),
			new GradientAlphaKey(0f, 1f)
		});
		trails.colorOverLifetime = new MinMaxGradient(val);
		trails.colorOverTrail = new MinMaxGradient(val);
	}

	public float ActivationPitchGet()
	{
		return activationPitch;
	}

	private void ActivationSoundPlay(Vector3 _position)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		for (int i = 0; i < boundPlatforms.Length; i++)
		{
			if ((activatedSideMask & (1 << i)) != 0)
			{
				num++;
			}
		}
		activationPitch = 1f + (float)Mathf.Clamp(num - 1, 0, 5) * 0.1f;
		activationDoneTimer = Mathf.Max(0f, activationLiftDuration) + 0.2f;
		activationEndFlashTimer = 0f;
		SoundPitchPlay(activationLiftLoop, _position, activationPitch);
		SoundPitchPlay(activationSound, _position, activationPitch);
		SoundPitchPlay(activationLayerSound, _position, activationPitch);
		SoundPitchPlay(activationLayer2Sound, _position, activationPitch);
		SoundPitchPlay(activationLayer3Sound, _position, activationPitch);
		SoundPitchPlay(activationLayer4Sound, _position, activationPitch);
	}

	public void PlatformBreakSoundPlay(KnowledgeGlyphSide _side, Vector3 _position)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		SoundPitchPlay(platformBreakSound, _position, 2f + (float)Mathf.Clamp((int)_side, 0, 5) * 0.2f);
	}

	private void SoundPitchPlay(Sound _sound, Vector3 _position, float _pitch)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (_sound != null && _sound.Sounds != null && _sound.Sounds.Length != 0)
		{
			_sound.Pitch = _pitch;
			_sound.PitchRandom = 0f;
			_sound.Doppler = 0f;
			_sound.Play(_position);
		}
	}

	private void ActivationEndTick()
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Charging || !Object.op_Implicit((Object)(object)physGrabObject) || physGrabObject.dead)
		{
			activationDoneTimer = 0f;
		}
		else
		{
			if (activationDoneTimer <= 0f)
			{
				return;
			}
			activationDoneTimer = Mathf.Max(0f, activationDoneTimer - Time.deltaTime);
			if (!(activationDoneTimer > 0f))
			{
				Vector3 val;
				if (!Object.op_Implicit((Object)(object)cubeRenderer))
				{
					val = ((Component)this).transform.position;
				}
				else
				{
					Bounds bounds = ((Renderer)cubeRenderer).bounds;
					val = bounds.center;
				}
				Vector3 position = val;
				SoundPitchPlay(activationDoneSound, position, activationPitch);
				SoundPitchPlay(activationLiftLoop, position, activationPitch);
				activationEndFlashTimer = activationEndFlashTime;
				if (Object.op_Implicit((Object)(object)GameDirector.instance))
				{
					SemiFunc.CameraShakeImpactDistance(position, activationEndImpactStrength, 0.1f, 3f, 12f);
				}
			}
		}
	}

	private void AudioLogic()
	{
		ActivationEndTick();
		if (activationVisualTimer > 0f)
		{
			activationVisualTimer -= Time.deltaTime;
		}
		if (finalFlashTimer > 0f)
		{
			finalFlashTimer -= Time.deltaTime;
		}
		bool playing = currentState == State.Finalizing;
		float num = Mathf.Clamp01(finalSequenceTimer / Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime));
		SoundLoopLogic(finalBuildUpLoop, playing, 0.2f, 8f, Mathf.Lerp(1f, 2f, num));
	}

	private void SoundLoopLogic(Sound _sound, bool _playing, float _fadeInSpeed, float _fadeOutSpeed, float _pitch)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (_sound != null && _sound.Sounds != null && _sound.Sounds.Length != 0)
		{
			Vector3 val;
			if (!Object.op_Implicit((Object)(object)cubeRenderer))
			{
				val = ((Component)this).transform.position;
			}
			else
			{
				Bounds bounds = ((Renderer)cubeRenderer).bounds;
				val = bounds.center;
			}
			Vector3 position = val;
			if (!Object.op_Implicit((Object)(object)_sound.Source) & _playing)
			{
				_sound.LoopSourceCreate(position);
			}
			if (Object.op_Implicit((Object)(object)_sound.Source))
			{
				((Component)_sound.Source).transform.position = position;
				_sound.StartTimeOverride = 0f;
				_sound.PlayLoop(_playing, _fadeInSpeed, _fadeOutSpeed, _pitch);
			}
		}
	}

	private void AudioCleanup()
	{
		TalkCleanup();
		activationDoneTimer = 0f;
		activationEndFlashTimer = 0f;
		if (activationLiftLoop != null && Object.op_Implicit((Object)(object)activationLiftLoop.Source))
		{
			Object.Destroy((Object)(object)((Component)activationLiftLoop.Source).gameObject);
			activationLiftLoop.Source = null;
		}
		if (finalBuildUpLoop != null && Object.op_Implicit((Object)(object)finalBuildUpLoop.Source))
		{
			Object.Destroy((Object)(object)((Component)finalBuildUpLoop.Source).gameObject);
			finalBuildUpLoop.Source = null;
		}
	}

	private void TalkScheduleBegin()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		talkRandom = new Random((photonView.ViewID * 397) ^ growthStartTime.GetHashCode());
		talkState = TalkState.Waiting;
		talkNextTime = growthStartTime + (double)growthDuration + (double)TalkTimeGet(talkQuietTime);
	}

	private float TalkTimeGet(Vector2 _range)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Max(0.1f, _range.x);
		return Mathf.Lerp(num, Mathf.Max(num, _range.y), (float)talkRandom.NextDouble());
	}

	private void TalkLogic()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		if (currentState != State.Awakened)
		{
			return;
		}
		if (talkRandom == null)
		{
			TalkScheduleBegin();
		}
		double num = GrowthTimeGet();
		while (num >= talkNextTime)
		{
			switch (talkState)
			{
			case TalkState.Waiting:
				talkState = TalkState.Speaking;
				talkStartTime = talkNextTime;
				talkDuration = TalkTimeGet(talkActiveTime);
				talkNoiseSeed = (float)talkRandom.NextDouble() * 1000f;
				talkNextTime = talkStartTime + (double)talkDuration;
				break;
			case TalkState.Speaking:
				talkState = TalkState.Waiting;
				talkNextTime += TalkTimeGet(talkQuietTime);
				break;
			}
		}
		TalkAudioLogic(num);
		TalkSpectrumLogic();
		TalkInvestigateLogic();
	}

	private void TalkAudioLogic(double _time)
	{
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		talkEnvelope = 0f;
		bool flag = talkState == TalkState.Speaking;
		if (flag)
		{
			float num = (float)(_time - talkStartTime);
			float num2 = Mathf.Min(Mathf.Max(0.01f, talkFadeTime), talkDuration * 0.4f);
			talkEnvelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(num / num2, (talkDuration - num) / num2));
			float num3 = Mathf.PerlinNoise(num * 0.057f, talkNoiseSeed + 13f) * 2f;
			float num4 = Mathf.PerlinNoise(num * 0.17f + num3, talkNoiseSeed) * 0.5f + Mathf.PerlinNoise(num * 0.63f, talkNoiseSeed + 37f) * 0.32f + Mathf.PerlinNoise(num * 1.47f, talkNoiseSeed + 79f) * 0.18f;
			float num5 = Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01((num4 - 0.5f) * 2f + 0.5f));
			talkPitch = Mathf.Clamp(Mathf.Lerp(talkPitch, num5, 1f - Mathf.Exp(-8f * Time.deltaTime)), 0.8f, 1.2f);
		}
		if (talkLoop.Sounds != null && talkLoop.Sounds.Length != 0)
		{
			Vector3 val;
			if (!Object.op_Implicit((Object)(object)cubeRenderer))
			{
				val = ((Component)this).transform.position;
			}
			else
			{
				Bounds bounds = ((Renderer)cubeRenderer).bounds;
				val = bounds.center;
			}
			Vector3 position = val;
			if (flag && !Object.op_Implicit((Object)(object)talkLoop.Source))
			{
				talkLoop.LoopSourceCreate(position);
			}
			if (Object.op_Implicit((Object)(object)talkLoop.Source))
			{
				((Component)talkLoop.Source).transform.position = position;
				talkLoop.PlayLoop(flag, 10f, 10f, talkPitch, talkEnvelope);
				talkLoop.Source.pitch = talkPitch;
			}
		}
	}

	private void TalkSpectrumLogic()
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		if (talkEnvelope > 0f && Object.op_Implicit((Object)(object)talkLoop.Source) && talkLoop.Source.isPlaying)
		{
			talkLoop.Source.GetOutputData(talkSamples, 0);
			talkLoop.Source.GetSpectrumData(talkSpectrum, 0, (FFTWindow)5);
			float[] array = talkSamples;
			foreach (float num5 in array)
			{
				num += num5 * num5;
			}
			num = Mathf.Clamp01(Mathf.Sqrt(num / (float)talkSamples.Length) * 12f) * talkEnvelope;
			for (int j = 1; j < talkSpectrum.Length; j++)
			{
				if (j < 6)
				{
					num2 += talkSpectrum[j];
				}
				else if (j < 32)
				{
					num3 += talkSpectrum[j];
				}
				else
				{
					num4 += talkSpectrum[j];
				}
			}
			num2 = Mathf.Clamp01(Mathf.Sqrt(num2) * 4f) * talkEnvelope;
			num3 = Mathf.Clamp01(Mathf.Sqrt(num3) * 4f) * talkEnvelope;
			num4 = Mathf.Clamp01(Mathf.Sqrt(num4) * 6f) * talkEnvelope;
		}
		float num6 = 1f - Mathf.Exp(-18f * Time.deltaTime);
		talkGain = Mathf.Lerp(talkGain, num, num6);
		talkBass = Mathf.Lerp(talkBass, num2, num6);
		talkMid = Mathf.Lerp(talkMid, num3, num6);
		talkHigh = Mathf.Lerp(talkHigh, num4, num6);
	}

	private void TalkInvestigateLogic()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (talkState == TalkState.Speaking && SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)EnemyDirector.instance) && SemiFunc.PerSecond(1f, (Object)(object)this))
		{
			SemiFunc.EnemyInvestigate(physGrabObject.centerPoint, 90f);
		}
	}

	private void TalkPhysics()
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Awakened && talkState == TalkState.Speaking && !(talkGain <= 0.001f) && !rb.isKinematic)
		{
			float num = (float)(GrowthTimeGet() - talkStartTime);
			Vector3 val = new Vector3(Mathf.PerlinNoise(num * 0.86f, talkNoiseSeed + 101f) * 2f - 1f, Mathf.PerlinNoise(num * 0.74f, talkNoiseSeed + 149f) * 2f - 1f, Mathf.PerlinNoise(num * 1.06f, talkNoiseSeed + 197f) * 2f - 1f);
			float num2 = talkTorque * talkGain;
			Vector3 val2 = (val.normalized * (num2 * 0.6f) - rb.angularVelocity) * Mathf.Min(80f, 1f / Time.fixedDeltaTime);
			rb.AddTorque(Vector3.ClampMagnitude(val2, num2 * 40f), (ForceMode)5);
		}
	}

	private void TalkCleanup()
	{
		talkEnvelope = 0f;
		talkGain = 0f;
		talkBass = 0f;
		talkMid = 0f;
		talkHigh = 0f;
		if (Object.op_Implicit((Object)(object)talkLoop.Source))
		{
			Object.Destroy((Object)(object)((Component)talkLoop.Source).gameObject);
			talkLoop.Source = null;
		}
	}

	private double GrowthTimeGet()
	{
		if (!SemiFunc.IsMultiplayer())
		{
			return Time.timeAsDouble;
		}
		return PhotonNetwork.Time;
	}

	private void GrowthBeginApply(Vector3 _startScale, Vector3 _endScale, double _startTime, float _duration)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		growthStartScale = _startScale;
		growthEndScale = _endScale;
		growthStartTime = _startTime;
		growthDuration = Mathf.Max(0.01f, _duration);
		growthVelocity = Vector3.zero;
		growthState = GrowthState.Growing;
	}

	private void GrowthEndApply(Vector3 _endScale)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)impactDetector))
		{
			impactDetector.impactAudioPitchMultiplier = 0.65f;
		}
		growthEndScale = _endScale;
		growthSafetyTime = GrowthTimeGet() + (double)Mathf.Max(1f, growthSafetyDelay);
		growthState = GrowthState.Settling;
	}

	private void GrowthLogic()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)physGrabObject) || physGrabObject.dead)
		{
			return;
		}
		switch (growthState)
		{
		case GrowthState.Growing:
		{
			float num2 = Mathf.Clamp01((float)(GrowthTimeGet() - growthStartTime) / growthDuration);
			Vector3 scale = Vector3.Lerp(growthStartScale, growthEndScale, Mathf.SmoothStep(0f, 1f, num2));
			GrowthScaleSmooth(scale);
			break;
		}
		case GrowthState.Settling:
			GrowthScaleSmooth(growthEndScale);
			if (GrowthTimeGet() >= growthSafetyTime)
			{
				GrowthConfirm();
			}
			break;
		case GrowthState.Confirming:
		{
			growthCorrectionTimer += Time.deltaTime;
			float num = Mathf.Clamp01(growthCorrectionTimer / Mathf.Max(0.1f, growthCorrectionTime));
			((Component)this).transform.localScale = Vector3.Lerp(growthCorrectionStartScale, growthEndScale, Mathf.SmoothStep(0f, 1f, num));
			if (num >= 1f)
			{
				((Component)this).transform.localScale = growthEndScale;
				growthState = GrowthState.Complete;
			}
			break;
		}
		}
	}

	private void GrowthScaleSmooth(Vector3 _scale)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).transform.localScale = Vector3.SmoothDamp(((Component)this).transform.localScale, _scale, ref growthVelocity, Mathf.Max(0.01f, growthSmoothTime), float.PositiveInfinity, Time.deltaTime);
	}

	private void GrowthConfirm()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && growthState == GrowthState.Settling && currentState == State.Awakened)
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("GrowthConfirmRPC", (RpcTarget)4, new object[1] { growthEndScale });
			}
			GrowthConfirmApply(growthEndScale);
		}
	}

	[PunRPC]
	private void GrowthConfirmRPC(Vector3 _endScale, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			GrowthConfirmApply(_endScale);
		}
	}

	private void GrowthConfirmApply(Vector3 _endScale)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Awakened && growthState != GrowthState.Confirming && growthState != GrowthState.Complete)
		{
			if (Object.op_Implicit((Object)(object)impactDetector))
			{
				impactDetector.impactAudioPitchMultiplier = 0.65f;
			}
			growthEndScale = _endScale;
			growthCorrectionStartScale = ((Component)this).transform.localScale;
			growthCorrectionTimer = 0f;
			growthVelocity = Vector3.zero;
			growthState = GrowthState.Confirming;
		}
	}

	private void ProtectionTick()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)physGrabObject) && !physGrabObject.dead && !(GrowthTimeGet() >= protectionEndTime))
		{
			physGrabObject.OverrideIndestructible();
		}
	}

	private void ActivationHoldLogic()
	{
		if (Object.op_Implicit((Object)(object)physGrabObject) && !physGrabObject.dead && (currentState == State.Finalizing || (currentState == State.Charging && activationVisualTimer > 0f)))
		{
			physGrabObject.OverrideZeroGravity(0.2f);
			physGrabObject.OverrideGrabDisable(0.2f);
			ActivationGrabsRelease();
		}
	}

	private void ActivationGrabsRelease()
	{
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarLocal();
		if (Object.op_Implicit((Object)(object)playerAvatar) && Object.op_Implicit((Object)(object)playerAvatar.physGrabber))
		{
			PhysGrabber physGrabber = playerAvatar.physGrabber;
			if (physGrabber.grabbed && !((Object)(object)physGrabber.grabbedPhysGrabObject != (Object)(object)physGrabObject))
			{
				physGrabber.ReleaseObject(photonView.ViewID, 0.2f);
			}
		}
	}

	private void PlatformAlignmentBegin(KnowledgeGlyphSide _side, Vector3 _position, Quaternion _rotation)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = (Object.op_Implicit((Object)(object)rb) ? rb.position : ((Component)this).transform.position);
		Quaternion val2 = (Object.op_Implicit((Object)(object)rb) ? rb.rotation : ((Component)this).transform.rotation);
		platformAlignmentPosition = _position;
		platformAlignmentUp = _rotation * Vector3.up;
		Vector3 val3;
		if (!Object.op_Implicit((Object)(object)cubeRenderer))
		{
			val3 = val;
		}
		else
		{
			Bounds bounds = ((Renderer)cubeRenderer).bounds;
			val3 = bounds.center;
		}
		Vector3 val4 = val3;
		platformAlignmentVisualCenterLocal = (Object.op_Implicit((Object)(object)rb) ? ((Component)rb).transform.InverseTransformPoint(val4) : Vector3.zero);
		platformAlignmentStartHeight = Mathf.Max(0f, Vector3.Dot(val4 - _position, platformAlignmentUp));
		Vector3 val5 = Vector3.ProjectOnPlane(_rotation * Vector3.forward, platformAlignmentUp);
		Vector3 normalized = val5.normalized;
		if (normalized.sqrMagnitude < 0.01f)
		{
			val5 = Vector3.ProjectOnPlane(_rotation * Vector3.right, platformAlignmentUp);
			normalized = val5.normalized;
		}
		Vector3 val6 = KnowledgeGlyphSideUtility.LocalDirectionGet(_side);
		Vector3 val7 = GlyphRayUpGet(_side);
		Quaternion val8 = Quaternion.LookRotation(-platformAlignmentUp, normalized) * Quaternion.Inverse(Quaternion.LookRotation(val6, val7));
		Quaternion val9 = (Object.op_Implicit((Object)(object)cubeRenderer) ? (Quaternion.Inverse(val2) * ((Component)cubeRenderer).transform.rotation) : Quaternion.identity);
		platformAlignmentRotation = val8 * Quaternion.Inverse(val9);
		platformAlignmentTimer = activationVisualTimer;
	}

	private void PlatformAlignmentFixedUpdate()
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		if (platformAlignmentTimer <= 0f)
		{
			return;
		}
		bool flag = platformAlignmentTimer <= Time.fixedDeltaTime;
		platformAlignmentTimer = Mathf.Max(0f, platformAlignmentTimer - Time.fixedDeltaTime);
		physGrabObject.OverrideZeroGravity(0.2f);
		float num = Mathf.Max(0.01f, activationLiftDuration);
		float num2 = 1f - platformAlignmentTimer / num;
		float num3 = 1f - Mathf.Pow(1f - Mathf.Clamp01(num2), 3f);
		float num4 = Mathf.Lerp(platformAlignmentStartHeight, activationLiftDistance, num3);
		Vector3 val = Vector3.Scale(platformAlignmentVisualCenterLocal, ((Component)rb).transform.lossyScale);
		Vector3 val2 = platformAlignmentRotation * val;
		Vector3 val3 = platformAlignmentPosition + platformAlignmentUp * num4 - val2;
		if (flag)
		{
			Vector3 val4 = rb.position + val2;
			Vector3 val5 = Vector3.ProjectOnPlane(platformAlignmentPosition - val4, platformAlignmentUp);
			rb.MovePosition(rb.position + val5);
			rb.MoveRotation(platformAlignmentRotation);
			rb.linearVelocity = Vector3.Project(rb.linearVelocity, platformAlignmentUp);
			rb.angularVelocity = Vector3.zero;
			return;
		}
		Vector3 val6 = val3 - rb.position;
		Vector3 val7 = Vector3.ProjectOnPlane(val6, platformAlignmentUp);
		Vector3 val8 = (val6 + val7 * (activationLateralStrengthMultiplier - 1f)) * activationPositionStrength - rb.linearVelocity * activationPositionDamping;
		rb.AddForce(val8, (ForceMode)5);
		Quaternion val9 = platformAlignmentRotation * Quaternion.Inverse(rb.rotation);
		float num5 = default;
		Vector3 val10 = default;
		val9.ToAngleAxis(ref num5, ref val10);
		if (num5 > 180f)
		{
			num5 -= 360f;
		}
		if (!float.IsNaN(val10.x))
		{
			Vector3 val11 = val10 * (num5 * (MathF.PI / 180f) * activationRotationStrength) - rb.angularVelocity * activationRotationDamping;
			rb.AddTorque(val11, (ForceMode)5);
		}
	}

	private void AwakenedFloatFixedUpdate()
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject.OverrideZeroGravity(0.2f);
		float num = Mathf.Max(0.01f, awakenedMoveTime);
		float num2 = Mathf.Clamp01(finalSequenceTimer / num);
		float num3 = Mathf.SmoothStep(0f, 1f, num2);
		Vector3 val = new Vector3(awakenedCenterPosition.x, targetFloatHeight, awakenedCenterPosition.z);
		Vector3 val2 = (Vector3.Lerp(awakenedStartPosition, val, num3) - rb.position) * floatStrength - rb.linearVelocity * floatDamping;
		rb.AddForce(val2, (ForceMode)5);
		float num4 = Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime);
		float num5 = Mathf.Pow(Mathf.Clamp01(finalSequenceTimer / num4), 2f);
		Vector3 val3 = (Vector3.up * awakenedSpinSpeed * num5 - rb.angularVelocity) * awakenedSpinStrength;
		rb.AddTorque(val3, (ForceMode)5);
		rb.AddTorque(new Vector3(0.35f, 0f, 0.2f) * floatTorque * num5, (ForceMode)5);
	}

	public bool ItemRejectActive()
	{
		if (((Behaviour)this).isActiveAndEnabled && currentState == State.Awakened && Object.op_Implicit((Object)(object)physGrabObject) && !physGrabObject.dead)
		{
			return destroyedTimer <= 0f;
		}
		return false;
	}

	public static bool ItemRejectInRange(PhysGrabObject _item, float _range)
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)_item) || _item.dead)
		{
			return false;
		}
		foreach (ValuableCubeOfKnowledge instance in instances)
		{
			if (!instance.ItemRejectActive())
			{
				continue;
			}
			Collider[] array = instance.itemRejectColliders;
			foreach (Collider val in array)
			{
				if (Object.op_Implicit((Object)(object)val) && val.enabled && !val.isTrigger && ((Component)val).gameObject.activeInHierarchy)
				{
					Vector3 val2 = val.ClosestPoint(((Component)_item).transform.position) - ((Component)_item).transform.position;
					if (val2.sqrMagnitude <= _range * _range)
					{
						return instance.ItemReject(_item);
					}
				}
			}
		}
		return false;
	}

	public bool ItemReject(PhysGrabObject _item)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		if (!ItemRejectActive() || !Object.op_Implicit((Object)(object)_item) || _item.dead)
		{
			return false;
		}
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return true;
		}
		ItemToggle component = ((Component)_item).GetComponent<ItemToggle>();
		if (!Object.op_Implicit((Object)(object)component) || !component.toggleState)
		{
			return true;
		}
		ItemDrone component2 = ((Component)_item).GetComponent<ItemDrone>();
		ItemOrb component3 = ((Component)_item).GetComponent<ItemOrb>();
		if (!Object.op_Implicit((Object)(object)component2) && !Object.op_Implicit((Object)(object)component3))
		{
			return false;
		}
		if (Object.op_Implicit((Object)(object)component2))
		{
			component2.RejectFromCube();
		}
		if (Object.op_Implicit((Object)(object)component3))
		{
			component3.RejectFromCube();
		}
		rejectedItems.Add(_item);
		Vector3 centerPoint = _item.centerPoint;
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("ItemRejectZapRPC", (RpcTarget)0, new object[2]
			{
				_item.photonView.ViewID,
				centerPoint
			});
		}
		else
		{
			ItemRejectZap(_item, centerPoint);
		}
		return true;
	}

	private void ItemRejectExisting()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		foreach (ItemDrone activeDrone in ItemDrone.activeDrones)
		{
			if (activeDrone.magnetActive && (Object)(object)activeDrone.magnetTargetPhysGrabObject == (Object)(object)physGrabObject)
			{
				ItemReject(((Component)activeDrone).GetComponent<PhysGrabObject>());
			}
		}
		ItemOrb[] array = Object.FindObjectsByType<ItemOrb>((FindObjectsSortMode)0);
		foreach (ItemOrb itemOrb in array)
		{
			if (itemOrb.currentState == ItemOrb.State.Active)
			{
				ItemRejectInRange(((Component)itemOrb).GetComponent<PhysGrabObject>(), itemOrb.orbRadius);
			}
		}
	}

	private void ItemRejectPhysics()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		foreach (PhysGrabObject rejectedItem in rejectedItems)
		{
			if (Object.op_Implicit((Object)(object)rejectedItem) && !rejectedItem.dead && Object.op_Implicit((Object)(object)rejectedItem.rb) && !rejectedItem.rb.isKinematic)
			{
				Vector3 val = Vector3.ProjectOnPlane(rejectedItem.centerPoint - physGrabObject.centerPoint, Vector3.up);
				if (val.sqrMagnitude < 0.0001f)
				{
					val = ((Component)this).transform.forward;
				}
				Vector3 val2 = val.normalized * itemRejectSpeed + Vector3.up * itemRejectUpSpeed;
				rejectedItem.OverrideGrabDisable(0.75f);
				rejectedItem.OverrideIndestructible(0.75f);
				rejectedItem.OverrideBreakEffects(0.75f);
				rejectedItem.rb.AddForce(val2 - rejectedItem.rb.linearVelocity, (ForceMode)2);
				rejectedItem.rb.AddTorque(Vector3.Cross(Vector3.up, val.normalized) * 8f, (ForceMode)2);
			}
		}
		rejectedItems.Clear();
	}

	[PunRPC]
	private void ItemRejectZapRPC(int _viewId, Vector3 _position, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			PhotonView val = PhotonView.Find(_viewId);
			ItemRejectZap(Object.op_Implicit((Object)(object)val) ? ((Component)val).GetComponent<PhysGrabObject>() : null, _position);
		}
	}

	private void ItemRejectZap(PhysGrabObject _item, Vector3 _position)
	{
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		itemRejectGlowTimer = 0.3f;
		if (Object.op_Implicit((Object)(object)_item))
		{
			_item.OverrideGrabDisable(0.75f);
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarLocal();
			if (Object.op_Implicit((Object)(object)playerAvatar) && Object.op_Implicit((Object)(object)playerAvatar.physGrabber) && (Object)(object)playerAvatar.physGrabber.grabbedPhysGrabObject == (Object)(object)_item)
			{
				playerAvatar.physGrabber.ReleaseObject(_item.photonView.ViewID, 0.75f);
			}
		}
		ItemRejectVisualsPlay(_item, _position);
		itemRejectSound.Play(_position);
	}

	private void ItemRejectVisualsPlay(PhysGrabObject _item, Vector3 _position)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.Photosensitivity())
		{
			LineBetweenTwoPoints lineBetweenTwoPoints = Object.Instantiate<LineBetweenTwoPoints>(itemRejectBeamPrefab);
			lineBetweenTwoPoints.SetColor(awakenedColor);
			lineBetweenTwoPoints.animationDuration = 0.025f;
			lineBetweenTwoPoints.lightRadius = 4.5f;
			float num = Mathf.Max(0.05f, itemRejectZapTime);
			itemZapBeams.Add(new ItemZap
			{
				beam = lineBetweenTwoPoints,
				target = _item,
				position = _position,
				timer = num,
				duration = num,
				seed = Random.Range(0f, 1000f),
				startFrame = Time.frameCount
			});
			ItemRejectParticlesPlay(itemRejectSparks, _position, 1.5f);
			ItemRejectParticlesPlay(itemRejectImpact, _position, 1.25f);
			ItemRejectParticlesPlay(itemRejectLightning, _position, 1.5f);
			ItemRejectParticlesPlay(itemRejectFlash, _position, 1.25f);
		}
	}

	private void ItemRejectParticlesPlay(ParticleSystem _prefab, Vector3 _position, float _scale)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		ParticleSystem val = Object.Instantiate<ParticleSystem>(_prefab, _position, Quaternion.identity);
		Transform transform = ((Component)val).transform;
		transform.localScale *= _scale;
		((Component)val).gameObject.SetActive(true);
		val.Play(true);
		MainModule main = val.main;
		GameObject gameObject = ((Component)val).gameObject;
		float duration = main.duration;
		MinMaxCurve startLifetime = main.startLifetime;
		Object.Destroy((Object)(object)gameObject, Mathf.Max(2f, duration + startLifetime.constantMax));
	}

	private void ItemZapTick()
	{
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.Photosensitivity())
		{
			if (itemZapBeams.Count > 0)
			{
				ItemZapCleanup();
			}
			return;
		}
		for (int num = itemZapBeams.Count - 1; num >= 0; num--)
		{
			ItemZap itemZap = itemZapBeams[num];
			if (itemZap.startFrame != Time.frameCount)
			{
				if (itemZap.timer <= 0f)
				{
					Object.Destroy((Object)(object)((Component)itemZap.beam).gameObject);
					itemZapBeams.RemoveAt(num);
				}
				else
				{
					if (Object.op_Implicit((Object)(object)itemZap.target) && !itemZap.target.dead)
					{
						itemZap.position = itemZap.target.centerPoint;
					}
					float num2 = itemZap.duration - itemZap.timer;
					float num3 = Mathf.SmoothStep(0f, 1f, num2 / 0.025f) * Mathf.SmoothStep(0f, 1f, itemZap.timer / 0.06f);
					float num4 = Mathf.Lerp(0.85f, 1.15f, Mathf.PerlinNoise(num2 * 90f, itemZap.seed));
					float width = 0.14f * num3 * num4;
					float flashIntensity = 8f * num3 * Mathf.Exp((0f - num2) * 18f);
					itemZap.beam.DrawLineLightning(physGrabObject.centerPoint, itemZap.position, width, flashIntensity, itemZap.seed);
					itemZap.timer = Mathf.Max(0f, itemZap.timer - Time.deltaTime);
				}
			}
		}
	}

	private void ItemZapCleanup()
	{
		itemRejectGlowTimer = 0f;
		itemRejectGlow = 0f;
		foreach (ItemZap itemZapBeam in itemZapBeams)
		{
			if (Object.op_Implicit((Object)(object)itemZapBeam.beam))
			{
				Object.Destroy((Object)(object)((Component)itemZapBeam.beam).gameObject);
			}
		}
		itemZapBeams.Clear();
	}

	private void AwakenedMassApply()
	{
		if (Object.op_Implicit((Object)(object)rb))
		{
			float num = ((Object.op_Implicit((Object)(object)physGrabObject) && physGrabObject.massOriginal > 0f) ? physGrabObject.massOriginal : rb.mass);
			float num2 = Mathf.Max(0.01f, num * awakenedMassMultiplier);
			rb.mass = num2;
			if (Object.op_Implicit((Object)(object)physGrabObject))
			{
				physGrabObject.massOriginal = num2;
			}
		}
	}

	private void Destroyed()
	{
		if (destroyedTimer > 0f)
		{
			return;
		}
		destroyedTimer = 1f;
		ItemZapCleanup();
		rejectedItems.Clear();
		platformAlignmentTimer = 0f;
		activationVisualTimer = 0f;
		AudioCleanup();
		for (int i = 0; i < boundPlatforms.Length; i++)
		{
			if (Object.op_Implicit((Object)(object)boundPlatforms[i]))
			{
				boundPlatforms[i].CubeDestroyed(this);
			}
		}
	}

	private float PlatformBreakDelayGet(int _side)
	{
		float num = Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime);
		float num2 = 0.65f;
		float num3 = Mathf.Max(0f, num - Mathf.Max(0.9f, finalPlatformBreakEndLeadTime) - num2) * Mathf.Clamp01((float)_side / 5f);
		return Mathf.Max(0f, num3 - finalSequenceTimer);
	}

	public float PlatformBreakTimeLeftGet(KnowledgeGlyphSide _side)
	{
		float num = Mathf.Max(0.01f, awakenedMoveTime + finalSpinTime);
		return Mathf.Max(0f, num - Mathf.Max(0.9f, finalPlatformBreakEndLeadTime) - 0.65f) * Mathf.Clamp01((float)_side / 5f) - Mathf.Max(0f, (float)(GrowthTimeGet() - growthStartTime));
	}

	public ValuableCubeOfKnowledge()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
	}
}
