using UnityEngine;

public class ValuableExtracting : MonoBehaviour
{
	private MeshFilter meshFilter;

	private MeshRenderer meshRenderer;

	private float upwardSpeed;

	private float rotationSpeed;

	private float maxDistance;

	private Vector3 startPosition;

	private bool initialized;

	public void Initialize(Mesh mesh, Material[] materials, Vector3 scale, float upwardSpeed, float rotationSpeed, float maxDistance)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		meshFilter = ((Component)this).GetComponent<MeshFilter>();
		meshRenderer = ((Component)this).GetComponent<MeshRenderer>();
		if (Object.op_Implicit((Object)(object)meshFilter))
		{
			meshFilter.sharedMesh = mesh;
		}
		if (Object.op_Implicit((Object)(object)meshRenderer))
		{
			((Renderer)meshRenderer).sharedMaterials = materials;
		}
		((Component)this).transform.localScale = scale;
		this.upwardSpeed = upwardSpeed;
		this.rotationSpeed = rotationSpeed;
		this.maxDistance = maxDistance;
		startPosition = ((Component)this).transform.position;
		initialized = true;
	}

	private void Update()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		if (initialized)
		{
			Transform transform = ((Component)this).transform;
			transform.position += Vector3.up * upwardSpeed * Time.deltaTime;
			((Component)this).transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
			if (Vector3.Distance(startPosition, ((Component)this).transform.position) >= maxDistance)
			{
				Object.Destroy((Object)(object)((Component)this).gameObject);
			}
		}
	}
}
