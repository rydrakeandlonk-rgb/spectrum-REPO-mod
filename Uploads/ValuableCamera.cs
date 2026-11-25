using System.Collections.Generic;
using UnityEngine;

public class ValuableCamera : MonoBehaviour
{
	public Sound soundFlash;

	public List<MeshRenderer> meshRenderers;

	private float emmissionTimer;

	private Transform stunExplosion;

	private void Start()
	{
		stunExplosion = ((Component)((Component)this).GetComponentInChildren<StunExplosion>()).transform;
		((Component)stunExplosion).gameObject.SetActive(false);
	}

	private void Update()
	{
		if (!(emmissionTimer > 0f))
		{
			return;
		}
		emmissionTimer -= Time.deltaTime;
		if (!(emmissionTimer <= 0f))
		{
			return;
		}
		foreach (MeshRenderer meshRenderer in meshRenderers)
		{
			((Renderer)meshRenderer).material.DisableKeyword("_EMISSION");
		}
	}

	public void Explosion()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		soundFlash.Play(((Component)this).transform.position);
		foreach (MeshRenderer meshRenderer in meshRenderers)
		{
			((Renderer)meshRenderer).material.EnableKeyword("_EMISSION");
			emmissionTimer = 0.2f;
		}
		GameObject val = Object.Instantiate<GameObject>(((Component)stunExplosion).gameObject, ((Component)this).transform.position, ((Component)this).transform.rotation);
		val.transform.parent = null;
		val.SetActive(true);
	}
}
