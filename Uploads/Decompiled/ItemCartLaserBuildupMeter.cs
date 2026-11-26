using UnityEngine;

public class ItemCartLaserBuildupMeter : MonoBehaviour
{
	private enum State
	{
		Inactive,
		Buildup,
		Shooting,
		GoingBack
	}

	private ItemCartLaser itemCartLaser;

	private Light lightMeter;

	private MeshRenderer meshRenderer;

	private ItemCartCannonMain itemCartCannonMain;

	private AnimationCurve animationCurveBuildup;

	private State statePrevState = State.GoingBack;

	private State stateCurrent;

	private bool stateStart = true;

	private int cartLaserStatePrev;

	private int cartLaserStateCurrent;

	private void Start()
	{
		itemCartLaser = ((Component)this).GetComponentInParent<ItemCartLaser>();
		itemCartCannonMain = ((Component)this).GetComponentInParent<ItemCartCannonMain>();
		lightMeter = ((Component)this).GetComponentInChildren<Light>();
		meshRenderer = ((Component)this).GetComponentInChildren<MeshRenderer>();
		animationCurveBuildup = itemCartLaser.animationCurveBuildup;
	}

	private void Update()
	{
		cartLaserStatePrev = itemCartLaser.stateCurrent;
		if (cartLaserStateCurrent != itemCartLaser.stateCurrent)
		{
			cartLaserStateCurrent = itemCartLaser.stateCurrent;
			UpdateMeterState();
		}
		StateMachine();
	}

	private void StateMachine()
	{
		switch (stateCurrent)
		{
		case State.Inactive:
			StateInactive();
			break;
		case State.Buildup:
			StateBuildup();
			break;
		case State.Shooting:
			StateShooting();
			break;
		case State.GoingBack:
			StateGoingBack();
			break;
		}
	}

	private void LoopTexture()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		((Renderer)meshRenderer).material.SetTextureOffset("_MainTex", new Vector2(0f, (0f - Time.time) * 20f));
		float num = 2f + 2f * ((Component)this).transform.localScale.z;
		((Renderer)meshRenderer).material.SetTextureScale("_MainTex", new Vector2(1f, num));
	}

	private void LightIntensity()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		float intensity = ((Component)this).transform.localScale.z * 4f;
		lightMeter.intensity = intensity;
	}

	private void StateInactive()
	{
		if (stateStart)
		{
			stateStart = false;
			ToggleMeterEffect(_toggle: false);
		}
	}

	private void StateBuildup()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			ToggleMeterEffect(_toggle: true);
		}
		float num = animationCurveBuildup.Evaluate(itemCartCannonMain.stateTimer / itemCartCannonMain.stateTimerMax);
		((Component)this).transform.localScale = new Vector3(((Component)this).transform.localScale.x, ((Component)this).transform.localScale.y, num);
		LoopTexture();
		LightIntensity();
	}

	private void StateShooting()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			ToggleMeterEffect(_toggle: true);
			((Component)this).transform.localScale = new Vector3(((Component)this).transform.localScale.x, ((Component)this).transform.localScale.y, 1f);
		}
		lightMeter.intensity = 2f + Mathf.Sin(Time.time * 10f) * 2f;
		LoopTexture();
	}

	private void StateGoingBack()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
			ToggleMeterEffect(_toggle: true);
		}
		float num = animationCurveBuildup.Evaluate(itemCartCannonMain.stateTimer / itemCartCannonMain.stateTimerMax);
		((Component)this).transform.localScale = new Vector3(((Component)this).transform.localScale.x, ((Component)this).transform.localScale.y, 1f - num);
		LightIntensity();
		LoopTexture();
		if (num > 0.95f && ((Behaviour)lightMeter).enabled)
		{
			ToggleMeterEffect(_toggle: false);
		}
	}

	private void ToggleMeterEffect(bool _toggle)
	{
		((Renderer)meshRenderer).enabled = _toggle;
		((Behaviour)lightMeter).enabled = _toggle;
	}

	private void UpdateMeterState()
	{
		if (cartLaserStateCurrent == 0 || cartLaserStateCurrent == 1)
		{
			StateSet(State.Inactive);
		}
		if (cartLaserStateCurrent == 2)
		{
			StateSet(State.Buildup);
		}
		if (cartLaserStateCurrent == 3)
		{
			StateSet(State.Shooting);
		}
		if (cartLaserStateCurrent == 4)
		{
			StateSet(State.GoingBack);
		}
	}

	private void StateSet(State _state)
	{
		if (stateCurrent != statePrevState && stateCurrent != _state)
		{
			statePrevState = stateCurrent;
			stateCurrent = _state;
			stateStart = true;
		}
	}
}
