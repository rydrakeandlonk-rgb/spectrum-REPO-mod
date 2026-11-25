using UnityEngine;

public class ValuablePills : MonoBehaviour
{
	private PhysGrabObject physgrabobject;

	private Vector3 previousVelocity;

	private Vector3 lastRattlePosition;

	public Sound soundPillsRattle;

	public Sound soundPillsRattleShort;

	private Vector3 currentVelocity;

	private float distanceTraveled;

	private float currentSpeed;

	private float previousSpeed;

	private float accelerationMagnitude;

	private float directionChangeThreshold = 0.1f;

	private float minimumVelocityMagnitude = 2f;

	private float shortRattleDistanceThreshold = 1.5f;

	private float minDistanceThreshold = 0.75f;

	private float minimumAccelerationMagnitude = 3f;

	private void Start()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		physgrabobject = ((Component)this).GetComponent<PhysGrabObject>();
		previousVelocity = Vector3.zero;
		lastRattlePosition = ((Component)this).transform.position;
	}

	private void Update()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if (physgrabobject.playerGrabbing.Count == 0)
		{
			return;
		}
		currentVelocity = physgrabobject.rbVelocity;
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
			((distanceTraveled < shortRattleDistanceThreshold) ? soundPillsRattleShort : soundPillsRattle).Play(((Component)this).transform.position);
		}
		lastRattlePosition = ((Component)this).transform.position;
	}
}
