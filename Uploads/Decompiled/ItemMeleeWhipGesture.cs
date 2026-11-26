using UnityEngine;

internal sealed class ItemMeleeWhipGesture
{
	internal enum Action
	{
		None,
		Prepare,
		Strike,
		Cancel
	}

	private enum State
	{
		Uninitialized,
		Ready,
		Backswing,
		Armed,
		ForwardSwing,
		Recovery
	}

	internal const float PreparationTime = 1.15f;

	private const float RestSpeed = 25f;

	private const float RestTimeout = 0.25f;

	private const float StartSpeed = 60f;

	private const float BackswingAngleMin = 6.4f;

	private const float BackswingTimeMax = 0.35f;

	private const float PeakHold = 0.85f;

	private const float ReturnFraction = 0.5f;

	private const float ReturnAngleMin = 4f;

	private const float ReturnTimeMax = 0.45f;

	private const float ForwardSwingTimeMax = 0.6f;

	private const float ForwardStopTime = 0.04f;

	private const float GamepadBackswingAngleMin = 4.8f;

	private const float GamepadReturnAngleMin = 3f;

	private State currentState;

	private Vector3 previousAim;

	private Vector3 previousRight;

	private Vector3 previousUp;

	private Vector3 restAim;

	private Vector3 restRight;

	private Vector3 restUp;

	private Vector2 restTravel;

	private float restTimer;

	private Vector3 capturedAim;

	private Vector3 capturedRight;

	private Vector3 capturedUp;

	private Vector3 backswingDirection;

	private Vector2 strokeTravel;

	private Vector2 peakTravel;

	private float reverseTravel;

	private float stopTimer;

	private float stateTimer;

	private float forwardTimer;

	private float forwardPeakSpeed;

	private bool throwRequested;

	private bool throwMode;

	private bool previousGamepad;

	private float BackswingAngleRequired
	{
		get
		{
			if (!previousGamepad)
			{
				return 6.4f;
			}
			return 4.8f;
		}
	}

	private float ReturnAngleRequired
	{
		get
		{
			if (!previousGamepad)
			{
				return 4f;
			}
			return 3f;
		}
	}

	internal bool Prepared
	{
		get
		{
			if (currentState != State.Armed)
			{
				return currentState == State.ForwardSwing;
			}
			return true;
		}
	}

	internal void Reset()
	{
		currentState = State.Uninitialized;
		stateTimer = 0f;
	}

	internal static bool DirectionValid(Vector3 _direction)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (float.IsFinite(_direction.x) && float.IsFinite(_direction.y) && float.IsFinite(_direction.z) && float.IsFinite(_direction.sqrMagnitude))
		{
			return _direction.sqrMagnitude > 0.0001f;
		}
		return false;
	}

	internal static Vector2 AngularDeltaGet(Vector2 _input, bool _gamepad, float _deltaTime)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (!float.IsFinite(_input.x) || !float.IsFinite(_input.y) || !float.IsFinite(_input.sqrMagnitude) || !float.IsFinite(_deltaTime) || _deltaTime <= 0f)
		{
			return Vector2.zero;
		}
		if (_gamepad)
		{
			if (_input.magnitude < 0.22f)
			{
				return Vector2.zero;
			}
			_input = Vector2.ClampMagnitude(_input, 1f);
		}
		Vector2 val = _input * 180f;
		if (val.magnitude < 12f)
		{
			return Vector2.zero;
		}
		return val * _deltaTime;
	}

	internal Action Tick(Vector3 _aim, Vector3 _right, Vector3 _up, Vector2 _input, bool _gamepad, bool _throw, float _deltaTime, out Vector3 _capturedAim, out Vector3 _backswing)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		_capturedAim = capturedAim;
		_backswing = backswingDirection;
		throwRequested = _throw;
		if (!DirectionValid(_aim) || !DirectionValid(_right) || !DirectionValid(_up) || !float.IsFinite(_input.x) || !float.IsFinite(_input.y) || !float.IsFinite(_input.sqrMagnitude) || !float.IsFinite(_deltaTime) || _deltaTime <= 0f || _deltaTime > 0.25f)
		{
			return Cancel();
		}
		Vector3 oldAim = previousAim;
		Vector3 oldRight = previousRight;
		Vector3 oldUp = previousUp;
		previousAim = _aim.normalized;
		previousRight = _right.normalized;
		previousUp = _up.normalized;
		if (currentState == State.Uninitialized)
		{
			previousGamepad = _gamepad;
			RestSet(previousAim, previousRight, previousUp);
			currentState = State.Ready;
			return Action.None;
		}
		if (previousGamepad != _gamepad)
		{
			return Cancel();
		}
		Vector2 delta = AngularDeltaGet(_input, _gamepad, _deltaTime);
		if (delta.magnitude > 75f)
		{
			return Cancel();
		}
		Action action = StateTick(oldAim, oldRight, oldUp, delta, _deltaTime);
		_capturedAim = ((action == Action.Strike && throwMode) ? previousAim : capturedAim);
		_backswing = backswingDirection;
		return action;
	}

	private Action Cancel()
	{
		bool prepared = Prepared;
		Reset();
		if (!prepared)
		{
			return Action.None;
		}
		return Action.Cancel;
	}

	private void RestSet(Vector3 _aim, Vector3 _right, Vector3 _up)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		restAim = _aim;
		restRight = _right;
		restUp = _up;
		restTravel = Vector2.zero;
		restTimer = 0f;
	}

	private void ReadySet()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		RestSet(previousAim, previousRight, previousUp);
		currentState = State.Ready;
	}

	private Action StateTick(Vector3 _oldAim, Vector3 _oldRight, Vector3 _oldUp, Vector2 _delta, float _deltaTime)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		switch (currentState)
		{
		case State.Ready:
			return StateReady(_oldAim, _oldRight, _oldUp, _delta, _deltaTime);
		case State.Backswing:
			return StateBackswing(_delta, _deltaTime);
		case State.Armed:
			return StateArmed(_delta, _deltaTime);
		case State.ForwardSwing:
			return StateForwardSwing(_delta, _deltaTime);
		case State.Recovery:
			stateTimer -= _deltaTime;
			if (stateTimer <= 0f)
			{
				ReadySet();
			}
			break;
		}
		return Action.None;
	}

	private Action StateReady(Vector3 _oldAim, Vector3 _oldRight, Vector3 _oldUp, Vector2 _delta, float _deltaTime)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		float num = _delta.magnitude / _deltaTime;
		if (num < 25f)
		{
			RestSet(previousAim, previousRight, previousUp);
			return Action.None;
		}
		if (restTimer >= 0.25f)
		{
			RestSet(_oldAim, _oldRight, _oldUp);
		}
		restTravel += _delta;
		restTimer += _deltaTime;
		if (num < 60f)
		{
			return Action.None;
		}
		capturedAim = restAim;
		capturedRight = restRight;
		capturedUp = restUp;
		strokeTravel = restTravel;
		peakTravel = strokeTravel;
		reverseTravel = 0f;
		stopTimer = 0f;
		stateTimer = 0f;
		throwMode = throwRequested;
		currentState = State.Backswing;
		if (!(peakTravel.magnitude >= BackswingAngleRequired))
		{
			return Action.None;
		}
		return Arm();
	}

	private Action StateBackswing(Vector2 _delta, float _deltaTime)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		stateTimer += _deltaTime;
		strokeTravel += _delta;
		if (strokeTravel.sqrMagnitude > peakTravel.sqrMagnitude)
		{
			peakTravel = strokeTravel;
		}
		if (peakTravel.magnitude >= BackswingAngleRequired)
		{
			return Arm();
		}
		if (stateTimer > 0.35f)
		{
			ReadySet();
		}
		return Action.None;
	}

	private Action Arm()
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		backswingDirection = BackswingDirectionGet(peakTravel);
		reverseTravel = 0f;
		stopTimer = 0f;
		stateTimer = 0f;
		currentState = State.Armed;
		return Action.Prepare;
	}

	private Action StateArmed(Vector2 _delta, float _deltaTime)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		stateTimer += _deltaTime;
		if (stateTimer >= 1.15f)
		{
			ReadySet();
			return Action.Cancel;
		}
		strokeTravel += _delta;
		float magnitude = strokeTravel.magnitude;
		float num = peakTravel.magnitude;
		if (magnitude > num)
		{
			peakTravel = strokeTravel;
			num = magnitude;
			backswingDirection = BackswingDirectionGet(peakTravel);
		}
		if (magnitude >= num * 0.85f)
		{
			stopTimer = 0f;
		}
		else
		{
			stopTimer += _deltaTime;
		}
		if (throwMode)
		{
			reverseTravel = ((num > 0.0001f) ? Vector2.Dot(peakTravel - strokeTravel, peakTravel / num) : 0f);
			if (stopTimer > 0.45f || reverseTravel < ReturnAngleRequired)
			{
				return Action.None;
			}
			forwardTimer = 0f;
			forwardPeakSpeed = _delta.magnitude / _deltaTime;
			stopTimer = 0f;
			currentState = State.ForwardSwing;
			return Action.None;
		}
		reverseTravel = num - magnitude;
		if (stopTimer > 0.45f)
		{
			return Action.None;
		}
		if (magnitude > num * 0.5f || reverseTravel < ReturnAngleRequired)
		{
			return Action.None;
		}
		return Strike();
	}

	private Action StateForwardSwing(Vector2 _delta, float _deltaTime)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		stateTimer += _deltaTime;
		forwardTimer += _deltaTime;
		strokeTravel += _delta;
		float num = _delta.magnitude / _deltaTime;
		forwardPeakSpeed = Mathf.Max(forwardPeakSpeed, num);
		if (stateTimer >= 1.15f || forwardTimer >= 0.6f)
		{
			return Strike();
		}
		if (num > Mathf.Clamp(forwardPeakSpeed * 0.2f, 20f, 60f))
		{
			stopTimer = 0f;
			return Action.None;
		}
		stopTimer += _deltaTime;
		if (!(stopTimer >= 0.04f))
		{
			return Action.None;
		}
		return Strike();
	}

	private Action Strike()
	{
		currentState = State.Recovery;
		stateTimer = 0.3f;
		return Action.Strike;
	}

	private Vector3 BackswingDirectionGet(Vector2 _travel)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		Vector3 direction = capturedRight * _travel.x + capturedUp * _travel.y;
		if (!DirectionValid(direction))
		{
			return capturedUp;
		}
		return direction.normalized;
	}
}
