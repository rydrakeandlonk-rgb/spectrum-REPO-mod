using System.Collections;
using UnityEngine;

public class ItemUpgradePlayerTumbleWingsLogic : MonoBehaviour
{
	private enum State
	{
		Intro,
		Outro,
		Active,
		Inactive
	}

	private PhysGrabObject physGrabObject;

	private PlayerTumble playerTumble;

	public PlayerAvatar playerAvatar;

	private string steamID = "";

	private bool stateStart;

	public Transform transformWings;

	public Transform transformWingLeft;

	public Transform transformWingRight;

	private bool isLocal;

	public Sound soundWingsLoop;

	private Vector3 posPrev;

	private Vector3 posCurrent;

	private float targetSpeed;

	private float currentSpeed;

	public AudioSource localAudioSource;

	private bool lateStartDone;

	private float pitchSpeed = 1f;

	private bool fetchComplete;

	private float wingsSwitchCooldown;

	public Light lightWings;

	internal float tumbleWingTimer;

	private bool hasBeenGrounded = true;

	private MeshRenderer wing1MeshRenderer;

	private MeshRenderer wing2MeshRenderer;

	private Color originalWingBaseColor;

	private Color originalWingFresnelColor;

	private Color lightOriginalColor;

	private State currentState;

	private void Start()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		lightOriginalColor = lightWings.color;
		wing1MeshRenderer = ((Component)transformWingLeft).GetComponentInChildren<MeshRenderer>();
		wing2MeshRenderer = ((Component)transformWingRight).GetComponentInChildren<MeshRenderer>();
		originalWingBaseColor = ((Renderer)wing1MeshRenderer).material.GetColor("_BaseColor");
		originalWingFresnelColor = ((Renderer)wing1MeshRenderer).material.GetColor("_FresnelColor");
		tumbleWingTimer = 1f;
		if (Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.upgradeTumbleWingsVisualsPink)
		{
			WingsSetPinkColors();
		}
		currentState = State.Inactive;
		stateStart = true;
		((MonoBehaviour)this).StartCoroutine(LateStart());
	}

	private IEnumerator LateStart()
	{
		while (!LevelGenerator.Instance.Generated)
		{
			yield return (object)new WaitForSeconds(0.1f);
		}
		if (!SemiFunc.RunIsLobbyMenu())
		{
			lateStartDone = true;
		}
	}

	private void StateMachine()
	{
		if (fetchComplete)
		{
			switch (currentState)
			{
			case State.Intro:
				StateIntro();
				break;
			case State.Outro:
				StateOutro();
				break;
			case State.Active:
				StateActive();
				break;
			case State.Inactive:
				StateInactive();
				break;
			}
		}
	}

	private void LoopSound()
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		if (soundWingsLoop == null || !Object.op_Implicit((Object)(object)soundWingsLoop.Source))
		{
			return;
		}
		if (!Object.op_Implicit((Object)(object)playerAvatar) || !Object.op_Implicit((Object)(object)playerTumble))
		{
			if (soundWingsLoop != null && Object.op_Implicit((Object)(object)soundWingsLoop.Source))
			{
				soundWingsLoop.Stop();
			}
			return;
		}
		bool playing = playerAvatar.upgradeTumbleWings > 0f && playerAvatar.upgradeTumbleWingsVisualsActive && !playerAvatar.isDisabled && !playerAvatar.deadSet && ((Behaviour)playerAvatar.playerAvatarVisuals).isActiveAndEnabled;
		if (SemiFunc.PerSecond(30f, (Object)(object)this))
		{
			posPrev = posCurrent;
			posCurrent = ((Component)this).transform.position;
			targetSpeed = Vector3.Distance(posPrev, posCurrent) * 5f;
		}
		currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 2f);
		pitchSpeed = Mathf.Clamp(currentSpeed * 2f, 1f, 4f);
		soundWingsLoop.PlayLoop(playing, 2f, 2f, pitchSpeed);
	}

	private void Update()
	{
		if (!lateStartDone)
		{
			return;
		}
		bool flag = Object.op_Implicit((Object)(object)playerAvatar) && playerAvatar.upgradeTumbleWingsVisualsActive && !playerAvatar.isDisabled && !playerAvatar.deadSet;
		if (fetchComplete)
		{
			if (flag && (currentState == State.Inactive || currentState == State.Outro))
			{
				StateSet(State.Intro);
			}
			else if (!flag && (currentState == State.Intro || currentState == State.Active))
			{
				StateSet(State.Outro);
			}
		}
		if (SemiFunc.IsMasterClientOrSingleplayer() && wingsSwitchCooldown > 0f)
		{
			wingsSwitchCooldown -= Time.deltaTime;
		}
		if (fetchComplete && Object.op_Implicit((Object)(object)playerAvatar) && (SemiFunc.IsMasterClientOrSingleplayer() || isLocal) && playerAvatar.upgradeTumbleWingsVisualsActive && tumbleWingTimer > 0f)
		{
			float num = 1f + playerAvatar.upgradeTumbleWings / 4f;
			tumbleWingTimer = Mathf.Max(0f, tumbleWingTimer - Time.deltaTime / num);
		}
		StateMachine();
		LoopSound();
		if (fetchComplete || !SemiFunc.PerSecond(1f, (Object)(object)this) || Object.op_Implicit((Object)(object)playerTumble) || !Object.op_Implicit((Object)(object)playerAvatar))
		{
			return;
		}
		playerTumble = playerAvatar.tumble;
		if (Object.op_Implicit((Object)(object)playerTumble))
		{
			steamID = playerAvatar.steamID;
			physGrabObject = playerTumble.physGrabObject;
			if (playerAvatar.isLocal)
			{
				isLocal = true;
			}
			playerAvatar.upgradeTumbleWingsLogic = this;
			fetchComplete = true;
			StateSet(State.Inactive);
			if (isLocal)
			{
				((Behaviour)localAudioSource).enabled = true;
				TumbleWingsUI.instance.itemUpgradePlayerTumbleWingsLogic = this;
				soundWingsLoop.Source = localAudioSource;
			}
		}
	}

	public void WingsSetOriginalColors()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)wing1MeshRenderer) && Object.op_Implicit((Object)(object)wing2MeshRenderer))
		{
			((Renderer)wing1MeshRenderer).material.SetColor("_FresnelColor", originalWingFresnelColor);
			((Renderer)wing2MeshRenderer).material.SetColor("_FresnelColor", originalWingFresnelColor);
			((Renderer)wing1MeshRenderer).material.SetColor("_BaseColor", originalWingBaseColor);
			((Renderer)wing2MeshRenderer).material.SetColor("_BaseColor", originalWingBaseColor);
			lightWings.color = lightOriginalColor;
		}
	}

	public void WingsSetPinkColors()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (Object.op_Implicit((Object)(object)wing1MeshRenderer) && Object.op_Implicit((Object)(object)wing2MeshRenderer))
		{
			((Renderer)wing1MeshRenderer).material.SetColor("_FresnelColor", new Color(1f, 0.1f, 0.4f, 1f));
			((Renderer)wing2MeshRenderer).material.SetColor("_FresnelColor", new Color(1f, 0.1f, 0.4f, 1f));
			((Renderer)wing1MeshRenderer).material.SetColor("_BaseColor", new Color(0.5f, 0.1f, 0.4f, 1f));
			((Renderer)wing2MeshRenderer).material.SetColor("_BaseColor", new Color(0.5f, 0.1f, 0.4f, 1f));
			lightWings.color = new Color(1f, 0.3f, 0.8f, 1f);
		}
	}

	private void OnDisable()
	{
		tumbleWingTimer = 0f;
		wingsSwitchCooldown = 0f;
		hasBeenGrounded = true;
		currentState = State.Inactive;
		stateStart = true;
		if (Object.op_Implicit((Object)(object)transformWings))
		{
			((Component)transformWings).gameObject.SetActive(false);
		}
		if (Object.op_Implicit((Object)(object)lightWings))
		{
			lightWings.intensity = 0f;
		}
		if (soundWingsLoop != null && Object.op_Implicit((Object)(object)soundWingsLoop.Source))
		{
			soundWingsLoop.Stop();
		}
	}

	private void FixedUpdate()
	{
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		if (!fetchComplete || !lateStartDone || !SemiFunc.IsMasterClientOrSingleplayer() || !Object.op_Implicit((Object)(object)playerAvatar) || playerAvatar.isDisabled || playerAvatar.deadSet || !Object.op_Implicit((Object)(object)playerTumble) || !Object.op_Implicit((Object)(object)physGrabObject) || playerAvatar.upgradeTumbleWingsVisualsPink || physGrabObject.timerZeroGravity > 0f)
		{
			return;
		}
		float upgradeTumbleWings = playerAvatar.upgradeTumbleWings;
		if (upgradeTumbleWings <= 0f)
		{
			return;
		}
		upgradeTumbleWings += 6f;
		if (!playerTumble.isTumbling || (!playerTumble.isPlayerInputTriggered && playerTumble.tumbleOverride) || physGrabObject.playerGrabbing.Count > 0)
		{
			hasBeenGrounded = true;
			return;
		}
		if (SemiFunc.OnGroundCheck(((Component)playerTumble).transform.position, 1f, physGrabObject))
		{
			if (!hasBeenGrounded && tumbleWingTimer > 0f && wingsSwitchCooldown > 0f)
			{
				playerAvatar.OverrideTumbleWings(pink: false, 0.2f);
			}
			else
			{
				hasBeenGrounded = true;
			}
			return;
		}
		Transform overrideTransform = playerAvatar.localCamera.GetOverrideTransform();
		Quaternion targetRotation = Quaternion.LookRotation(overrideTransform.forward, Vector3.up);
		Vector3 val = SemiFunc.PhysFollowRotation(((Component)physGrabObject).transform, targetRotation, physGrabObject.rb, 20f);
		physGrabObject.rb.AddTorque(val, (ForceMode)1);
		if (!playerAvatar.upgradeTumbleWingsVisualsActive && wingsSwitchCooldown <= 0f && hasBeenGrounded)
		{
			tumbleWingTimer = 1f;
			hasBeenGrounded = false;
			wingsSwitchCooldown = 2f;
		}
		if (tumbleWingTimer <= 0f)
		{
			wingsSwitchCooldown = Mathf.Max(wingsSwitchCooldown, 0.5f);
		}
		else if (!hasBeenGrounded)
		{
			playerAvatar.OverrideTumbleWings(pink: false, 0.2f);
			if (upgradeTumbleWings <= 15f)
			{
				physGrabObject.rb.AddForceAtPosition(Vector3.up * (1.9f * upgradeTumbleWings), transformWings.position, (ForceMode)0);
			}
			else
			{
				physGrabObject.OverrideZeroGravity();
			}
			physGrabObject.rb.AddForceAtPosition(overrideTransform.forward * (0.02f * upgradeTumbleWings), transformWings.position, (ForceMode)1);
		}
	}

	private void StateIntro()
	{
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			((Component)transformWings).gameObject.SetActive(true);
			transformWings.localScale = Vector3.zero;
			lightWings.intensity = 0f;
			transformWingLeft.localRotation = Quaternion.Euler(0f, 0f, 0f);
			transformWingRight.localRotation = Quaternion.Euler(0f, 0f, 0f);
			stateStart = false;
		}
		transformWings.localScale = Vector3.Lerp(transformWings.localScale, Vector3.one, Time.deltaTime * 10f);
		if (transformWings.localScale.x > 0.98f)
		{
			transformWings.localScale = Vector3.one;
			StateSet(State.Active);
		}
		lightWings.intensity = transformWings.localScale.x * 3f;
		FlapWings();
	}

	private void StateOutro()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (stateStart)
		{
			stateStart = false;
		}
		transformWings.localScale = Vector3.Lerp(transformWings.localScale, Vector3.zero, Time.deltaTime * 10f);
		if (transformWings.localScale.x < 0.02f)
		{
			transformWings.localScale = Vector3.zero;
			StateSet(State.Inactive);
		}
		lightWings.intensity = transformWings.localScale.x * 3f;
		FlapWings();
	}

	private void StateActive()
	{
		if (stateStart)
		{
			stateStart = false;
			lightWings.intensity = 2f;
		}
		if (!playerAvatar.upgradeTumbleWingsVisualsActive)
		{
			StateSet(State.Outro);
		}
		else
		{
			FlapWings();
		}
	}

	private void StateInactive()
	{
		if (stateStart)
		{
			((Component)transformWings).gameObject.SetActive(false);
			stateStart = false;
		}
	}

	private void FlapWings()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		float num = -54f;
		float num2 = 40f;
		float num3 = Mathf.Sin(Time.time * num2) * num;
		transformWingLeft.localRotation = Quaternion.Euler(0f, -34f - num3, 0f);
		transformWingRight.localRotation = Quaternion.Euler(0f, 34f + num3, 0f);
	}

	private void StateSet(State newState)
	{
		if (currentState != newState)
		{
			currentState = newState;
			stateStart = true;
		}
	}
}
