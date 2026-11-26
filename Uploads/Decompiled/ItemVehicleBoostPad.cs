using UnityEngine;

public class ItemVehicleBoostPad : MonoBehaviour
{
	[Range(1f, 3f)]
	public int boostLevel = 1;

	public Transform quadMain;

	public Transform quadOverlay;

	public Transform quadBackground;

	public Sound boostSound;

	private bool isALoopPad;

	public float loopPadDuration = 1f;

	internal Color level1ColorMain = new Color(1f, 0.5f, 0f);

	internal Color level1ColorOverlay = new Color(1f, 0.1f, 0f);

	internal Color level1ColorBackground = new Color(0.6f, 0.3f, 0f);

	internal Color level2ColorMain = new Color(0.7f, 0f, 1f);

	internal Color level2ColorOverlay = new Color(0f, 0.2f, 1f);

	internal Color level2ColorBackground = new Color(0.4f, 0f, 0.6f);

	internal Color level3ColorMain = new Color(0f, 0.9f, 1f);

	internal Color level3ColorOverlay = new Color(0f, 1f, 0.4f);

	internal Color level3ColorBackground = new Color(0f, 0.5f, 0.6f);

	internal float scrollSpeed = 2f;

	private Material materialMain;

	private Material materialOverlay;

	private Material materialBackground;

	private float scrollOffset;

	private float scrollOffsetOverlay;

	private Color baseColorMain;

	private Color baseColorOverlay;

	private void Start()
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		materialMain = ((Component)quadMain).GetComponent<Renderer>().material;
		materialOverlay = ((Component)quadOverlay).GetComponent<Renderer>().material;
		materialBackground = ((Component)quadBackground).GetComponent<Renderer>().material;
		isALoopPad = false;
		switch (boostLevel)
		{
		case 1:
			materialMain.color = level1ColorMain;
			materialOverlay.color = level1ColorOverlay;
			materialBackground.color = level1ColorBackground;
			break;
		case 2:
			materialMain.color = level2ColorMain;
			materialOverlay.color = level2ColorOverlay;
			materialBackground.color = level2ColorBackground;
			break;
		case 3:
			materialMain.color = level3ColorMain;
			materialOverlay.color = level3ColorOverlay;
			materialBackground.color = level3ColorBackground;
			break;
		}
		baseColorMain = materialMain.color;
		baseColorOverlay = materialOverlay.color;
	}

	private void Update()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		UpdateQuad(quadMain, materialMain, baseColorMain, ref scrollOffset, 0.6f, 0.6f, 0.5f, 1f, 2f);
		UpdateQuad(quadOverlay, materialOverlay, baseColorOverlay, ref scrollOffsetOverlay, 1f, 1f, 0.25f, 1f, 1f);
	}

	private void UpdateQuad(Transform _quad, Material _material, Color _baseColor, ref float _scrollOffset, float _scaleMultX, float _scaleMultY, float _speedMult, float _pulseMax, float _pulseSpeed)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Sin(Time.time * _pulseSpeed) * 0.5f + 0.5f;
		num *= _pulseMax;
		_scrollOffset -= Time.deltaTime * scrollSpeed * _speedMult * Mathf.Lerp(1f, 2f, num);
		_material.mainTextureScale = new Vector2(_quad.lossyScale.x * Mathf.Lerp(1f, 1.4f, num) * _scaleMultX, (_quad.lossyScale.y + Mathf.Lerp(0.25f, 0.1f, num) * -1f) * _scaleMultY);
		_material.mainTextureOffset = new Vector2(_quad.lossyScale.x * (1f - Mathf.Lerp(1f, 1.4f, num) * _scaleMultX) * 0.5f, _quad.lossyScale.y * (1f - _scaleMultY) * 0.5f + _scrollOffset);
		_material.color = _baseColor * Mathf.Lerp(10f, 5f, num);
	}

	private void OnTriggerEnter(Collider _other)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		ItemVehicle componentInParent = ((Component)_other).GetComponentInParent<ItemVehicle>();
		if (Object.op_Implicit((Object)(object)componentInParent))
		{
			Vector3 forward = ((Component)this).transform.forward;
			float angle = Mathf.Asin(forward.y) * 57.29578f;
			forward.y = 0f;
			forward.Normalize();
			componentInParent.OverrideYaw(forward, 0.25f, boostLevel);
			componentInParent.OverridePitch(angle, 1f);
			if (isALoopPad)
			{
				componentInParent.OverrideLoopMode(loopPadDuration);
				componentInParent.OverrideCameraLookForward(loopPadDuration);
			}
		}
	}

	private void OnDrawGizmos()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.color = Color.yellow;
		Vector3 val = ((Component)this).transform.position + ((Component)this).transform.forward * 5f;
		Gizmos.DrawLine(((Component)this).transform.position, val);
		Gizmos.DrawLine(val, val - ((Component)this).transform.forward * 1.5f + ((Component)this).transform.right * 0.8f);
		Gizmos.DrawLine(val, val - ((Component)this).transform.forward * 1.5f - ((Component)this).transform.right * 0.8f);
	}

	public ItemVehicleBoostPad()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
	}
}
