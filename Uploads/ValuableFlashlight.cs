using System.Collections;
using UnityEngine;

public class ValuableFlashlight : MonoBehaviour
{
	public Sound clickOn;

	public Sound clickOff;

	public Sound flickerOn;

	public Sound flickerOff;

	public GameObject bigLight;

	public CollisionFree colliderCollision;

	private MeshRenderer meshRenderer;

	private bool grabbedPrev;

	private bool skipAimLogic = true;

	private PhysGrabObject physGrabObject;

	private Coroutine flickerRoutine;

	private void Start()
	{
		physGrabObject = ((Component)this).GetComponent<PhysGrabObject>();
		meshRenderer = ((Component)this).GetComponentInChildren<MeshRenderer>();
		bigLight.SetActive(false);
		((Renderer)meshRenderer).material.DisableKeyword("_EMISSION");
	}

	private void Update()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (physGrabObject.grabbed)
		{
			if (!grabbedPrev)
			{
				clickOn.Play(physGrabObject.centerPoint);
				if (flickerRoutine != null)
				{
					((MonoBehaviour)this).StopCoroutine(flickerRoutine);
				}
				flickerRoutine = ((MonoBehaviour)this).StartCoroutine(Flicker(_turnOn: true));
				grabbedPrev = true;
			}
		}
		else if (grabbedPrev)
		{
			clickOff.Play(physGrabObject.centerPoint);
			if (flickerRoutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(flickerRoutine);
			}
			flickerRoutine = ((MonoBehaviour)this).StartCoroutine(Flicker(_turnOn: false));
			skipAimLogic = true;
			grabbedPrev = false;
		}
	}

	public void ImpactFlicker()
	{
		if (!SemiFunc.Photosensitivity() && !physGrabObject.impactDetector.inCart)
		{
			if (flickerRoutine != null)
			{
				((MonoBehaviour)this).StopCoroutine(flickerRoutine);
			}
			flickerRoutine = ((MonoBehaviour)this).StartCoroutine(Flicker(_turnOn: true, !physGrabObject.grabbed));
		}
	}

	public void aimLogic()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer() || (skipAimLogic && colliderCollision.colliding))
		{
			return;
		}
		if (!colliderCollision.colliding)
		{
			skipAimLogic = false;
		}
		float num = -0.2f;
		float num2 = -5f;
		if (physGrabObject.playerGrabbing.Count <= 0)
		{
			return;
		}
		Quaternion turnX = Quaternion.Euler(num2, 0f, 0f);
		Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
		Quaternion rotation = ((Component)this).transform.rotation;
		Quaternion turnZ = Quaternion.Euler(0f, 0f, rotation.eulerAngles.z);
		bool flag = false;
		bool flag2 = true;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (flag2)
			{
				flag2 = false;
			}
			if (item.isRotating)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			physGrabObject.TurnXYZ(turnX, turnY, turnZ);
		}
		float pos = num;
		physGrabObject.OverrideGrabVerticalPosition(pos);
		if (!flag && physGrabObject.grabbed)
		{
			physGrabObject.OverrideTorqueStrength(12f);
			physGrabObject.OverrideAngularDrag(20f);
		}
		if (flag)
		{
			physGrabObject.OverrideTorqueStrength(2f);
			physGrabObject.OverrideAngularDrag(20f);
		}
	}

	private IEnumerator Flicker(bool _turnOn, bool _dropped = false)
	{
		for (int i = 0; i < Random.Range(1, 4); i++)
		{
			bool flag = _turnOn && !SemiFunc.Photosensitivity() && i % 2 == 0;
			bigLight.SetActive(flag);
			if (flag)
			{
				((Renderer)meshRenderer).material.EnableKeyword("_EMISSION");
				flickerOn.Play(physGrabObject.centerPoint);
			}
			else
			{
				((Renderer)meshRenderer).material.DisableKeyword("_EMISSION");
				flickerOff.Play(physGrabObject.centerPoint);
			}
			yield return (object)new WaitForSeconds(Random.Range(0.05f, 0.1f));
		}
		bigLight.SetActive(_turnOn && !_dropped);
		if (_turnOn && !_dropped)
		{
			((Renderer)meshRenderer).material.EnableKeyword("_EMISSION");
			flickerOn.Play(physGrabObject.centerPoint);
		}
		else
		{
			((Renderer)meshRenderer).material.DisableKeyword("_EMISSION");
			flickerOff.Play(physGrabObject.centerPoint);
		}
	}
}
