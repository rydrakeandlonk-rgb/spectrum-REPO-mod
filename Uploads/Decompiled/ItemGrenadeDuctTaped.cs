using Photon.Pun;
using UnityEngine;

public class ItemGrenadeDuctTaped : MonoBehaviour
{
	public GameObject grenadePrefab;

	private ParticleScriptExplosion particleScriptExplosion;

	private PhotonView photonView;

	private ItemGrenade itemGrenade;

	public Sound soundExplosion;

	public Sound soundExplosionGlobal;

	private void Start()
	{
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		photonView = ((Component)this).GetComponent<PhotonView>();
		itemGrenade = ((Component)this).GetComponent<ItemGrenade>();
	}

	public void Explosion()
	{
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		PlayerAvatar playerAvatar = (Object.op_Implicit((Object)(object)itemGrenade) ? itemGrenade.activatorPlayerAvatar : null);
		if (!SemiFunc.IsMultiplayer())
		{
			for (int i = 0; i < 3; i++)
			{
				Vector3 val = new Vector3(0f, 0.2f * (float)i, 0f);
				GameObject val2 = Object.Instantiate<GameObject>(grenadePrefab, ((Component)this).transform.position + val, Quaternion.identity);
				val2.GetComponent<ItemGrenadeHuman>().Spawn();
				if (Object.op_Implicit((Object)(object)playerAvatar))
				{
					ItemGrenade component = val2.GetComponent<ItemGrenade>();
					if (Object.op_Implicit((Object)(object)component))
					{
						component.SetActivator(playerAvatar);
					}
				}
			}
		}
		else if (SemiFunc.IsMasterClient())
		{
			for (int j = 0; j < 3; j++)
			{
				Vector3 val3 = new Vector3(0f, 0.2f * (float)j, 0f);
				GameObject val4 = PhotonNetwork.Instantiate("Items/Item Grenade Human", ((Component)this).transform.position + val3, Quaternion.identity, (byte)0, (object[])null);
				val4.GetComponent<ItemGrenadeHuman>().Spawn();
				if (Object.op_Implicit((Object)(object)playerAvatar))
				{
					ItemGrenade component2 = val4.GetComponent<ItemGrenade>();
					if (Object.op_Implicit((Object)(object)component2))
					{
						component2.SetActivator(playerAvatar);
					}
				}
			}
		}
		if (Object.op_Implicit((Object)(object)playerAvatar))
		{
			particleScriptExplosion.playerCausingHurtOverride = playerAvatar;
		}
		particleScriptExplosion.playerHitFullRuckusOverride = true;
		particleScriptExplosion.Spawn(((Component)this).transform.position, 0.8f, 50, 100, 4f, onlyParticleEffect: false, disableSound: true);
		soundExplosion.Play(((Component)this).transform.position);
		soundExplosionGlobal.Play(((Component)this).transform.position);
	}
}
