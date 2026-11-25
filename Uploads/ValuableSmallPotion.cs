using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ValuableSmallPotion : MonoBehaviour
{
	public GameObject healAura;

	public int healthPool = 10;

	public List<ParticleSystem> breakParticles;

	public void PotionBreak()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			GameObject val = ((!SemiFunc.IsMultiplayer()) ? Object.Instantiate<GameObject>(healAura, ((Component)this).transform.position, Quaternion.identity) : PhotonNetwork.Instantiate("Enemies/" + ((Object)healAura).name, ((Component)this).transform.position, Quaternion.identity, (byte)0, (object[])null));
			if (Object.op_Implicit((Object)(object)val))
			{
				val.GetComponent<EnemyTickHealAura>().healthPool = healthPool;
			}
		}
		foreach (ParticleSystem breakParticle in breakParticles)
		{
			((Component)breakParticle).transform.SetParent((Transform)null);
			breakParticle.Play(true);
		}
	}
}
