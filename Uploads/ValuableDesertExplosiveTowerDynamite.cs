using System;
using Photon.Pun;
using UnityEngine;

public class ValuableDesertExplosiveTowerDynamite : MonoBehaviour
{
	private PhotonView photonView;

	private EnemyBangFuse enemyBangFuse;

	private void Awake()
	{
		photonView = ((Component)this).GetComponentInParent<PhotonView>();
		enemyBangFuse = ((Component)this).GetComponentInChildren<EnemyBangFuse>();
	}

	public void StandaloneSetup(float _speed)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("StandaloneSetupRPC", (RpcTarget)0, new object[1] { _speed });
		}
		else
		{
			StandaloneSetupRPC(_speed);
		}
	}

	[PunRPC]
	private void StandaloneSetupRPC(float _speed, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			enemyBangFuse.standaloneFuseSpeed = _speed;
		}
	}

	public void Explode()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMultiplayer())
		{
			photonView.RPC("ExplodeRPC", (RpcTarget)0, Array.Empty<object>());
		}
		else
		{
			ExplodeRPC();
		}
	}

	[PunRPC]
	private void ExplodeRPC(PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.MasterOnlyRPC(_info))
		{
			((Component)this).GetComponentInParent<ParticleScriptExplosion>().Spawn(enemyBangFuse.standalonePhysObject.centerPoint, 0.75f, 15, 10, 1.5f);
			EnemyDirector.instance.SetInvestigate(enemyBangFuse.standalonePhysObject.centerPoint, 40f);
			Object.Destroy((Object)(object)((Component)enemyBangFuse.standalonePhysObject).gameObject);
		}
	}
}
