using UnityEngine;

public class ValuableForeverCandle : MonoBehaviour
{
	public Sound soundHit;

	public void OnHit()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		soundHit.Play(((Component)this).transform.position);
	}
}
