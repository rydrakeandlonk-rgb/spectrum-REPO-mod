using UnityEngine;

internal static class ItemMeleeWhipTether
{
	internal static float PullForce(float _extension, float _separatingSpeed, float _mass, float _deltaTime, float _spring, float _damping, float _forceMax)
	{
		if (_extension <= 0f || _mass <= 0f || _deltaTime <= 0f)
		{
			return 0f;
		}
		float num = _extension * _spring + _separatingSpeed * _damping;
		float num2 = 1f + _deltaTime * (_damping + _spring * _deltaTime) / _mass;
		return Mathf.Clamp(num / num2, 0f, _forceMax);
	}
}
