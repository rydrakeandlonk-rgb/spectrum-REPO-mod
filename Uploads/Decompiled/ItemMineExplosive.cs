using UnityEngine;

public class ItemMineExplosive : MonoBehaviour
{
	private ParticleScriptExplosion particleScriptExplosion;

	private ItemMine itemMine;

	private void Start()
	{
		particleScriptExplosion = ((Component)this).GetComponent<ParticleScriptExplosion>();
		itemMine = ((Component)this).GetComponent<ItemMine>();
	}

	public void OnTriggered()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)itemMine) && Object.op_Implicit((Object)(object)itemMine.activatorPlayerAvatar))
		{
			particleScriptExplosion.playerCausingHurtOverride = itemMine.activatorPlayerAvatar;
		}
		particleScriptExplosion.Spawn(((Component)this).transform.position, 1.2f, 75, 200, 4f);
	}
}
