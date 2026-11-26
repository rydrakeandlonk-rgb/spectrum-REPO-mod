using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ItemMelee), typeof(PhysGrabObject), typeof(PhotonView))]
public class ItemMeleeWhip : MonoBehaviour, IPunObservable
{
	public enum State
	{
		Idle,
		Casting,
		Holding,
		Windup,
		Releasing,
		Retracting,
		Backswing,
		HoldingBackswing,
		Catching,
		Cracked
	}

	private enum GestureAction
	{
		Prepare = 0,
		Strike = 1,
		Cancel = 2,
		Fling = 4
	}

	private sealed class CrackHitWindow
	{
		internal int sequence;

		internal float expires;

		internal float durability;

		internal PlayerAvatar holder;
	}

	private const float LowestEnemyDifficultyMultiplier = 0.65f;

	private const float NonEnemyDurabilityMultiplier = 0.2f;

	public SemiTentacle whip;

	public Transform origin;

	public float reach = 6f;

	public float catchRadius = 0.4f;

	public float castTime = 1.5f;

	public float crackTime = 0.26f;

	public float crackRecoverTime = 0.06f;

	public float retractTime = 1.5f;

	[Range(0f, 30f)]
	public float aimAssistAngle = 10f;

	public float aimAssistRadius = 1f;

	public ParticleSystem crackEffect;

	public HurtCollider crackHurtCollider;

	public float durabilityPerCrackHit = 5f;

	[Range(0f, 1f)]
	public float heldTension = 0.95f;

	public float blockedTime = 0.3f;

	public Vector2 enemyLevel1HoldTime = new Vector2(10f, 12f);

	public Vector2 enemyLevel2HoldTime = new Vector2(7f, 9f);

	public Vector2 enemyLevel3HoldTime = new Vector2(3f, 4f);

	public float tetherSlack = 0.25f;

	public float tetherSpring = 120f;

	public float tetherDamping = 18f;

	public float tetherForceMax = 150f;

	public float tetherAccelerationMax = 40f;

	public float tetherStretchMax = 2f;

	public float windupTime = 0.2f;

	public float flingSpeed = 18f;

	public float flingForceMax = 100f;

	public float releaseTime = 1f;

	public float durabilityPerFling = 5f;

	public float flingWaveSpeed = 24f;

	public float flingBendSize = 0.9f;

	public Sound soundCast = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundCatch = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundHoldLoop = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundFling = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundCrack = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundRetract = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundMove = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public Sound soundSwing = new Sound
	{
		Sounds = Array.Empty<AudioClip>()
	};

	public FakeVelocities fakeVelocities;

	public float moveSoundSpeed = 3.5f;

	public float moveSoundSpeedMax = 8f;

	public float moveSoundCooldown = 0.3f;

	public float swingSoundSpeed = 5f;

	public float swingSoundSpeedMax = 12f;

	public float swingSoundCooldown = 0.15f;

	internal State currentState;

	private PhotonView photonView;

	private ItemMelee melee;

	private ItemEquippable equippable;

	private ItemBattery battery;

	private PhysGrabObject physGrabObject;

	private PhysGrabObject target;

	private PlayerAvatar holder;

	private Transform castPoint;

	private Vector3 castPosition;

	private Vector3 flingDirection;

	private float flingPower;

	private float flingWaveDuration;

	private int flingWaveTimestamp;

	private Vector3 backswingDirection;

	private float tetherLength;

	private Vector3 tetherVelocity;

	private float stretchTimer;

	private float restingTension;

	private float stateTimer;

	private float enemyHoldTimer;

	private float obstructionTimer;

	private float requestTimer;

	private float backswingVisualTimer;

	private float backswingImpulseTimer;

	private Vector3 backswingVisualDirection;

	private int localGestureId;

	private int preparedGestureId;

	private readonly ItemMeleeWhipGesture gesture = new ItemMeleeWhipGesture();

	private readonly ItemMeleeWhipFlingGesture flingGesture = new ItemMeleeWhipFlingGesture();

	private Transform previousInputCamera;

	private int sequence;

	private int gestureStartSequence;

	private int targetViewID;

	private int holderViewID;

	private int publishedSequence;

	private int pendingCastSequence = -1;

	private int castTimestamp;

	private SemiTentacleCrackState castPath;

	private byte[] networkState;

	private int recoverySequence;

	private float recoveryRequestTime;

	private readonly Dictionary<int, float> recoveryReplyTimes = new Dictionary<int, float>();

	private float crackHurtTime;

	private int crackPlayerDamage;

	private bool crackPlayerKill;

	private PhysGrabObject crackHitTarget;

	private int crackHitSequence = -1;

	private int crackPlayerReportSequence = -1;

	private readonly List<CrackHitWindow> crackHitWindows = new List<CrackHitWindow>();

	private float holdSoundPitch = 1f;

	private float moveSoundTimer;

	private bool moveSoundReady = true;

	private Vector3 moveSoundVelocity;

	private const int CocoonSoundCount = 4;

	private int cocoonSoundIndex = -1;

	private float swingSoundTimer;

	private bool swingSoundReady = true;

	private Vector3 swingSoundVelocity;

	private readonly RaycastHit[] hits = new RaycastHit[64];

	private readonly Collider[] crackHits = new Collider[64];

	private readonly Collider[] aimHits = new Collider[128];

	private void Awake()
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected Obj, but got Unknown
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected Obj, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected Obj, but got Unknown
		photonView = ((Component)this).GetComponent<PhotonView>();
		melee = ((Component)this).GetComponent<ItemMelee>();
		equippable = ((Component)this).GetComponent<ItemEquippable>();
		battery = ((Component)this).GetComponent<ItemBattery>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		restingTension = whip.tension;
		castPoint = new GameObject("Whip Cast Point").transform;
		SceneManager.MoveGameObjectToScene(((Component)castPoint).gameObject, ((Component)this).gameObject.scene);
		if (Object.op_Implicit((Object)(object)crackHurtCollider))
		{
			crackHurtTime = crackHurtCollider.timer;
			crackPlayerDamage = crackHurtCollider.playerDamage;
			crackPlayerKill = crackHurtCollider.playerKill;
			crackHurtCollider.onImpactEnemy.AddListener((UnityAction)CrackHit);
			crackHurtCollider.onImpactPhysObject.AddListener((UnityAction)CrackPhysObjectHit);
			crackHurtCollider.onImpactPlayer.AddListener((UnityAction)CrackPlayerHit);
			((Component)crackHurtCollider).gameObject.SetActive(false);
			((Component)crackHurtCollider).transform.SetParent(castPoint, false);
			crackHurtCollider.addRuckusOnPhysHit = true;
		}
	}

	private void Update()
	{
		if (Object.op_Implicit((Object)(object)LevelGenerator.Instance) && LevelGenerator.Instance.Generated)
		{
			ReferenceTick();
			RecoveryTick();
			VisualTick();
			HoldSoundTick();
			CocoonSoundTick();
			MoveSoundTick();
			if (!SemiFunc.IsMasterClientOrSingleplayer())
			{
				stateTimer += Time.deltaTime;
				return;
			}
			InterruptionTick();
			EnemyHoldTick();
			PlayerHoldTick();
			StateMachine();
			stateTimer += Time.deltaTime;
		}
	}

	private void FixedUpdate()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && TargetValid(target) && HolderValid(holder) && target.playerGrabbing.Count <= 0 && !target.rb.isKinematic && (!target.isPlayer || PlayerTargetReady(target)))
		{
			if (currentState == State.Releasing)
			{
				ReleasePhysics();
			}
			else if (currentState == State.Holding || currentState == State.HoldingBackswing || currentState == State.Windup)
			{
				TetherPhysics();
			}
		}
	}

	private void OnDisable()
	{
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)crackHurtCollider))
		{
			((Component)crackHurtCollider).gameObject.SetActive(false);
		}
		crackHitTarget = null;
		crackHitSequence = -1;
		crackPlayerReportSequence = -1;
		crackHitWindows.Clear();
		if (Object.op_Implicit((Object)(object)whip))
		{
			whip.SetTension(restingTension);
			whip.SpringIn();
		}
		HoldSoundStop();
		cocoonSoundIndex = -1;
		currentState = State.Idle;
		target = null;
		holder = null;
		targetViewID = 0;
		holderViewID = 0;
		stateTimer = 0f;
		enemyHoldTimer = 0f;
		obstructionTimer = 0f;
		requestTimer = 0f;
		tetherLength = 0f;
		tetherVelocity = Vector3.zero;
		stretchTimer = 0f;
		gesture.Reset();
		flingGesture.Reset();
		flingPower = 0f;
		flingWaveDuration = 0f;
		preparedGestureId = 0;
		backswingVisualTimer = 0f;
		backswingImpulseTimer = 0f;
		sequence = (SemiFunc.IsMasterClientOrSingleplayer() ? (sequence + 1) : (-1));
		gestureStartSequence = sequence;
		publishedSequence = sequence;
		pendingCastSequence = -1;
		castPath = default;
		networkState = null;
		recoverySequence = Mathf.Max(0, sequence);
		recoveryRequestTime = 0f;
		recoveryReplyTimes.Clear();
	}

	private void OnDestroy()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected Obj, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected Obj, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected Obj, but got Unknown
		if (Object.op_Implicit((Object)(object)crackHurtCollider))
		{
			crackHurtCollider.onImpactEnemy.RemoveListener((UnityAction)CrackHit);
			crackHurtCollider.onImpactPhysObject.RemoveListener((UnityAction)CrackPhysObjectHit);
			crackHurtCollider.onImpactPlayer.RemoveListener((UnityAction)CrackPlayerHit);
		}
		if (Object.op_Implicit((Object)(object)castPoint))
		{
			Object.Destroy((Object)(object)((Component)castPoint).gameObject);
		}
	}

	private void StateMachine()
	{
		switch (currentState)
		{
		case State.Casting:
			StateCasting();
			break;
		case State.Holding:
			StateHolding();
			break;
		case State.Windup:
			StateWindup();
			break;
		case State.Releasing:
			StateReleasing();
			break;
		case State.Retracting:
			StateRetracting();
			break;
		case State.Backswing:
			StateBackswing();
			break;
		case State.HoldingBackswing:
			StateHoldingBackswing();
			break;
		case State.Catching:
			StateCatching();
			break;
		case State.Cracked:
			StateCracked();
			break;
		case State.Idle:
			break;
		}
	}

	private void StateCasting()
	{
		if (whip.CrackReachedPoint(castPoint))
		{
			Crack();
		}
		else if (stateTimer >= castTime)
		{
			StateSet(State.Retracting);
		}
	}

	private void StateCatching()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if ((!target.isPlayer || PlayerTargetReady(target)) && whip.AttachedToPoint(((Component)target).transform))
		{
			tetherLength = Vector3.Distance(origin.position, target.centerPoint) + tetherSlack;
			stretchTimer = 0f;
			enemyHoldTimer = EnemyHoldTimeGet();
			StateSet(State.Holding);
		}
		else if (stateTimer >= castTime)
		{
			StateSet(State.Retracting);
		}
	}

	private void StateCracked()
	{
		if (!CrackHitTargetCatch() && stateTimer >= Mathf.Max(crackRecoverTime, crackHurtTime))
		{
			StateSet(State.Retracting);
		}
	}

	private void StateHolding()
	{
		if (!whip.AttachedToPoint(((Component)target).transform))
		{
			StateSet(State.Retracting);
		}
	}

	private void StateBackswing()
	{
		if (!(stateTimer < 1.25f))
		{
			preparedGestureId = 0;
			StateSet(State.Idle);
		}
	}

	private void StateHoldingBackswing()
	{
		if (!whip.AttachedToPoint(((Component)target).transform))
		{
			StateSet(State.Retracting);
		}
		else if (!(stateTimer < 1.25f))
		{
			preparedGestureId = 0;
			StateSet(State.Holding);
		}
	}

	private void StateWindup()
	{
		if (!whip.AttachedToPoint(((Component)target).transform))
		{
			StateSet(State.Retracting);
		}
		else if (!(FlingWaveElapsedGet() < flingWaveDuration))
		{
			StateSet(State.Releasing);
		}
	}

	private void StateReleasing()
	{
		if (stateTimer >= releaseTime)
		{
			StateSet(State.Retracting);
		}
	}

	private void StateRetracting()
	{
		if (whip.FullyRetracted())
		{
			StateSet(State.Idle);
		}
		else if (!(stateTimer < retractTime))
		{
			whip.SpringInDetached();
			StateSet(State.Idle);
		}
	}

	private void StateSet(State _state, bool _playFling = false)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || currentState == _state)
		{
			return;
		}
		if (_state == State.Retracting && !_playFling)
		{
			TetherVelocityCancel();
		}
		if (_state == State.Retracting || _state == State.Releasing || _state == State.Idle)
		{
			tetherVelocity = Vector3.zero;
		}
		int num = sequence + 1;
		StateApply(num, _state, target, holder, castPosition, flingDirection, backswingDirection, tetherLength, _playFling);
		pendingCastSequence = -1;
		if (SemiFunc.IsMultiplayer())
		{
			if (_state == State.Casting)
			{
				pendingCastSequence = sequence;
			}
			else
			{
				StatePublish(_playFling);
			}
		}
	}

	private void StateApply(int _sequence, State _state, PhysGrabObject _target, PlayerAvatar _holder, Vector3 _castPosition, Vector3 _flingDirection, Vector3 _backswingDirection, float _tetherLength, bool _playFling = false, bool _recovery = false)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (_sequence <= sequence)
		{
			return;
		}
		State state = currentState;
		sequence = _sequence;
		currentState = _state;
		if ((_state == State.Idle && state != State.Backswing) || (_state == State.Holding && state == State.Catching))
		{
			gestureStartSequence = sequence;
		}
		crackHitTarget = null;
		stateTimer = 0f;
		obstructionTimer = 0f;
		target = _target;
		holder = _holder;
		castPosition = _castPosition;
		flingDirection = _flingDirection;
		backswingDirection = _backswingDirection;
		tetherLength = _tetherLength;
		if (_state == State.Backswing || _state == State.HoldingBackswing)
		{
			if (!_recovery && backswingVisualTimer <= 0f)
			{
				BackswingVisualStart(backswingDirection);
			}
		}
		else
		{
			backswingVisualTimer = 0f;
			backswingImpulseTimer = 0f;
		}
		if (_state == State.Idle || _state == State.Retracting)
		{
			enemyHoldTimer = 0f;
		}
		if (_state != State.Windup)
		{
			whip.OverrideAttachedWaveReset();
		}
		if (!_recovery && state != State.Cracked && (_state == State.Catching || _state == State.Cracked))
		{
			CrackImpactPlay();
		}
		if (_state == State.Holding && state == State.Catching)
		{
			gesture.Reset();
			flingGesture.Reset();
			requestTimer = 0f;
		}
		if (_state != State.Backswing && _state != State.HoldingBackswing)
		{
			preparedGestureId = 0;
		}
		if (_recovery || _state == State.Idle || _state == State.Releasing || _state == State.Retracting)
		{
			cocoonSoundIndex = -1;
		}
		else if (_state == State.Catching)
		{
			cocoonSoundIndex = 0;
		}
		if (!_recovery)
		{
			StateSound(_state, state, _playFling);
		}
		if ((_state == State.Retracting) & _playFling)
		{
			whip.SpringInDetached();
		}
		else if (_state == State.Releasing || _state == State.Retracting)
		{
			whip.SpringIn();
		}
		if (_state == State.Idle)
		{
			castPath = default;
			target = null;
			holder = null;
			targetViewID = 0;
			holderViewID = 0;
			tetherLength = 0f;
			stretchTimer = 0f;
		}
	}

	private void LateUpdate()
	{
		if (Object.op_Implicit((Object)(object)LevelGenerator.Instance) && LevelGenerator.Instance.Generated)
		{
			InputTick();
		}
	}

	private void InputTick()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		if (requestTimer > 0f)
		{
			requestTimer -= Time.deltaTime;
			gesture.Reset();
			flingGesture.Reset();
			return;
		}
		PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarLocal();
		if (!GestureCanUse(playerAvatar))
		{
			GestureCancel(playerAvatar);
			return;
		}
		Transform val = playerAvatar.localCamera.GetOverrideTransform();
		if (!playerAvatar.localCamera.GetOverrideActive() && Object.op_Implicit((Object)(object)CameraAim.Instance))
		{
			val = ((Component)CameraAim.Instance).transform;
		}
		if ((Object)(object)previousInputCamera != (Object)(object)val)
		{
			GestureCancel(playerAvatar);
			previousInputCamera = val;
		}
		Vector2 input = InputManager.instance.GrabCursorLookGet(clampInput: false);
		bool gamepad = InputManager.instance.CurrentMode == InputModes.Gamepad;
		if (currentState == State.Holding || currentState == State.HoldingBackswing)
		{
			FlingInputTick(playerAvatar, val, input, gamepad);
			return;
		}
		flingGesture.Reset();
		Vector3 _capturedAim;
		Vector3 _backswing;
		switch (gesture.Tick(val.forward, val.right, val.up, input, gamepad, _throw: false, Time.deltaTime, out _capturedAim, out _backswing))
		{
		case ItemMeleeWhipGesture.Action.Prepare:
			localGestureId++;
			BackswingVisualStart(_backswing);
			GestureRequest(playerAvatar, GestureAction.Prepare, _capturedAim, _backswing);
			break;
		case ItemMeleeWhipGesture.Action.Strike:
			BackswingVisualStop();
			requestTimer = 0.3f;
			GestureRequest(playerAvatar, GestureAction.Strike, _capturedAim, _backswing);
			break;
		case ItemMeleeWhipGesture.Action.Cancel:
			BackswingVisualStop();
			GestureRequest(playerAvatar, GestureAction.Cancel, Vector3.zero, Vector3.zero);
			break;
		}
	}

	private void FlingInputTick(PlayerAvatar _player, Transform _camera, Vector2 _input, bool _gamepad)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		gesture.Reset();
		Vector3 targetDirection = (Object.op_Implicit((Object)(object)target) ? (whip.GrabAnchorPosition() - _camera.position) : Vector3.zero);
		Vector3 _direction;
		Vector3 _bend;
		float _power;
		switch (flingGesture.Tick(_camera.forward, _camera.right, _camera.up, _input, _gamepad, targetDirection, Time.deltaTime, out _direction, out _bend, out _power))
		{
		case ItemMeleeWhipGesture.Action.Prepare:
			localGestureId++;
			BackswingVisualStart(_bend);
			GestureRequest(_player, GestureAction.Prepare, _direction, _bend);
			break;
		case ItemMeleeWhipGesture.Action.Strike:
			BackswingVisualStop();
			requestTimer = 0.3f;
			GestureRequest(_player, GestureAction.Fling, _direction, _bend, _power);
			break;
		case ItemMeleeWhipGesture.Action.Cancel:
			BackswingVisualStop();
			GestureRequest(_player, GestureAction.Cancel, Vector3.zero, Vector3.zero);
			break;
		}
	}

	private bool GestureCanUse(PlayerAvatar _player)
	{
		if ((currentState == State.Idle || currentState == State.Holding || currentState == State.Backswing || currentState == State.HoldingBackswing) && physGrabObject.heldByLocalPlayer && Object.op_Implicit((Object)(object)PlayerController.instance) && PlayerController.instance.InputDisableTimer <= 0f && Application.isFocused && Object.op_Implicit((Object)(object)InputManager.instance) && !InputManager.instance.ExpressionWheelActive() && (!Object.op_Implicit((Object)(object)CameraAim.Instance) || (!CameraAim.Instance.AimTargetActive && !CameraAim.Instance.overrideAimStop)) && (!Object.op_Implicit((Object)(object)GameDirector.instance) || !GameDirector.instance.DisableInput) && !SemiFunc.InputHold(InputKey.Rotate) && !SemiFunc.InputHold(InputKey.PushPull) && HolderValid(_player) && melee.currentState != ItemMelee.State.Broken)
		{
			if (Object.op_Implicit((Object)(object)battery))
			{
				return battery.batteryLife > 0f;
			}
			return true;
		}
		return false;
	}

	private void GestureCancel(PlayerAvatar _player)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (gesture.Prepared || flingGesture.Prepared)
		{
			if (Object.op_Implicit((Object)(object)_player) && physGrabObject.heldByLocalPlayer)
			{
				GestureRequest(_player, GestureAction.Cancel, Vector3.zero, Vector3.zero);
			}
			BackswingVisualStop();
		}
		gesture.Reset();
		flingGesture.Reset();
	}

	private void GestureRequest(PlayerAvatar _player, GestureAction _action, Vector3 _aim, Vector3 _backswing, float _power = 0f)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("UseRPC", (RpcTarget)2, new object[7]
			{
				_player.photonView.ViewID,
				localGestureId,
				sequence,
				(int)_action,
				_aim,
				_backswing,
				_power
			});
		}
		else
		{
			GestureApply(_player, localGestureId, sequence, _action, _aim, _backswing, _power);
		}
	}

	private void GestureApply(PlayerAvatar _player, int _gestureId, int _sequence, GestureAction _action, Vector3 _aim, Vector3 _backswing, float _power = 0f)
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !HolderValid(_player))
		{
			return;
		}
		switch (_action)
		{
		case GestureAction.Fling:
			if ((currentState == State.Holding || currentState == State.HoldingBackswing) && _sequence >= gestureStartSequence && _sequence <= sequence && (currentState != State.HoldingBackswing || (_gestureId == preparedGestureId && preparedGestureId != 0)) && !((Object)(object)holder != (Object)(object)_player) && TargetValid(target) && target.playerGrabbing.Count <= 0 && melee.currentState != ItemMelee.State.Broken && (!Object.op_Implicit((Object)(object)battery) || !(battery.batteryLife <= 0f)) && ItemMeleeWhipGesture.DirectionValid(_aim) && ItemMeleeWhipGesture.DirectionValid(_backswing) && float.IsFinite(_power) && !(_power <= 0f))
			{
				FlingStart(_aim, _backswing, Mathf.Clamp01(_power));
			}
			return;
		case GestureAction.Prepare:
			GesturePrepare(_player, _gestureId, _sequence, _aim, _backswing);
			return;
		case GestureAction.Strike:
			if (currentState == State.Idle || currentState == State.Holding)
			{
				if (_sequence >= gestureStartSequence && _sequence <= sequence)
				{
					backswingDirection = (ItemMeleeWhipGesture.DirectionValid(_backswing) ? _backswing.normalized : Vector3.zero);
					UseDirection(_player, _aim);
				}
				return;
			}
			break;
		}
		if (_gestureId == preparedGestureId && preparedGestureId != 0 && !((Object)(object)holder != (Object)(object)_player) && (currentState == State.Backswing || currentState == State.HoldingBackswing))
		{
			preparedGestureId = 0;
			switch (_action)
			{
			case GestureAction.Strike:
				UseDirection(_player, _aim);
				break;
			case GestureAction.Cancel:
				StateSet((currentState != State.Backswing) ? State.Holding : State.Idle);
				break;
			}
		}
	}

	private void GesturePrepare(PlayerAvatar _player, int _gestureId, int _sequence, Vector3 _aim, Vector3 _backswing)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		if (_gestureId > 0 && (currentState == State.Idle || currentState == State.Holding) && _sequence >= gestureStartSequence && _sequence <= sequence && melee.currentState != ItemMelee.State.Broken && (!Object.op_Implicit((Object)(object)battery) || !(battery.batteryLife <= 0f)) && ItemMeleeWhipGesture.DirectionValid(_aim) && ItemMeleeWhipGesture.DirectionValid(_backswing) && (currentState != State.Holding || !((Object)(object)holder != (Object)(object)_player)) && (currentState != State.Holding || TargetValid(target)) && (currentState != State.Idle || whip.FullyRetracted()))
		{
			preparedGestureId = _gestureId;
			flingDirection = _aim.normalized;
			backswingDirection = _backswing.normalized;
			holder = _player;
			holderViewID = _player.photonView.ViewID;
			StateSet((currentState == State.Idle) ? State.Backswing : State.HoldingBackswing);
		}
	}

	private void Use(PlayerAvatar _player)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (HolderValid(_player))
		{
			UseDirection(_player, AimDirection(_player));
		}
	}

	private void UseDirection(PlayerAvatar _player, Vector3 _direction)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer() && HolderValid(_player) && melee.currentState != ItemMelee.State.Broken && (!Object.op_Implicit((Object)(object)battery) || !(battery.batteryLife <= 0f)) && ItemMeleeWhipGesture.DirectionValid(_direction))
		{
			if (currentState == State.Idle || currentState == State.Backswing)
			{
				Cast(_player, _direction.normalized);
			}
			else if ((currentState == State.HoldingBackswing || (currentState == State.Holding && stateTimer >= 0.2f)) && (Object)(object)holder == (Object)(object)_player && TargetValid(target))
			{
				StateSet(State.Retracting);
			}
		}
	}

	private void FlingStart(Vector3 _direction, Vector3 _bend, float _power)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		flingDirection = _direction.normalized;
		backswingDirection = _bend.normalized;
		flingPower = _power;
		float num = Vector3.Distance(origin.position, target.centerPoint);
		flingWaveDuration = Mathf.Clamp(Mathf.Max(windupTime, num / Mathf.Max(0.01f, flingWaveSpeed)), 0.12f, 0.5f);
		flingWaveTimestamp = PhotonNetwork.ServerTimestamp;
		StateSet(State.Windup);
	}

	private float FlingWaveElapsedGet()
	{
		return Mathf.Min(stateTimer, flingWaveDuration);
	}

	private bool HolderValid(PlayerAvatar _player)
	{
		if (Object.op_Implicit((Object)(object)_player) && !_player.isDisabled && !_player.deadSet && Object.op_Implicit((Object)(object)_player.physGrabber) && Object.op_Implicit((Object)(object)_player.localCamera) && ((Behaviour)physGrabObject).isActiveAndEnabled && !physGrabObject.dead && physGrabObject.playerGrabbing.Contains(_player.physGrabber))
		{
			if (Object.op_Implicit((Object)(object)equippable))
			{
				if (!equippable.IsEquipped() && !equippable.isEquipping)
				{
					return !equippable.isUnequipping;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private Vector3 AimDirection(PlayerAvatar _player)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		Transform overrideTransform = _player.localCamera.GetOverrideTransform();
		Vector3 val = _player.physGrabber.physGrabPointPullerPosition - overrideTransform.position;
		if (val.sqrMagnitude < 0.0001f)
		{
			return overrideTransform.forward;
		}
		return val.normalized;
	}

	private void Cast(PlayerAvatar _player, Vector3 _direction)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (whip.FullyRetracted())
		{
			holder = _player;
			holderViewID = _player.photonView.ViewID;
			flingDirection = _direction;
			if (!CrackPositionTryGet(_player, _direction, out castPosition))
			{
				StateSet(State.Retracting);
				return;
			}
			target = null;
			targetViewID = 0;
			tetherLength = 0f;
			stretchTimer = 0f;
			StateSet(State.Casting);
		}
	}

	private bool CrackPositionTryGet(PlayerAvatar _player, Vector3 _direction, out Vector3 _position)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = _player.localCamera.GetOverrideTransform().position;
		_position = position + _direction * reach;
		int num = Physics.RaycastNonAlloc(position, _direction, hits, reach, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()), (QueryTriggerInteraction)1);
		if (num == hits.Length)
		{
			return false;
		}
		float num2 = float.PositiveInfinity;
		PhysGrabObject physGrabObject = null;
		for (int i = 0; i < num; i++)
		{
			RaycastHit val = hits[i];
			if (!IgnoreCollider(val.collider) && !(val.distance >= num2))
			{
				num2 = val.distance;
				_position = val.point;
				physGrabObject = ColliderTargetGet(val.collider);
			}
		}
		if (!TargetValid(physGrabObject) || physGrabObject.playerGrabbing.Count > 0)
		{
			AimAssistTryGet(position, _direction, ref _position);
		}
		return CrackPathTryClip(ref _position);
	}

	private bool CrackPathTryClip(ref Vector3 _position)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = _position - origin.position;
		float num = val.magnitude;
		if (num <= 0.001f)
		{
			return true;
		}
		int num2 = Physics.RaycastNonAlloc(origin.position, val / num, hits, num, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()), (QueryTriggerInteraction)1);
		if (num2 == hits.Length)
		{
			return false;
		}
		for (int i = 0; i < num2; i++)
		{
			RaycastHit val2 = hits[i];
			if (!IgnoreCollider(val2.collider) && !(val2.distance >= num))
			{
				num = val2.distance;
				_position = val2.point;
			}
		}
		return true;
	}

	private void AimAssistTryGet(Vector3 _aimOrigin, Vector3 _direction, ref Vector3 _position)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		if (aimAssistAngle <= 0f || aimAssistRadius <= 0f)
		{
			return;
		}
		int num = Physics.OverlapSphereNonAlloc(_aimOrigin, reach, aimHits, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPlayersAndPhysObjects()), (QueryTriggerInteraction)1);
		if (num == aimHits.Length)
		{
			return;
		}
		float num2 = float.PositiveInfinity;
		for (int i = 0; i < num; i++)
		{
			Collider val = aimHits[i];
			if (IgnoreCollider(val) || !ColliderSupportsClosestPoint(val))
			{
				continue;
			}
			PhysGrabObject physGrabObject = ColliderTargetGet(val);
			if (TargetValid(physGrabObject) && physGrabObject.playerGrabbing.Count <= 0)
			{
				Bounds bounds = val.bounds;
				float num3 = Mathf.Clamp(Vector3.Dot(bounds.center - _aimOrigin, _direction), 0f, reach);
				Vector3 val2 = val.ClosestPoint(_aimOrigin + _direction * num3);
				float num4 = ItemMeleeWhipAim.Score(val2 - _aimOrigin, _direction, reach, aimAssistAngle, aimAssistRadius);
				if (!(num4 >= num2) && !ObstructedFrom(_aimOrigin, val2, physGrabObject) && !Obstructed(val2, physGrabObject))
				{
					num2 = num4;
					_position = val2;
				}
			}
		}
	}

	private float FlingSpeedGet()
	{
		float num = flingSpeed * flingPower;
		if (!Object.op_Implicit((Object)(object)target) || !Object.op_Implicit((Object)(object)target.rb))
		{
			return num;
		}
		return Mathf.Min(num, flingForceMax / Mathf.Max(0.01f, target.rb.mass));
	}

	private void Crack()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		castPosition = whip.TipPositionGet();
		target = CrackTargetGet();
		PlayerCatchStart();
		targetViewID = ((Object.op_Implicit((Object)(object)target) && Object.op_Implicit((Object)(object)target.photonView)) ? target.photonView.ViewID : 0);
		StateSet(Object.op_Implicit((Object)(object)target) ? State.Catching : State.Cracked);
	}

	private PhysGrabObject CrackTargetGet()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		int num = Physics.OverlapSphereNonAlloc(castPosition, catchRadius, crackHits, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()), (QueryTriggerInteraction)1);
		if (num == crackHits.Length)
		{
			return null;
		}
		float num2 = float.PositiveInfinity;
		PhysGrabObject physGrabObject = null;
		Vector3 end = castPosition;
		for (int i = 0; i < num; i++)
		{
			Collider val = crackHits[i];
			if (!IgnoreCollider(val) && ColliderSupportsClosestPoint(val))
			{
				Vector3 val2 = val.ClosestPoint(castPosition);
				Vector3 val3 = val2 - castPosition;
				float sqrMagnitude = val3.sqrMagnitude;
				if (!(sqrMagnitude >= num2))
				{
					num2 = sqrMagnitude;
					end = val2;
					physGrabObject = ColliderTargetGet(val);
				}
			}
		}
		if (!TargetValid(physGrabObject) || physGrabObject.playerGrabbing.Count > 0 || Obstructed(castPosition, physGrabObject))
		{
			return null;
		}
		if (Obstructed(end, physGrabObject) || ObstructedFrom(castPosition, end, physGrabObject))
		{
			return null;
		}
		return physGrabObject;
	}

	private bool ColliderSupportsClosestPoint(Collider _collider)
	{
		if (!(_collider is BoxCollider) && !(_collider is SphereCollider) && !(_collider is CapsuleCollider))
		{
			MeshCollider val = (MeshCollider)(object)((_collider is MeshCollider) ? _collider : null);
			if (val != null)
			{
				return val.convex;
			}
			return false;
		}
		return true;
	}

	private bool TargetValid(PhysGrabObject _target)
	{
		if (!Object.op_Implicit((Object)(object)_target) || !((Behaviour)_target).isActiveAndEnabled || _target.dead || !Object.op_Implicit((Object)(object)_target.rb) || (Object)(object)_target == (Object)(object)physGrabObject || ((Component)_target).transform.IsChildOf(((Component)this).transform) || ((Component)_target).transform.IsChildOf(Object.op_Implicit((Object)(object)holder) ? ((Component)holder).transform : ((Component)this).transform))
		{
			return false;
		}
		if (_target.isPlayer)
		{
			PlayerTumble component = ((Component)_target).GetComponent<PlayerTumble>();
			PlayerAvatar playerAvatar = (Object.op_Implicit((Object)(object)component) ? component.playerAvatar : null);
			if (!Object.op_Implicit((Object)(object)playerAvatar) || (Object)(object)playerAvatar == (Object)(object)holder || !((Behaviour)playerAvatar).isActiveAndEnabled || playerAvatar.isDisabled || playerAvatar.deadSet || (Object)(object)playerAvatar.tumble != (Object)(object)component || !((Behaviour)component).isActiveAndEnabled)
			{
				return false;
			}
		}
		else if (_target.rb.isKinematic)
		{
			return false;
		}
		if (Object.op_Implicit((Object)(object)((Component)_target).GetComponent<PhysGrabCart>()) || Object.op_Implicit((Object)(object)((Component)_target).GetComponent<PhysGrabHinge>()))
		{
			return false;
		}
		ItemEquippable itemEquippable = default;
		if (((Component)_target).TryGetComponent<ItemEquippable>(ref itemEquippable) && itemEquippable.IsEquipped())
		{
			return false;
		}
		if (SemiFunc.IsMultiplayer() && (!Object.op_Implicit((Object)(object)_target.photonView) || _target.photonView.ViewID == 0))
		{
			return false;
		}
		if (_target.isEnemy && (!Object.op_Implicit((Object)(object)_target.enemyRigidbody) || !_target.enemyRigidbody.enemy.HasStateStunned))
		{
			return false;
		}
		return true;
	}

	private PhysGrabObject ColliderTargetGet(Collider _collider)
	{
		PhysGrabObject componentInParent = ((Component)_collider).GetComponentInParent<PhysGrabObject>();
		if (Object.op_Implicit((Object)(object)componentInParent))
		{
			return componentInParent;
		}
		PlayerAvatar playerAvatar = ((Component)_collider).GetComponentInParent<PlayerAvatar>();
		if (!Object.op_Implicit((Object)(object)playerAvatar))
		{
			PlayerController componentInParent2 = ((Component)_collider).GetComponentInParent<PlayerController>();
			if (Object.op_Implicit((Object)(object)componentInParent2))
			{
				playerAvatar = componentInParent2.playerAvatarScript;
			}
		}
		if (!Object.op_Implicit((Object)(object)playerAvatar))
		{
			PlayerAvatarCollision componentInParent3 = ((Component)_collider).GetComponentInParent<PlayerAvatarCollision>();
			if (Object.op_Implicit((Object)(object)componentInParent3))
			{
				playerAvatar = componentInParent3.PlayerAvatar;
			}
		}
		if (!Object.op_Implicit((Object)(object)playerAvatar))
		{
			PlayerAvatarVisuals componentInParent4 = ((Component)_collider).GetComponentInParent<PlayerAvatarVisuals>();
			if (Object.op_Implicit((Object)(object)componentInParent4))
			{
				playerAvatar = componentInParent4.playerAvatar;
			}
		}
		if (!Object.op_Implicit((Object)(object)playerAvatar) || !Object.op_Implicit((Object)(object)playerAvatar.tumble))
		{
			return null;
		}
		return playerAvatar.tumble.physGrabObject;
	}

	private bool PlayerTargetReady(PhysGrabObject _target)
	{
		PlayerTumble component = ((Component)_target).GetComponent<PlayerTumble>();
		if (Object.op_Implicit((Object)(object)component) && component.isTumbling && Object.op_Implicit((Object)(object)component.playerAvatar))
		{
			return component.playerAvatar.isTumbling;
		}
		return false;
	}

	private void PlayerCatchStart()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)target) && target.isPlayer)
		{
			PlayerTumble component = ((Component)target).GetComponent<PlayerTumble>();
			component.OverrideTumbleHold(0.25f);
			if (!component.isTumbling)
			{
				component.TumbleSet(_isTumbling: true, _playerInput: false);
			}
		}
	}

	private float EnemyHoldTimeGet()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (!target.isEnemy)
		{
			return 0f;
		}
		EnemyParent.Difficulty difficulty = target.enemyRigidbody.enemy.EnemyParent.difficulty;
		Vector2 val = enemyLevel2HoldTime;
		switch (difficulty)
		{
		case EnemyParent.Difficulty.Difficulty1:
			val = enemyLevel1HoldTime;
			break;
		case EnemyParent.Difficulty.Difficulty3:
			val = enemyLevel3HoldTime;
			break;
		}
		return Random.Range(val.x, val.y);
	}

	private void EnemyHoldTick()
	{
		if (Object.op_Implicit((Object)(object)target) && target.isEnemy && (currentState == State.Holding || currentState == State.HoldingBackswing || currentState == State.Windup))
		{
			if (enemyHoldTimer <= 0f)
			{
				StateSet(State.Retracting);
			}
			else
			{
				enemyHoldTimer = Mathf.Max(0f, enemyHoldTimer - Time.deltaTime);
			}
		}
	}

	private void PlayerHoldTick()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && Object.op_Implicit((Object)(object)target) && target.isPlayer && (currentState == State.Catching || currentState == State.Holding || currentState == State.HoldingBackswing || currentState == State.Windup || currentState == State.Releasing) && TargetValid(target) && PlayerTargetReady(target))
		{
			((Component)target).GetComponent<PlayerTumble>().OverrideTumbleHold(0.25f);
		}
	}

	private bool IgnoreCollider(Collider _collider)
	{
		if (Object.op_Implicit((Object)(object)_collider) && !((Component)_collider).transform.IsChildOf(((Component)this).transform))
		{
			if (Object.op_Implicit((Object)(object)holder))
			{
				if (!((Component)_collider).transform.IsChildOf(((Component)holder).transform))
				{
					if (Object.op_Implicit((Object)(object)holder.tumble))
					{
						return (Object)(object)ColliderTargetGet(_collider) == (Object)(object)holder.tumble.physGrabObject;
					}
					return false;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private bool Obstructed(Vector3 _end, PhysGrabObject _allowedTarget = null)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return ObstructedFrom(origin.position, _end, _allowedTarget);
	}

	private bool ObstructedFrom(Vector3 _start, Vector3 _end, PhysGrabObject _allowedTarget)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = _end - _start;
		if (val.sqrMagnitude <= 0.0001f)
		{
			return false;
		}
		int num = Physics.RaycastNonAlloc(_start, val.normalized, hits, val.magnitude, LayerMask.op_Implicit(SemiFunc.LayerMaskGetVisionObstruct()), (QueryTriggerInteraction)1);
		if (num == hits.Length)
		{
			return true;
		}
		for (int i = 0; i < num; i++)
		{
			Collider collider = hits[i].collider;
			if (!IgnoreCollider(collider))
			{
				PhysGrabObject physGrabObject = ColliderTargetGet(collider);
				if (!Object.op_Implicit((Object)(object)physGrabObject) || (!((Object)(object)physGrabObject == (Object)(object)target) && !((Object)(object)physGrabObject == (Object)(object)_allowedTarget)))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void InterruptionTick()
	{
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Idle || currentState == State.Retracting)
		{
			return;
		}
		if (!HolderValid(holder) || (!Object.op_Implicit((Object)(object)target) && (targetViewID != 0 || (currentState != State.Casting && currentState != State.Backswing && currentState != State.Cracked))))
		{
			StateSet(State.Retracting);
		}
		else
		{
			if (!Object.op_Implicit((Object)(object)target))
			{
				return;
			}
			if (!TargetValid(target) || target.playerGrabbing.Count > 0 || (target.isPlayer && currentState != State.Catching && !PlayerTargetReady(target)) || (currentState == State.Catching && Vector3.Distance(origin.position, target.centerPoint) > reach + 1f))
			{
				StateSet(State.Retracting);
				return;
			}
			if (Obstructed(target.centerPoint))
			{
				obstructionTimer += Time.deltaTime;
			}
			else
			{
				obstructionTimer = 0f;
			}
			if (obstructionTimer >= blockedTime)
			{
				StateSet(State.Retracting);
			}
		}
	}

	private void TetherPhysics()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		if (tetherLength <= 0f)
		{
			return;
		}
		Vector3 val = origin.position - target.centerPoint;
		float magnitude = val.magnitude;
		float num = magnitude - tetherLength;
		if (TetherOverstretched(num))
		{
			return;
		}
		target.NetworkTrackerOverride();
		if (target.isEnemy)
		{
			target.enemyRigidbody.enemy.StateStunned.Set(0.2f);
		}
		TetherVelocityTick();
		if (!(magnitude <= 0.001f))
		{
			Vector3 val2 = val / magnitude;
			val2.y = 0f;
			if (!(num <= 0f) && !Obstructed(target.centerPoint))
			{
				float separatingSpeed = Vector3.Dot(physGrabObject.rb.GetPointVelocity(origin.position) - target.rb.linearVelocity, val2);
				float forceMax = Mathf.Min(tetherForceMax, target.rb.mass * tetherAccelerationMax);
				float num2 = ItemMeleeWhipTether.PullForce(num, separatingSpeed, target.rb.mass, Time.fixedDeltaTime, tetherSpring, tetherDamping, forceMax);
				target.rb.AddForce(val2 * num2, (ForceMode)0);
				tetherVelocity += val2 * (num2 / target.rb.mass * Time.fixedDeltaTime);
			}
		}
	}

	private void TetherVelocityTick()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		float magnitude = tetherVelocity.magnitude;
		if (magnitude <= 0.0001f)
		{
			tetherVelocity = Vector3.zero;
			return;
		}
		Vector3 val = tetherVelocity / magnitude;
		float num = Mathf.Max(0f, Vector3.Dot(target.rb.linearVelocity, val));
		tetherVelocity = val * (Mathf.Min(magnitude, num) * Mathf.Exp(0f - Time.fixedDeltaTime));
	}

	private void TetherVelocityCancel()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = tetherVelocity;
		tetherVelocity = Vector3.zero;
		if (!Object.op_Implicit((Object)(object)target) || !Object.op_Implicit((Object)(object)target.rb) || target.rb.isKinematic || target.playerGrabbing.Count > 0)
		{
			return;
		}
		float magnitude = val.magnitude;
		if (!(magnitude <= 0.01f))
		{
			Vector3 val2 = val / magnitude;
			float num = Mathf.Clamp(Vector3.Dot(target.rb.linearVelocity, val2), 0f, magnitude);
			if (!(num <= 0f))
			{
				Rigidbody rb = target.rb;
				rb.linearVelocity -= val2 * num;
				target.NetworkTrackerOverride(0.5f);
			}
		}
	}

	private float TargetDifficultyMultiplierGet()
	{
		if (!target.isEnemy)
		{
			return 1f;
		}
		return target.enemyRigidbody.enemy.EnemyParent.difficulty switch
		{
			EnemyParent.Difficulty.Difficulty1 => 0.65f, 
			EnemyParent.Difficulty.Difficulty3 => 1.35f, 
			_ => 1f, 
		};
	}

	private bool TetherOverstretched(float _extension)
	{
		if (_extension > tetherStretchMax)
		{
			stretchTimer += Time.fixedDeltaTime * TargetDifficultyMultiplierGet();
		}
		else
		{
			stretchTimer = 0f;
		}
		if (stretchTimer < blockedTime && _extension <= tetherStretchMax * 2f)
		{
			return false;
		}
		StateSet(State.Retracting);
		return true;
	}

	private void ReleasePhysics()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		float num = FlingSpeedGet();
		if (target.isPlayer)
		{
			((Component)target).GetComponent<PlayerTumble>().OverrideTumbleHold(releaseTime);
		}
		target.rb.linearVelocity = flingDirection * num;
		target.NetworkTrackerOverride(0.5f);
		if (Object.op_Implicit((Object)(object)battery) && durabilityPerFling > 0f && !SemiFunc.RunIsShop())
		{
			float num2 = (target.isEnemy ? TargetDifficultyMultiplierGet() : 0.13f);
			float num3 = durabilityPerFling * num2;
			battery.batteryLife = Mathf.Max(0f, battery.batteryLife - num3);
			battery.TriggerSmallBatteryDeduction();
		}
		StateSet(State.Retracting, _playFling: true);
	}

	private void CrackHitWindowStart()
	{
		crackHitSequence = sequence;
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		for (int num = crackHitWindows.Count - 1; num >= 0; num--)
		{
			if (crackHitWindows[num].expires <= Time.time)
			{
				crackHitWindows.RemoveAt(num);
			}
		}
		crackHitWindows.Add(new CrackHitWindow
		{
			sequence = crackHitSequence,
			expires = Time.time + Mathf.Max(3f, crackHurtTime),
			durability = durabilityPerCrackHit,
			holder = holder
		});
	}

	private void CrackHit()
	{
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		CrackDurabilityUse(crackHitSequence, null, 1f);
		if (currentState != State.Cracked || Object.op_Implicit((Object)(object)target) || Object.op_Implicit((Object)(object)crackHitTarget) || crackHitSequence != sequence)
		{
			return;
		}
		Enemy onImpactEnemyEnemy = crackHurtCollider.onImpactEnemyEnemy;
		if (Object.op_Implicit((Object)(object)onImpactEnemyEnemy) && onImpactEnemyEnemy.HasRigidbody)
		{
			PhysGrabObject physGrabObject = onImpactEnemyEnemy.Rigidbody.physGrabObject;
			if (TargetValid(physGrabObject) && physGrabObject.playerGrabbing.Count <= 0 && !Obstructed(castPosition, physGrabObject))
			{
				crackHitTarget = physGrabObject;
			}
		}
	}

	private bool CrackHitTargetCatch()
	{
		if (!TargetValid(crackHitTarget) || crackHitTarget.playerGrabbing.Count > 0)
		{
			return false;
		}
		target = crackHitTarget;
		targetViewID = (Object.op_Implicit((Object)(object)target.photonView) ? target.photonView.ViewID : 0);
		StateSet(State.Catching);
		return true;
	}

	private void CrackPhysObjectHit()
	{
		CrackDurabilityUse(crackHitSequence, null, 0.2f);
	}

	private void CrackPlayerHit()
	{
		PlayerAvatar onImpactPlayerAvatar = crackHurtCollider.onImpactPlayerAvatar;
		if (Object.op_Implicit((Object)(object)onImpactPlayerAvatar) && crackHitSequence >= 0 && crackPlayerReportSequence != crackHitSequence && (!SemiFunc.IsMultiplayer() || onImpactPlayerAvatar.photonView.IsMine))
		{
			crackPlayerReportSequence = crackHitSequence;
			if (!Object.op_Implicit((Object)(object)onImpactPlayerAvatar.tumble) || (Object)(object)onImpactPlayerAvatar.tumble.physGrabObject != (Object)(object)target)
			{
				whip.PlayerCameraWrapOverride(onImpactPlayerAvatar, crackHurtCollider.playerStunTime);
			}
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				CrackDurabilityUse(crackHitSequence, onImpactPlayerAvatar, 0.2f);
				return;
			}
			photonView.RPC("CrackPlayerHitRPC", (RpcTarget)2, new object[2]
			{
				crackHitSequence,
				onImpactPlayerAvatar.photonView.ViewID
			});
		}
	}

	private void CrackDurabilityUse(int _sequence, PlayerAvatar _player, float _multiplier)
	{
		if (!((Behaviour)this).isActiveAndEnabled || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		foreach (CrackHitWindow crackHitWindow in crackHitWindows)
		{
			if (crackHitWindow.sequence != _sequence || crackHitWindow.expires <= Time.time || crackHitWindow.durability <= 0f)
			{
				continue;
			}
			if (!Object.op_Implicit((Object)(object)_player) || !((Object)(object)_player == (Object)(object)crackHitWindow.holder))
			{
				float num = crackHitWindow.durability * _multiplier;
				crackHitWindow.durability = 0f;
				if (Object.op_Implicit((Object)(object)battery) && !SemiFunc.RunIsShop())
				{
					battery.batteryLife = Mathf.Max(0f, battery.batteryLife - num);
					battery.TriggerSmallBatteryDeduction();
				}
			}
			break;
		}
	}

	private AudioSource SoundPlay(Sound _sound, Transform _target)
	{
		if (_sound == null || _sound.Sounds == null || _sound.Sounds.Length == 0)
		{
			return null;
		}
		return _sound.Play(_target);
	}

	private void CrackSoundPlay()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (soundCrack != null && soundCrack.Sounds != null && soundCrack.Sounds.Length != 0)
		{
			soundCrack.Play(castPosition);
		}
	}

	private void CrackImpactPlay()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		castPoint.position = castPosition;
		GameDirector.instance.CameraShake.ShakeDistance(1.5f, 2f, 8f, castPosition, 0.1f);
		GameDirector.instance.CameraImpact.ShakeDistance(3f, 2f, 8f, castPosition, 0.05f);
		if (Object.op_Implicit((Object)(object)crackEffect))
		{
			ParticleSystem val = Object.Instantiate<ParticleSystem>(crackEffect, castPosition, Quaternion.LookRotation(flingDirection));
			((Component)val).gameObject.SetActive(true);
			val.Play(true);
			Object.Destroy((Object)(object)((Component)val).gameObject, 3f);
		}
		if (Object.op_Implicit((Object)(object)crackHurtCollider))
		{
			((Component)crackHurtCollider).gameObject.SetActive(false);
			crackHurtCollider.ignoreObjects.Clear();
			crackHurtCollider.ignoreObjects.Add(physGrabObject);
			crackHurtCollider.ignorePlayers.Clear();
			if (Object.op_Implicit((Object)(object)holder))
			{
				crackHurtCollider.ignorePlayers.Add(holder);
			}
			crackHurtCollider.playerCausingHurtOverride = holder;
			crackHurtCollider.timer = crackHurtTime;
			bool flag = SemiFunc.RunIsArena() || SemiFunc.RunIsShop();
			crackHurtCollider.playerLogic = true;
			crackHurtCollider.playerDamage = (flag ? crackPlayerDamage : 0);
			crackHurtCollider.playerKill = flag && crackPlayerKill;
			CrackHitWindowStart();
			((Component)crackHurtCollider).transform.SetPositionAndRotation(castPosition, Quaternion.LookRotation(flingDirection));
			((Component)crackHurtCollider).gameObject.SetActive(true);
		}
	}

	private void StateSound(State _state, State _previousState, bool _playFling)
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		Transform val = (Object.op_Implicit((Object)(object)target) ? ((Component)target).transform : origin);
		switch (_state)
		{
		case State.Casting:
			SoundPlay(soundCast, origin);
			melee.SwingFeedback();
			break;
		case State.Catching:
		case State.Cracked:
			if (_previousState != State.Cracked)
			{
				CrackSoundPlay();
			}
			break;
		case State.Holding:
			if (_previousState == State.Catching)
			{
				SoundPlay(soundCatch, val);
			}
			break;
		case State.Retracting:
			if (_playFling)
			{
				SoundPlay(soundFling, val);
				Vector3 position = (Object.op_Implicit((Object)(object)target) ? target.centerPoint : origin.position);
				GameDirector.instance.CameraShake.ShakeDistance(2f, 2f, 8f, position, 0.1f);
				GameDirector.instance.CameraImpact.ShakeDistance(4f, 2f, 8f, position, 0.05f);
			}
			SoundPlay(soundRetract, origin);
			break;
		case State.Windup:
		case State.Releasing:
		case State.Backswing:
		case State.HoldingBackswing:
			break;
		}
	}

	private void CocoonSoundTick()
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		if (cocoonSoundIndex < 0)
		{
			return;
		}
		if (!whip.CocoonWrapTryGet(target, out var _progress, out var _soundTarget, out var _soundPosition))
		{
			if (cocoonSoundIndex > 0)
			{
				cocoonSoundIndex = -1;
			}
		}
		else if (!(_progress + 0.0001f < CocoonSoundProgress(cocoonSoundIndex)))
		{
			float pitchMultiplier = Mathf.Lerp(1f, 2f, (float)cocoonSoundIndex / 3f);
			CocoonSoundPlay(_soundTarget, _soundPosition, pitchMultiplier);
			cocoonSoundIndex++;
			if (cocoonSoundIndex >= 4)
			{
				cocoonSoundIndex = -1;
			}
		}
	}

	private static float CocoonSoundProgress(int _index)
	{
		float num = 1f - (float)_index / 3f;
		return 1f - num * num;
	}

	private void CocoonSoundPlay(Transform _target, Vector3 _position, float _pitchMultiplier)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (!SoundHasClips(soundSwing))
		{
			return;
		}
		float pitch = soundSwing.Pitch;
		float pitchRandom = soundSwing.PitchRandom;
		soundSwing.Pitch = pitch * _pitchMultiplier;
		soundSwing.PitchRandom = 0f;
		try
		{
			soundSwing.Play(_target, _position);
		}
		finally
		{
			soundSwing.Pitch = pitch;
			soundSwing.PitchRandom = pitchRandom;
		}
	}

	private void HoldSoundTick()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		if (soundHoldLoop == null || soundHoldLoop.Sounds == null || soundHoldLoop.Sounds.Length == 0)
		{
			return;
		}
		bool flag = Object.op_Implicit((Object)(object)target) && tetherLength > 0f && (currentState == State.Holding || currentState == State.HoldingBackswing || currentState == State.Windup);
		float extension = (flag ? (Vector3.Distance(origin.position, target.centerPoint) - tetherLength) : (-1f));
		float num = (flag ? Mathf.Lerp(1f, 2f, HoldSoundTensionGet(extension)) : 1f);
		holdSoundPitch = Mathf.MoveTowards(holdSoundPitch, num, Time.deltaTime * 4f);
		Vector3 position = whip.MiddlePositionGet();
		if (flag && !Object.op_Implicit((Object)(object)soundHoldLoop.Source))
		{
			soundHoldLoop.LoopSourceCreate(position);
		}
		if (Object.op_Implicit((Object)(object)soundHoldLoop.Source))
		{
			((Component)soundHoldLoop.Source).transform.position = position;
			soundHoldLoop.PlayLoop(flag, 2f, 4f, holdSoundPitch);
			if (!flag && !soundHoldLoop.Source.isPlaying)
			{
				HoldSoundStop();
			}
		}
	}

	private float HoldSoundTensionGet(float _extension)
	{
		float num = Mathf.Clamp01(_extension / Mathf.Max(0.01f, tetherStretchMax));
		float num2 = 0f;
		if (currentState == State.HoldingBackswing)
		{
			num2 = 0.35f * Mathf.Clamp01(stateTimer / 0.15f);
		}
		if (currentState == State.Windup)
		{
			float num3 = Mathf.Clamp01(FlingWaveElapsedGet() / Mathf.Max(0.01f, flingWaveDuration));
			num2 = Mathf.Lerp(0.35f, flingPower, num3);
		}
		return Mathf.Max(num, num2);
	}

	private void HoldSoundStop()
	{
		holdSoundPitch = 1f;
		if (soundHoldLoop != null && Object.op_Implicit((Object)(object)soundHoldLoop.Source))
		{
			AudioSource source = soundHoldLoop.Source;
			soundHoldLoop.LoopSourceSet(null);
			Object.Destroy((Object)(object)((Component)source).gameObject);
		}
	}

	private void MoveSoundTick()
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		if (moveSoundTimer > 0f)
		{
			moveSoundTimer -= Time.deltaTime;
		}
		if (swingSoundTimer > 0f)
		{
			swingSoundTimer -= Time.deltaTime;
		}
		if (!Object.op_Implicit((Object)(object)fakeVelocities))
		{
			return;
		}
		if (!physGrabObject.grabbed || physGrabObject.playerGrabbing.Count == 0 || currentState == State.Casting || currentState == State.Cracked || currentState == State.Catching || currentState == State.Windup || currentState == State.Releasing)
		{
			moveSoundReady = true;
			swingSoundReady = true;
			return;
		}
		Vector3 val = fakeVelocities.velocity;
		PhysGrabber physGrabber = physGrabObject.playerGrabbing[0];
		if (Object.op_Implicit((Object)(object)physGrabber) && Object.op_Implicit((Object)(object)physGrabber.playerAvatar))
		{
			val -= physGrabber.playerAvatar.rbVelocityRaw;
		}
		float magnitude = val.magnitude;
		if (magnitude < swingSoundSpeed * 0.6f || Vector3.Angle(val, swingSoundVelocity) > 90f)
		{
			swingSoundReady = true;
		}
		if (magnitude < moveSoundSpeed * 0.6f || Vector3.Angle(val, moveSoundVelocity) > 100f)
		{
			moveSoundReady = true;
		}
		if (swingSoundReady && swingSoundTimer <= 0f && magnitude >= swingSoundSpeed && SoundHasClips(soundSwing))
		{
			swingSoundReady = false;
			swingSoundTimer = swingSoundCooldown;
			swingSoundVelocity = val;
			moveSoundReady = false;
			moveSoundTimer = moveSoundCooldown;
			moveSoundVelocity = val;
			float volumeMultiplier = Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(swingSoundSpeed, Mathf.Max(swingSoundSpeed, swingSoundSpeedMax), magnitude));
			soundSwing.Play(origin.position, volumeMultiplier);
		}
		else if (moveSoundReady && !(magnitude < moveSoundSpeed) && !(moveSoundTimer > 0f) && SoundHasClips(soundMove))
		{
			moveSoundReady = false;
			moveSoundTimer = moveSoundCooldown;
			moveSoundVelocity = val;
			float volumeMultiplier2 = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(moveSoundSpeed, Mathf.Max(moveSoundSpeed, moveSoundSpeedMax), magnitude));
			soundMove.Play(origin.position, volumeMultiplier2);
		}
	}

	private static bool SoundHasClips(Sound _sound)
	{
		if (_sound != null && _sound.Sounds != null)
		{
			return _sound.Sounds.Length != 0;
		}
		return false;
	}

	private void BackswingVisualStart(Vector3 _direction)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		backswingVisualTimer = 1.25f;
		backswingImpulseTimer = 0.1f;
		backswingVisualDirection = _direction;
		if (currentState != State.Holding && currentState != State.HoldingBackswing)
		{
			whip.AddImpulseAt(0.2f, _direction * 3f, 0.25f);
		}
	}

	private void BackswingVisualStop()
	{
		backswingVisualTimer = 0f;
		backswingImpulseTimer = 0f;
		whip.OverrideAttachedWaveReset();
	}

	private void BackswingVisualTick()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		if (backswingVisualTimer <= 0f)
		{
			return;
		}
		if (currentState == State.Holding || currentState == State.HoldingBackswing)
		{
			whip.OverrideAttachedWave(backswingVisualDirection * flingBendSize * 0.35f, 0.12f);
			backswingVisualTimer = Mathf.Max(0f, backswingVisualTimer - Time.deltaTime);
			return;
		}
		if (backswingImpulseTimer > 0f)
		{
			backswingImpulseTimer -= Time.deltaTime;
			if (backswingImpulseTimer <= 0f)
			{
				whip.AddImpulseAt(0.65f, backswingVisualDirection * 4f, 0.3f);
			}
		}
		backswingVisualTimer = Mathf.Max(0f, backswingVisualTimer - Time.deltaTime);
	}

	private void FlingWaveVisualTick()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		if (currentState == State.Windup && Object.op_Implicit((Object)(object)target))
		{
			float progress = FlingWaveElapsedGet() / Mathf.Max(0.01f, flingWaveDuration);
			float num = Vector3.Distance(origin.position, target.centerPoint);
			float num2 = Mathf.Min(flingBendSize, num * 0.25f) * flingPower;
			whip.OverrideAttachedWave(backswingDirection * num2, progress);
		}
	}

	private void TetherVisualTick()
	{
		float num = ((backswingVisualTimer > 0f) ? (restingTension * 0.4f) : restingTension);
		if (Object.op_Implicit((Object)(object)target) && (currentState == State.Catching || currentState == State.Holding || currentState == State.HoldingBackswing || currentState == State.Windup))
		{
			num = heldTension;
		}
		whip.SetTension(Mathf.MoveTowards(whip.tension, num, Time.deltaTime * 6f));
	}

	private void ReferenceTick()
	{
		if (!Object.op_Implicit((Object)(object)target) && targetViewID > 0 && SemiFunc.PerSecond(10f, (Object)(object)this))
		{
			PhotonView val = PhotonView.Find(targetViewID);
			if (Object.op_Implicit((Object)(object)val))
			{
				target = ((Component)val).GetComponent<PhysGrabObject>();
			}
		}
	}

	private void VisualTick()
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (!Object.op_Implicit((Object)(object)whip) || !Object.op_Implicit((Object)(object)origin))
		{
			return;
		}
		TetherVisualTick();
		BackswingVisualTick();
		FlingWaveVisualTick();
		castPoint.position = castPosition;
		if (currentState == State.Casting || currentState == State.Cracked)
		{
			CastVisualTick();
		}
		else
		{
			if (currentState != State.Catching && currentState != State.Holding && currentState != State.HoldingBackswing && currentState != State.Windup)
			{
				return;
			}
			if (Object.op_Implicit((Object)(object)target) && target.isPlayer)
			{
				if (PlayerTargetReady(target))
				{
					whip.ShootToPoint(((Component)target).transform);
				}
			}
			else if (Object.op_Implicit((Object)(object)target))
			{
				whip.ShootToPhysObject(target);
			}
			else
			{
				whip.SpringIn();
			}
		}
	}

	private void CastVisualTick()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			whip.CrackToPoint(castPoint, crackTime, backswingDirection);
			if (pendingCastSequence == sequence && currentState == State.Casting && whip.CrackPathTryGet(castPoint, out castPath))
			{
				castTimestamp = PhotonNetwork.ServerTimestamp - Mathf.RoundToInt(Time.deltaTime * 1000f);
				StatePublish(_playFling: false);
			}
		}
		else if (!(castPath.duration < 0.1f))
		{
			float elapsed = ItemMeleeWhipSnapshot.CastElapsed(PhotonNetwork.ServerTimestamp, castTimestamp, castPath.duration);
			whip.NetworkCrackToPoint(castPoint, castPath, elapsed);
		}
	}

	[PunRPC]
	private void CrackPlayerHitRPC(int _sequence, int _playerViewID, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_playerViewID);
			if (Object.op_Implicit((Object)(object)playerAvatar) && SemiFunc.OwnerOnlyRPC(_info, playerAvatar.photonView))
			{
				CrackDurabilityUse(_sequence, playerAvatar, 0.2f);
			}
		}
	}

	[PunRPC]
	private void UseRPC(int _playerViewID, int _gestureId, int _sequence, int _action, Vector3 _aim, Vector3 _backswing, float _power, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromPhotonID(_playerViewID);
			if (Object.op_Implicit((Object)(object)playerAvatar) && SemiFunc.OwnerOnlyRPC(_info, playerAvatar.photonView) && _action >= 0 && (_action <= 2 || _action == 4))
			{
				GestureApply(playerAvatar, _gestureId, _sequence, (GestureAction)_action, _aim, _backswing, _power);
			}
		}
	}

	private void StatePublish(bool _playFling)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		ItemMeleeWhipSnapshot itemMeleeWhipSnapshot = new ItemMeleeWhipSnapshot
		{
			sequence = sequence,
			state = currentState,
			targetViewID = targetViewID,
			holderViewID = holderViewID,
			castPosition = castPosition,
			flingDirection = flingDirection,
			backswingDirection = backswingDirection,
			tetherLength = tetherLength,
			playFling = _playFling,
			castTimestamp = castTimestamp,
			castPath = castPath,
			flingPower = flingPower,
			flingWaveDuration = flingWaveDuration,
			flingWaveTimestamp = flingWaveTimestamp
		};
		networkState = itemMeleeWhipSnapshot.Encode();
		publishedSequence = sequence;
		pendingCastSequence = -1;
		photonView.RPC("StateRPC", (RpcTarget)1, new object[2] { networkState, false });
	}

	[PunRPC]
	private void StateRPC(byte[] _data, bool _recovery, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		if (!((Behaviour)this).isActiveAndEnabled || !SemiFunc.MasterOnlyRPC(_info) || !ItemMeleeWhipSnapshot.TryDecode(_data, out var _snapshot) || _snapshot.sequence <= sequence)
		{
			return;
		}
		PhotonView val = ((_snapshot.targetViewID > 0) ? PhotonView.Find(_snapshot.targetViewID) : null);
		PhysGrabObject physGrabObject = (Object.op_Implicit((Object)(object)val) ? ((Component)val).GetComponent<PhysGrabObject>() : null);
		PlayerAvatar playerAvatar = ((_snapshot.holderViewID > 0) ? SemiFunc.PlayerAvatarGetFromPhotonID(_snapshot.holderViewID) : null);
		targetViewID = _snapshot.targetViewID;
		holderViewID = _snapshot.holderViewID;
		if (_snapshot.state == State.Windup)
		{
			flingPower = _snapshot.flingPower;
			flingWaveDuration = _snapshot.flingWaveDuration;
			flingWaveTimestamp = _snapshot.flingWaveTimestamp;
		}
		if (_snapshot.state == State.Casting)
		{
			castPath = _snapshot.castPath;
			castTimestamp = _snapshot.castTimestamp;
		}
		else if (_recovery)
		{
			castPath = default;
		}
		if (_recovery)
		{
			BackswingVisualStop();
			if (Object.op_Implicit((Object)(object)crackHurtCollider))
			{
				((Component)crackHurtCollider).gameObject.SetActive(false);
			}
		}
		StateApply(_snapshot.sequence, _snapshot.state, physGrabObject, playerAvatar, _snapshot.HasImpact ? _snapshot.castPosition : castPosition, (_snapshot.HasImpact || _snapshot.state == State.Windup) ? _snapshot.flingDirection : flingDirection, _snapshot.HasBackswing ? _snapshot.backswingDirection : backswingDirection, _snapshot.HasTether ? _snapshot.tetherLength : tetherLength, _snapshot.playFling, _recovery);
		if (_snapshot.state == State.Windup)
		{
			stateTimer = ItemMeleeWhipSnapshot.CastElapsed(PhotonNetwork.ServerTimestamp, flingWaveTimestamp, flingWaveDuration);
		}
	}

	[PunRPC]
	private void SnapshotRequestRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || !SemiFunc.IsMultiplayer() || _info.Sender == null || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.GetPlayer(_info.Sender.ActorNumber, false) != _info.Sender || !((Behaviour)this).isActiveAndEnabled || pendingCastSequence == sequence)
		{
			return;
		}
		if (networkState == null)
		{
			if (currentState != State.Idle)
			{
				return;
			}
			networkState = new ItemMeleeWhipSnapshot
			{
				sequence = sequence,
				state = State.Idle
			}.Encode();
		}
		int actorNumber = _info.Sender.ActorNumber;
		if (!recoveryReplyTimes.TryGetValue(actorNumber, out var value) || !(Time.unscaledTime < value))
		{
			recoveryReplyTimes[actorNumber] = Time.unscaledTime + 1f;
			photonView.RPC("StateRPC", _info.Sender, new object[2] { networkState, true });
		}
	}

	private void RecoveryTick()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer() && sequence < recoverySequence && PhotonNetwork.InRoom && photonView.ViewID > 0 && !(Time.unscaledTime < recoveryRequestTime))
		{
			recoveryRequestTime = Time.unscaledTime + 1f;
			photonView.RPC("SnapshotRequestRPC", (RpcTarget)2, Array.Empty<object>());
		}
	}

	public void OnPhotonSerializeView(PhotonStream _stream, PhotonMessageInfo _info)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (_stream.IsWriting)
		{
			_stream.SendNext((object)publishedSequence);
			return;
		}
		int num = (int)_stream.ReceiveNext();
		if (SemiFunc.MasterOnlyRPC(_info) && num > sequence && ((Behaviour)this).isActiveAndEnabled)
		{
			recoverySequence = Mathf.Max(recoverySequence, num);
		}
	}

	public ItemMeleeWhip()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
	}
}
