using UnityEngine;

internal sealed class ItemMeleeWhipFlingGesture
{
	private enum State
	{
		Aligning,
		Ready,
		Returning,
		Sweeping,
		Recovery
	}

	private const float AimAngle = 75f;

	private const float AimPaddingAngle = 10f;

	private const float AimRestSpeed = 25f;

	private const float StartSpeed = 140f;

	private const float StrokeAngle = 24f;

	private const float StrokeTime = 0.3f;

	private const float MouseThresholdMultiplier = 0.8f;

	private State currentState;

	private Vector3 previousRight;

	private Vector3 previousUp;

	private Vector3 capturedRight;

	private Vector3 capturedUp;

	private Vector3 direction;

	private Vector3 bend;

	private Vector2 travel;

	private Vector2 previousReturnDelta;

	private float strokeTravelAngle;

	private float peakSpeed;

	private float stateTimer;

	private float quietTimer;

	private bool previousGamepad;

	internal bool Prepared => currentState == State.Sweeping;

	internal bool Ready
	{
		get
		{
			if (currentState != State.Ready)
			{
				return currentState == State.Returning;
			}
			return true;
		}
	}

	internal void Reset()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		currentState = State.Aligning;
		previousRight = Vector3.zero;
		previousUp = Vector3.zero;
		stateTimer = 0f;
		quietTimer = 0f;
		travel = Vector2.zero;
		strokeTravelAngle = 0f;
		peakSpeed = 0f;
	}

	internal ItemMeleeWhipGesture.Action Tick(Vector3 _aim, Vector3 _right, Vector3 _up, Vector2 _input, bool _gamepad, Vector3 _targetDirection, float _deltaTime, out Vector3 _direction, out Vector3 _bend, out float _power)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
		//IL_0217: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		_direction = direction;
		_bend = bend;
		_power = 0f;
		if (!ItemMeleeWhipGesture.DirectionValid(_aim) || !ItemMeleeWhipGesture.DirectionValid(_right) || !ItemMeleeWhipGesture.DirectionValid(_up) || !ItemMeleeWhipGesture.DirectionValid(_targetDirection) || (_gamepad && !float.IsFinite(_input.sqrMagnitude)) || !float.IsFinite(_deltaTime) || _deltaTime <= 0f || _deltaTime > 0.1f)
		{
			return Cancel();
		}
		Vector3 val = (ItemMeleeWhipGesture.DirectionValid(previousRight) ? previousRight : _right.normalized);
		Vector3 val2 = (ItemMeleeWhipGesture.DirectionValid(previousUp) ? previousUp : _up.normalized);
		previousRight = _right.normalized;
		previousUp = _up.normalized;
		if (currentState == State.Recovery)
		{
			stateTimer = Mathf.Max(0f, stateTimer - _deltaTime);
			if (stateTimer <= 0f)
			{
				Reset();
			}
			return ItemMeleeWhipGesture.Action.None;
		}
		if (Prepared && _gamepad != previousGamepad)
		{
			return Cancel();
		}
		Vector2 val3 = (_gamepad ? ItemMeleeWhipGesture.AngularDeltaGet(_input, _gamepad: true, _deltaTime) : AimDeltaGet(_aim.normalized, val, val2));
		float num = val3.magnitude / _deltaTime;
		if (_gamepad ? (val3.magnitude > 75f) : (val3.magnitude >= 179f))
		{
			return Cancel();
		}
		_targetDirection.Normalize();
		if (currentState == State.Aligning)
		{
			if (Vector3.Angle(_aim, _targetDirection) > 75f)
			{
				return ItemMeleeWhipGesture.Action.None;
			}
			currentState = State.Ready;
		}
		float num2 = 140f * (_gamepad ? 1f : 0.8f);
		ItemMeleeWhipGesture.Action result;
		if (Ready)
		{
			if (Vector3.Angle(Vector3.Cross(val, val2), _targetDirection) > 75f)
			{
				currentState = State.Aligning;
				if (Vector3.Angle(_aim, _targetDirection) <= 75f)
				{
					ReturnStart(val3);
				}
				return ItemMeleeWhipGesture.Action.None;
			}
			if (currentState == State.Returning && num > 25f && Vector2.Dot(val3, previousReturnDelta) > 0f)
			{
				previousReturnDelta = val3;
				return ItemMeleeWhipGesture.Action.None;
			}
			currentState = State.Ready;
			if (num < num2)
			{
				return ItemMeleeWhipGesture.Action.None;
			}
			Vector3 val4 = val * val3.x + val2 * val3.y;
			if (Vector3.Dot(val4.normalized, _targetDirection) > Mathf.Sin(0.17453292f))
			{
				ReturnStart(val3);
				return ItemMeleeWhipGesture.Action.None;
			}
			result = SweepStart(val, val2, val3, num, _gamepad, _deltaTime, out _power);
		}
		else
		{
			if (Vector2.Dot(val3, travel) < 0f && num >= num2)
			{
				ReturnStart(val3);
				return ItemMeleeWhipGesture.Action.Cancel;
			}
			stateTimer += _deltaTime;
			quietTimer = ((num < num2) ? (quietTimer + _deltaTime) : 0f);
			if (stateTimer > 0.3f || quietTimer > 0.08f)
			{
				return Cancel();
			}
			result = SweepTick(val3, num, out _power);
		}
		_direction = direction;
		_bend = bend;
		return result;
	}

	internal static Vector2 AimDeltaGet(Vector3 _aim, Vector3 _right, Vector3 _up)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.Cross(_right, _up);
		Vector2 val2 = new Vector2(Vector3.Dot(_aim, _right), Vector3.Dot(_aim, _up));
		float num = Mathf.Atan2(val2.magnitude, Vector3.Dot(_aim, val)) * 57.29578f;
		return ((val2.sqrMagnitude > 0f) ? val2.normalized : Vector2.right) * num;
	}

	private void ReturnStart(Vector2 _delta)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		currentState = State.Returning;
		previousReturnDelta = _delta;
	}

	private ItemMeleeWhipGesture.Action SweepStart(Vector3 _right, Vector3 _up, Vector2 _delta, float _speed, bool _gamepad, float _deltaTime, out float _power)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		_power = 0f;
		capturedRight = _right;
		capturedUp = _up;
		previousGamepad = _gamepad;
		travel = _delta;
		strokeTravelAngle = _delta.magnitude;
		peakSpeed = _speed;
		stateTimer = _deltaTime;
		quietTimer = 0f;
		currentState = State.Sweeping;
		Vector3 val = capturedRight * _delta.x + capturedUp * _delta.y;
		bend = val.normalized;
		direction = bend;
		float num = 24f * (_gamepad ? 1f : 0.8f);
		if (!(travel.magnitude >= num))
		{
			return ItemMeleeWhipGesture.Action.Prepare;
		}
		return Strike(bend, bend, _speed, out _power);
	}

	private ItemMeleeWhipGesture.Action SweepTick(Vector2 _delta, float _speed, out float _power)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		_power = 0f;
		float num = (previousGamepad ? 1f : 0.8f);
		float num2 = 140f * num;
		travel += _delta;
		strokeTravelAngle += _delta.magnitude;
		peakSpeed = Mathf.Max(peakSpeed, _speed);
		if (travel.magnitude < strokeTravelAngle * 0.85f)
		{
			return Cancel();
		}
		if (travel.magnitude < 24f * num || _speed < num2)
		{
			return ItemMeleeWhipGesture.Action.None;
		}
		Vector3 val = capturedRight * travel.x + capturedUp * travel.y;
		Vector3 normalized = val.normalized;
		float speed = (previousGamepad ? peakSpeed : (strokeTravelAngle / stateTimer));
		return Strike(normalized, normalized, speed, out _power);
	}

	private ItemMeleeWhipGesture.Action Strike(Vector3 _direction, Vector3 _bend, float _speed, out float _power)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		direction = _direction;
		bend = _bend;
		_power = Mathf.Lerp(0.4f, 1f, Mathf.InverseLerp(90f, 180f, _speed));
		currentState = State.Recovery;
		stateTimer = 0.3f;
		return ItemMeleeWhipGesture.Action.Strike;
	}

	private ItemMeleeWhipGesture.Action Cancel()
	{
		bool prepared = Prepared;
		Reset();
		if (!prepared)
		{
			return ItemMeleeWhipGesture.Action.None;
		}
		return ItemMeleeWhipGesture.Action.Cancel;
	}
}
