using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDuckBucket : MonoBehaviour
{
	public SphereCollider sphereCollider;

	private bool active;

	private bool activePrevious;

	private PlayerAvatar playerAvatar;

	public GameObject lowPassParent;

	private List<DuckBucketCollider> duckBucketColliders = new List<DuckBucketCollider>();

	private void Start()
	{
		((MonoBehaviour)this).StartCoroutine(DuckFinder());
	}

	private IEnumerator DuckFinder()
	{
		while (true)
		{
			active = false;
			playerAvatar = null;
			duckBucketColliders.Clear();
			Collider[] array = Physics.OverlapSphere(((Component)sphereCollider).transform.position, sphereCollider.radius, LayerMask.GetMask(new string[1] { "PhysGrabObject" }));
			for (int i = 0; i < array.Length; i++)
			{
				DuckBucketCollider component = ((Component)array[i]).GetComponent<DuckBucketCollider>();
				if (Object.op_Implicit((Object)(object)component))
				{
					duckBucketColliders.Add(component);
					active = true;
				}
			}
			array = Physics.OverlapSphere(((Component)sphereCollider).transform.position, sphereCollider.radius, LayerMask.op_Implicit(SemiFunc.LayerMaskGetPlayersAndPhysObjects()));
			foreach (Collider val in array)
			{
				if (((Component)((Component)val).transform).gameObject.layer == LayerMask.NameToLayer("Player"))
				{
					PlayerController componentInParent = ((Component)((Component)val).transform).GetComponentInParent<PlayerController>();
					if (Object.op_Implicit((Object)(object)componentInParent))
					{
						playerAvatar = componentInParent.playerAvatarScript;
					}
					else
					{
						playerAvatar = ((Component)((Component)val).transform).GetComponentInParent<PlayerAvatar>();
					}
					active = true;
					break;
				}
				PlayerTumble componentInParent2 = ((Component)((Component)val).transform).GetComponentInParent<PlayerTumble>();
				if (Object.op_Implicit((Object)(object)componentInParent2))
				{
					playerAvatar = componentInParent2.playerAvatar;
					active = true;
					break;
				}
			}
			yield return (object)new WaitForSeconds(1f);
		}
	}

	private void Update()
	{
		if (active)
		{
			foreach (DuckBucketCollider duckBucketCollider in duckBucketColliders)
			{
				duckBucketCollider.OverrideBucketActive(0.5f);
			}
			if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.isLocal)
			{
				PlayerController.instance.OverrideJumpCooldown(0.5f);
			}
		}
		if (active != activePrevious)
		{
			activePrevious = active;
			if (active)
			{
				lowPassParent.SetActive(true);
			}
			else
			{
				lowPassParent.SetActive(false);
			}
		}
	}
}
