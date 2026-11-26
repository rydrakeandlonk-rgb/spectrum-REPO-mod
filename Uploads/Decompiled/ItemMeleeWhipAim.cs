using UnityEngine;

internal static class ItemMeleeWhipAim
{
	internal static float Score(Vector3 _offset, Vector3 _direction, float _reach, float _angle, float _radius)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		float magnitude = _offset.magnitude;
		float num = Vector3.Dot(_offset, _direction);
		if (num <= 0.2f || magnitude > _reach || _angle <= 0f || _radius <= 0f)
		{
			return float.PositiveInfinity;
		}
		Vector3 val = _offset - _direction * num;
		float magnitude2 = val.magnitude;
		float num2 = Mathf.Atan2(magnitude2, num) * 57.29578f;
		if (num2 > _angle || magnitude2 > _radius)
		{
			return float.PositiveInfinity;
		}
		return num2 / _angle + magnitude / _reach * 0.1f;
	}

	internal static Vector3 LaunchDirection(Vector3 _delta, float _speed, float _gravity)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		Vector3 normalized = _delta.normalized;
		Vector3 val = new Vector3(_delta.x, 0f, _delta.z);
		float magnitude = val.magnitude;
		if (_gravity <= 0f || _speed <= 0.01f || magnitude <= 0.05f)
		{
			return normalized;
		}
		float num = _speed * _speed;
		float num2 = num * num - _gravity * (_gravity * magnitude * magnitude + 2f * _delta.y * num);
		if (num2 < 0f)
		{
			return normalized;
		}
		float num3 = Mathf.Atan((num - Mathf.Sqrt(num2)) / (_gravity * magnitude));
		Vector3 val2 = val / magnitude * Mathf.Cos(num3) + Vector3.up * Mathf.Sin(num3);
		return val2.normalized;
	}
}
