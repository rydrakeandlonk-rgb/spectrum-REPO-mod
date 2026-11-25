using Photon.Pun;
using UnityEngine;

public class ValuableCubeBall : MonoBehaviour
{
	public Sound bounceSound;

	private Vector2 bigForceRange = new Vector2(2f, 4f);

	private Vector2 smallForceRange = new Vector2(1f, 1.5f);

	private Vector2 mediumForceRange = new Vector2(1f, 2f);

	private Vector2 bigTorqueRange = new Vector2(0.1f, 0.5f);

	private Vector2 smallTorqueRange = new Vector2(0f, 0.1f);

	private Vector2 mediumTorqueRange = new Vector2(0.05f, 0.2f);

	private float torqueMultiplier = 0.1f;

	private int bounceAmount = 3;

	private int bounces;

	private Rigidbody rb;

	private PhotonView photonView;

	private PhysGrabObject physGrabObject;

	private void Start()
	{
		rb = ((Component)this).GetComponent<Rigidbody>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		bounceAmount = Random.Range(2, 6);
	}

	public void BigBounce()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		bounceSound.Play(physGrabObject.centerPoint);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		torqueMultiplier = Random.Range(bigTorqueRange.x, bigTorqueRange.y);
		if (bounces < bounceAmount && !physGrabObject.grabbed)
		{
			bounces++;
			rb.AddForce(Vector3.up * Random.Range(bigForceRange.x, bigForceRange.y), (ForceMode)1);
			rb.AddTorque(Random.insideUnitSphere * torqueMultiplier, (ForceMode)1);
			return;
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude < 0.1f || physGrabObject.grabbed)
		{
			bounces = 0;
			bounceAmount = Random.Range(2, 6);
		}
	}

	public void SmallBounce()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		bounceSound.Play(physGrabObject.centerPoint);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		torqueMultiplier = Random.Range(smallTorqueRange.x, smallTorqueRange.y);
		Vector3 linearVelocity;
		if (bounces < bounceAmount && !physGrabObject.grabbed)
		{
			linearVelocity = rb.linearVelocity;
			if (linearVelocity.magnitude > 0.2f)
			{
				bounces++;
				rb.AddForce(Vector3.up * Random.Range(smallForceRange.x, smallForceRange.y), (ForceMode)1);
				rb.AddTorque(Random.insideUnitSphere * torqueMultiplier, (ForceMode)1);
				bounces = bounceAmount;
				return;
			}
		}
		linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude <= 0.2f || physGrabObject.grabbed)
		{
			bounces = 0;
			bounceAmount = Random.Range(2, 6);
		}
	}

	public void MediumBounce()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		bounceSound.Play(physGrabObject.centerPoint);
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		torqueMultiplier = Random.Range(mediumTorqueRange.x, mediumTorqueRange.y);
		if (bounces < bounceAmount && !physGrabObject.grabbed)
		{
			bounces++;
			rb.AddForce(Vector3.up * Random.Range(mediumForceRange.x, mediumForceRange.y), (ForceMode)1);
			rb.AddTorque(Random.insideUnitSphere * torqueMultiplier, (ForceMode)1);
			return;
		}
		Vector3 linearVelocity = rb.linearVelocity;
		if (linearVelocity.magnitude < 0.1f || physGrabObject.grabbed)
		{
			bounces = 0;
			bounceAmount = Random.Range(2, 6);
		}
	}

	public ValuableCubeBall()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
	}
}
