using UnityEngine;

public class ValuableLevitationPotion : MonoBehaviour
{
	public GameObject levitationSphere;

	public void ActivateSphere()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Object.Instantiate<GameObject>(levitationSphere, ((Component)this).transform.position, Quaternion.identity);
	}
}
