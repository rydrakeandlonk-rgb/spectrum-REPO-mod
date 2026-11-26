using System.IO;
using Photon.Pun;
using UnityEngine;

internal struct ItemMeleeWhipSnapshot
{
	internal int sequence;

	internal ItemMeleeWhip.State state;

	internal int targetViewID;

	internal int holderViewID;

	internal Vector3 castPosition;

	internal Vector3 flingDirection;

	internal Vector3 backswingDirection;

	internal float tetherLength;

	internal bool playFling;

	internal int castTimestamp;

	internal SemiTentacleCrackState castPath;

	internal float flingPower;

	internal float flingWaveDuration;

	internal int flingWaveTimestamp;

	private bool HasTarget
	{
		get
		{
			if (state != ItemMeleeWhip.State.Idle && state != ItemMeleeWhip.State.Backswing)
			{
				return state != ItemMeleeWhip.State.Casting;
			}
			return false;
		}
	}

	internal bool HasImpact
	{
		get
		{
			if (state != ItemMeleeWhip.State.Casting && state != ItemMeleeWhip.State.Catching)
			{
				return state == ItemMeleeWhip.State.Cracked;
			}
			return true;
		}
	}

	internal bool HasBackswing
	{
		get
		{
			if (state != ItemMeleeWhip.State.Backswing && state != ItemMeleeWhip.State.HoldingBackswing)
			{
				return state == ItemMeleeWhip.State.Windup;
			}
			return true;
		}
	}

	internal bool HasTether
	{
		get
		{
			if (state != ItemMeleeWhip.State.Holding && state != ItemMeleeWhip.State.HoldingBackswing)
			{
				return state == ItemMeleeWhip.State.Windup;
			}
			return true;
		}
	}

	internal byte[] Encode()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		using MemoryStream memoryStream = new MemoryStream(81);
		using BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		binaryWriter.Write(sequence);
		binaryWriter.Write((byte)((uint)state | (uint)(playFling ? 128 : 0)));
		if (state == ItemMeleeWhip.State.Idle)
		{
			return memoryStream.ToArray();
		}
		binaryWriter.Write(holderViewID);
		if (HasTarget)
		{
			binaryWriter.Write(targetViewID);
		}
		if (HasImpact)
		{
			WriteVector(binaryWriter, castPosition);
			WriteVector(binaryWriter, flingDirection);
		}
		if (HasBackswing)
		{
			WriteVector(binaryWriter, backswingDirection);
		}
		if (HasTether)
		{
			binaryWriter.Write(tetherLength);
		}
		if (state == ItemMeleeWhip.State.Windup)
		{
			WriteVector(binaryWriter, flingDirection);
			binaryWriter.Write(flingPower);
			binaryWriter.Write(flingWaveDuration);
			binaryWriter.Write(flingWaveTimestamp);
		}
		if (state == ItemMeleeWhip.State.Casting)
		{
			binaryWriter.Write(castTimestamp);
			WriteVector(binaryWriter, castPath.start);
			WriteVector(binaryWriter, castPath.control);
			WriteVector(binaryWriter, castPath.loopDirection);
			binaryWriter.Write(castPath.distance);
			binaryWriter.Write(castPath.duration);
		}
		return memoryStream.ToArray();
	}

	private static void WriteVector(BinaryWriter _writer, Vector3 _value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		_writer.Write(_value.x);
		_writer.Write(_value.y);
		_writer.Write(_value.z);
	}

	internal static bool TryDecode(byte[] _data, out ItemMeleeWhipSnapshot _snapshot)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		_snapshot = default;
		SemiNetPackedReader val = new SemiNetPackedReader(_data);
		_snapshot.sequence = val.ReadInt32();
		byte b = val.ReadByte();
		_snapshot.state = (ItemMeleeWhip.State)(b & 0x7F);
		_snapshot.playFling = (b & 0x80) != 0;
		if (_snapshot.sequence < 0 || _snapshot.state > ItemMeleeWhip.State.Cracked)
		{
			return false;
		}
		if (_snapshot.playFling && _snapshot.state != ItemMeleeWhip.State.Retracting)
		{
			return false;
		}
		if (_snapshot.state == ItemMeleeWhip.State.Idle)
		{
			return val.CompleteGet();
		}
		_snapshot.holderViewID = val.ReadInt32();
		if (_snapshot.HasTarget)
		{
			_snapshot.targetViewID = val.ReadInt32();
		}
		if (_snapshot.HasImpact)
		{
			_snapshot.castPosition = val.ReadPosition(true);
			_snapshot.flingDirection = val.ReadPosition(true);
		}
		if (_snapshot.HasBackswing)
		{
			_snapshot.backswingDirection = val.ReadPosition(true);
		}
		if (_snapshot.HasTether)
		{
			_snapshot.tetherLength = val.ReadFloat();
		}
		if (_snapshot.state == ItemMeleeWhip.State.Windup)
		{
			_snapshot.flingDirection = val.ReadPosition(true);
			_snapshot.flingPower = val.ReadFloat();
			_snapshot.flingWaveDuration = val.ReadFloat();
			_snapshot.flingWaveTimestamp = val.ReadInt32();
			if (!ItemMeleeWhipGesture.DirectionValid(_snapshot.flingDirection) || !ItemMeleeWhipGesture.DirectionValid(_snapshot.backswingDirection) || !float.IsFinite(_snapshot.flingPower) || _snapshot.flingPower <= 0f || _snapshot.flingPower > 1f || !float.IsFinite(_snapshot.flingWaveDuration) || _snapshot.flingWaveDuration < 0.12f || _snapshot.flingWaveDuration > 0.5f)
			{
				return false;
			}
		}
		if (_snapshot.state == ItemMeleeWhip.State.Casting)
		{
			_snapshot.castTimestamp = val.ReadInt32();
			_snapshot.castPath.start = val.ReadPosition(true);
			_snapshot.castPath.control = val.ReadPosition(true);
			_snapshot.castPath.loopDirection = val.ReadPosition(true);
			_snapshot.castPath.distance = val.ReadFloat();
			_snapshot.castPath.duration = val.ReadFloat();
			if (_snapshot.castPath.duration < 0.1f || _snapshot.castPath.distance < 0f)
			{
				return false;
			}
		}
		if (val.CompleteGet() && _snapshot.holderViewID >= 0 && _snapshot.targetViewID >= 0)
		{
			return _snapshot.tetherLength >= 0f;
		}
		return false;
	}

	internal static float CastElapsed(int _now, int _started, float _duration)
	{
		return Mathf.Clamp((float)(_now - _started) * 0.001f, 0f, _duration);
	}
}
