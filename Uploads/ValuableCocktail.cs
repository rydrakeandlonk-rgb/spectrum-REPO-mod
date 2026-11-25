using System;
using Photon.Pun;
using UnityEngine;

public class ValuableCocktail : Trap
{
	public Transform liquid;

	public Transform SemiPukeTransform;

	public Transform eye;

	public ParticleSystem eyeParticle;

	public Sound soundSpill;

	private bool isSpilled;

	private bool spill;

	private SemiPuke semiPuke;

	protected override void Start()
	{
		base.Start();
		semiPuke = ((Component)this).GetComponentInChildren<SemiPuke>();
	}

	protected override void Update()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if (!isSpilled)
		{
			if (spill)
			{
				SpillEffects();
			}
			if (SemiFunc.IsMasterClientOrSingleplayer() && (Vector3.Dot(((Component)this).transform.up, Vector3.up) < 0.4f || (physGrabObject.rbVelocity.magnitude > 7f && !physGrabObject.impactDetector.inCart)) && !isSpilled)
			{
				SpillCocktail();
			}
		}
	}

	private void SpillEffects()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		Quaternion direction = Quaternion.Lerp(Quaternion.LookRotation(((Component)this).transform.up), Quaternion.LookRotation(Vector3.down), 0.75f);
		semiPuke.PukeActive(SemiPukeTransform.position, direction);
		((Component)liquid).gameObject.SetActive(false);
		((Component)eye).gameObject.SetActive(false);
		eyeParticle.Play();
		soundSpill.Play(physGrabObject.centerPoint);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			physGrabObject.impactDetector.BreakLight(physGrabObject.centerPoint, _forceBreak: true);
		}
		isSpilled = true;
	}

	private void SpillCocktail()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			if (SemiFunc.IsMultiplayer())
			{
				photonView.RPC("SpillCocktailRPC", (RpcTarget)0, Array.Empty<object>());
			}
			else
			{
				SpillCocktailRPC();
			}
		}
	}

	[PunRPC]
	private void SpillCocktailRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			spill = true;
		}
	}
}
