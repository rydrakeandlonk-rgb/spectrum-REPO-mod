using UnityEngine;

public class ValuableRattleSnake : MonoBehaviour
{
	public Transform spring1Transform;

	public Transform spring1Target;

	public Transform spring2Transform;

	public Transform spring2Target;

	public Transform spring3Transform;

	public Transform spring3Target;

	public SpringQuaternion spring1;

	public SpringQuaternion spring2;

	public SpringQuaternion spring3;

	public Sound soundRattle;

	public Sound soundRattleShort;

	private PhysGrabObject physGrabObject;

	private Vector3 previousVelocity;

	private Vector3 lastRattlePosition;

	private Vector3 currentVelocity;

	private float distanceTraveled;

	private float currentSpeed;

	private float previousSpeed;

	private float accelerationMagnitude;

	private float directionChangeThreshold = 0.1f;

	private float minimumVelocityMagnitude = 1.5f;

	private float shortRattleDistanceThreshold = 1f;

	private float minDistanceThreshold = 0.5f;

	private float minimumAccelerationMagnitude = 2f;

	private void Start()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		previousVelocity = Vector3.zero;
		lastRattlePosition = ((Component)this).transform.position;
	}

	private void Update()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		spring1Transform.rotation = SemiFunc.SpringQuaternionGet(spring1, spring1Target.rotation);
		spring2Transform.rotation = SemiFunc.SpringQuaternionGet(spring2, spring2Target.rotation);
		spring3Transform.rotation = SemiFunc.SpringQuaternionGet(spring3, spring3Target.rotation);
		if (physGrabObject.playerGrabbing.Count == 0)
		{
			return;
		}
		currentVelocity = physGrabObject.rbVelocity;
		currentSpeed = currentVelocity.magnitude;
		previousSpeed = previousVelocity.magnitude;
		Vector3 val = (currentVelocity - previousVelocity) / Time.deltaTime;
		accelerationMagnitude = val.magnitude;
		if (currentSpeed > minimumVelocityMagnitude && previousSpeed > minimumVelocityMagnitude)
		{
			if (Vector3.Dot(currentVelocity.normalized, previousVelocity.normalized) < directionChangeThreshold)
			{
				TryPlayRattleSound();
			}
		}
		else if (currentSpeed > minimumVelocityMagnitude != previousSpeed > minimumVelocityMagnitude && accelerationMagnitude >= minimumAccelerationMagnitude)
		{
			TryPlayRattleSound();
		}
		previousVelocity = currentVelocity;
	}

	private void TryPlayRattleSound()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		distanceTraveled = Vector3.Distance(((Component)this).transform.position, lastRattlePosition);
		if (distanceTraveled >= minDistanceThreshold)
		{
			((distanceTraveled < shortRattleDistanceThreshold) ? soundRattleShort : soundRattle).Play(((Component)this).transform.position);
		}
		lastRattlePosition = ((Component)this).transform.position;
	}
}
