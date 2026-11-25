using UnityEngine;

public class ValuableVolumeDebugDisplay : MonoBehaviour
{
	public ValuableVolume.Type VolumeType;

	[SerializeField]
	private DebugValuableMeshes debugValuableMeshes;

	private void OnDrawGizmos()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.color = new Color(1f, 0.92f, 0.02f, 0.3f);
		Gizmos.matrix = ((Component)this).transform.localToWorldMatrix;
		Mesh val = null;
		if (Object.op_Implicit((Object)(object)debugValuableMeshes))
		{
			val = VolumeType switch
			{
				ValuableVolume.Type.Tiny => debugValuableMeshes.tiny, 
				ValuableVolume.Type.Small => debugValuableMeshes.small, 
				ValuableVolume.Type.Medium => debugValuableMeshes.medium, 
				ValuableVolume.Type.Big => debugValuableMeshes.big, 
				ValuableVolume.Type.Wide => debugValuableMeshes.wide, 
				ValuableVolume.Type.Tall => debugValuableMeshes.tall, 
				ValuableVolume.Type.VeryTall => debugValuableMeshes.veryTall, 
				_ => (Mesh)null, 
			};
		}
		Gizmos.DrawMesh(val, Vector3.zero, Quaternion.identity, Vector3.one);
	}

	private void OnDrawGizmosSelected()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.color = Color.yellow;
		Gizmos.matrix = ((Component)this).transform.localToWorldMatrix;
		Mesh val = null;
		if (Object.op_Implicit((Object)(object)debugValuableMeshes))
		{
			val = VolumeType switch
			{
				ValuableVolume.Type.Tiny => debugValuableMeshes.tiny, 
				ValuableVolume.Type.Small => debugValuableMeshes.small, 
				ValuableVolume.Type.Medium => debugValuableMeshes.medium, 
				ValuableVolume.Type.Big => debugValuableMeshes.big, 
				ValuableVolume.Type.Wide => debugValuableMeshes.wide, 
				ValuableVolume.Type.Tall => debugValuableMeshes.tall, 
				ValuableVolume.Type.VeryTall => debugValuableMeshes.veryTall, 
				_ => (Mesh)null, 
			};
		}
		Gizmos.DrawWireMesh(val, Vector3.zero, Quaternion.identity, Vector3.one);
	}
}
