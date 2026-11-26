using UnityEngine;

public class ItemMeleeWhipRing : MonoBehaviour
{
	public Transform ring;

	public Transform brokenRing;

	public FakeVelocities fakeVelocities;

	public Vector3 angleLimits = new Vector3(90f, 12f, 8f);

	public float pendulumLength = 0.025f;

	public float damping = 4.5f;

	public float sidewaysSpring = 180f;

	[Range(0f, 0.9f)]
	public float bounce = 0.55f;

	public float movementStrength = 0.6f;

	private Quaternion ringRotation;

	private Quaternion brokenRingRotation;

	private Vector3 angles;

	private Vector3 angularVelocity;

	private Vector3 previousVelocity;

	private Vector3 previousAngularVelocity;

	private Vector3 previousPosition;

	private Quaternion previousRotation;

	private float motionSuppressTimer;

	private void Awake()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		ringRotation = ring.localRotation;
		brokenRingRotation = brokenRing.localRotation;
	}

	private void OnEnable()
	{
		MotionReset();
	}

	private void LateUpdate()
	{
		MotionTick(Time.deltaTime);
		VisualTick();
	}

	private void OnDisable()
	{
		MotionReset();
		VisualTick();
	}

	private void MotionReset()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		angles = Vector3.zero;
		angularVelocity = Vector3.zero;
		previousVelocity = Vector3.zero;
		previousAngularVelocity = Vector3.zero;
		previousPosition = ((Component)this).transform.position;
		previousRotation = ((Component)this).transform.rotation;
		motionSuppressTimer = 0.12f;
	}

	private void MotionTick(float time)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		if (time <= 0f)
		{
			return;
		}
		if (!(time > 0.25f))
		{
			Vector3 val = ((Component)this).transform.position - previousPosition;
			if (!(val.sqrMagnitude > 4f) && !(Quaternion.Angle(previousRotation, ((Component)this).transform.rotation) > 120f))
			{
				previousPosition = ((Component)this).transform.position;
				previousRotation = ((Component)this).transform.rotation;
				Transform trackedTransform = fakeVelocities.trackedTransform;
				Vector3 val2 = fakeVelocities.velocity + Vector3.Cross(fakeVelocities.angularVelocity, ring.position - trackedTransform.position);
				Vector3 val3 = fakeVelocities.angularVelocity;
				Quaternion val4 = Quaternion.Inverse(ring.parent.rotation * ringRotation);
				Vector3 val5 = val4 * (val2 - previousVelocity);
				Vector3 val6 = val4 * (val3 - previousAngularVelocity);
				previousVelocity = val2;
				previousAngularVelocity = val3;
				if (motionSuppressTimer > 0f)
				{
					motionSuppressTimer = Mathf.Max(0f, motionSuppressTimer - time);
					return;
				}
				float num = Mathf.Max(0.005f, pendulumLength);
				Vector3 val7 = Vector3.Cross(Quaternion.Euler(angles) * Vector3.down, -val5) * (movementStrength / num) - val6;
				angularVelocity += val7 * 57.29578f;
				angularVelocity = Vector3.ClampMagnitude(angularVelocity, 2400f);
				Vector3 val8 = val4 * Physics.gravity;
				int num2 = Mathf.CeilToInt(time * 120f);
				float num3 = time / (float)num2;
				for (int i = 0; i < num2; i++)
				{
					Vector3 val9 = Vector3.Cross(Quaternion.Euler(angles) * Vector3.down, val8) * (57.29578f / num);
					val9.y -= angles.y * sidewaysSpring;
					val9.z -= angles.z * sidewaysSpring;
					angularVelocity += val9 * num3;
					angularVelocity *= Mathf.Exp((0f - damping) * num3);
					angles += angularVelocity * num3;
					LimitAxis(ref angles.x, ref angularVelocity.x, angleLimits.x, val9.x * num3);
					LimitAxis(ref angles.y, ref angularVelocity.y, angleLimits.y, val9.y * num3);
					LimitAxis(ref angles.z, ref angularVelocity.z, angleLimits.z, val9.z * num3);
				}
				return;
			}
		}
		MotionReset();
	}

	private void LimitAxis(ref float angle, ref float velocity, float limit, float stepVelocity)
	{
		limit = Mathf.Max(0f, limit);
		float num = 0f;
		if (angle > limit)
		{
			angle = limit;
			if (velocity > 0f)
			{
				num = velocity;
			}
		}
		else if (angle < 0f - limit)
		{
			angle = 0f - limit;
			if (velocity < 0f)
			{
				num = 0f - velocity;
			}
		}
		if (!(num <= 0f))
		{
			if (num <= Mathf.Max(20f, Mathf.Abs(stepVelocity) * 1.5f))
			{
				velocity = 0f;
			}
			else
			{
				velocity *= 0f - bounce;
			}
		}
	}

	private void VisualTick()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		Quaternion val = Quaternion.Euler(angles);
		ring.localRotation = ringRotation * val;
		brokenRing.localRotation = brokenRingRotation * val;
	}

	public ItemMeleeWhipRing()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
	}
}
