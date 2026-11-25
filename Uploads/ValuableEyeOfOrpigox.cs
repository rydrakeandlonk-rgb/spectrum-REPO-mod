using UnityEngine;

public class ValuableEyeOfOrpigox : MonoBehaviour
{
	public Transform eye;

	public Sound slimyEyeLoop;

	[Header("Rotation Threshold Settings")]
	public float rotationThreshold = 0.1f;

	public float maxRotationForVolume = 10f;

	private PhysGrabObject physGrabObject;

	private float pitchMultiplier;

	private Quaternion previousEyeRotation;

	private bool eyeLoopPlaying;

	private void Start()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		previousEyeRotation = eye.rotation;
	}

	private void Update()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		slimyEyeLoop.PlayLoop(eyeLoopPlaying, 10f, 1f, pitchMultiplier);
		if (physGrabObject.playerGrabbing.Count <= 0 || !Object.op_Implicit((Object)(object)physGrabObject.playerGrabbing[0]))
		{
			eyeLoopPlaying = false;
			return;
		}
		PlayerAvatar component = ((Component)physGrabObject.playerGrabbing[0]).GetComponent<PlayerAvatar>();
		Vector3 val = ((!SemiFunc.IsMultiplayer()) ? component.localCamera.GetOverrideTransform().position : component.playerAvatarVisuals.headLookAtTransform.position);
		Vector3 val2 = val - eye.position;
		Quaternion val3 = Quaternion.LookRotation(val2.normalized);
		float magnitude = physGrabObject.rbAngularVelocity.magnitude;
		float num = Mathf.Lerp(10f, 30f, Mathf.Clamp01(magnitude / 5f));
		eye.rotation = Quaternion.Slerp(eye.rotation, val3, Time.deltaTime * num);
		float num2 = Quaternion.Angle(previousEyeRotation, eye.rotation);
		previousEyeRotation = eye.rotation;
		if (num2 > rotationThreshold)
		{
			eyeLoopPlaying = true;
		}
		else
		{
			eyeLoopPlaying = false;
		}
		pitchMultiplier = Mathf.Clamp(num2, 0.75f, 1.5f);
	}
}
