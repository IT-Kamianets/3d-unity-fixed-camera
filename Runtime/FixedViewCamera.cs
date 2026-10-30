using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITKamianets.Engine.FixedCamera
{
	/// <summary>
	/// The one camera for fixed shots. It looks at <see cref="Subject"/> from the anchor of the shot volume the
	/// subject stands in, cutting or gliding between shots, and can take a close-up override on top.
	/// </summary>
	[RequireComponent(typeof(Camera))]
	[DisallowMultipleComponent]
	public sealed class FixedViewCamera : MonoBehaviour
	{
		public Transform Subject;
		public Vector3 LookOffset = new Vector3(0f, 1.1f, 0f);
		/// <summary>Metres the subject must be outside a shot before the camera lets go of it.</summary>
		public float Hysteresis = 0.3f;
		public float BlendSeconds = 0.3f;
		public float Fallback = 7f;

		Camera _camera;
		CameraShotVolume _current;
		float _yawAtCut;
		bool _holdBasis;
		bool _snap;
		bool _wasOverride;
		float _blendLeft;
		Vector3 _fromPosition;
		Quaternion _fromRotation;
		float _fromFov;
		readonly List<(Transform anchor, Transform look, float fov)> _overrides = new List<(Transform, Transform, float)>();

		public CameraShotVolume Current => _current;
		public string ShotId => _overrides.Count > 0 ? "inspect" : _current != null ? _current.ShotId : "";
		public int CutCount { get; private set; }
		public bool Overriding => _overrides.Count > 0;

		/// <summary>Raised when the shot changes (not for overrides).</summary>
		public event Action<string> ShotChanged;

		/// <summary>
		/// The yaw to move relative to. While <see cref="HoldBasis"/> is on (a movement key is down) it stays
		/// where it was when the key went down, so a cut never turns held input around.
		/// </summary>
		public float MovementYaw => _holdBasis ? _yawAtCut : transform.eulerAngles.y;

		void Awake() => _camera = GetComponent<Camera>();

		/// <summary>Call every frame with whether a movement input is held.</summary>
		public void HoldBasis(bool held)
		{
			if (held && !_holdBasis) _yawAtCut = transform.eulerAngles.y;
			_holdBasis = held;
		}

		public void PushOverride(Transform anchor, Transform look, float fov = 40f) => _overrides.Add((anchor, look, fov));

		public void PopOverride()
		{
			if (_overrides.Count > 0) _overrides.RemoveAt(_overrides.Count - 1);
		}

		/// <summary>Puts the camera on the shot it should have right now, with no glide.</summary>
		public void Snap()
		{
			_snap = true;
			if (Subject != null) Choose();
		}

		void Choose()
		{
			var next = CameraShotVolume.Select(CameraShotVolume.All, Subject.position, _current, Hysteresis);
			if (next == _current) return;
			_current = next;
			CutCount++;
			if (next != null)
			{
				if (next.Cut) _snap = true;
				else BeginBlend();
				ShotChanged?.Invoke(next.ShotId);
			}
		}

		void BeginBlend()
		{
			_fromPosition = transform.position;
			_fromRotation = transform.rotation;
			_fromFov = _camera.fieldOfView;
			_blendLeft = BlendSeconds;
		}

		void LateUpdate()
		{
			if (Subject == null) return;
			if (_overrides.Count == 0) Choose();

			var focus = Subject.position + LookOffset;
			Vector3 position;
			Quaternion rotation;
			float fov;
			if (_overrides.Count > 0)
			{
				var top = _overrides[_overrides.Count - 1];
				position = top.anchor.position;
				rotation = Quaternion.LookRotation((top.look != null ? top.look.position : focus) - position);
				fov = top.fov;
				if (!_wasOverride) BeginBlend();
			}
			else if (_current != null && _current.Anchor != null)
			{
				position = _current.Anchor.position;
				rotation = Quaternion.LookRotation(focus - position);
				fov = _current.Fov;
				if (_wasOverride) BeginBlend();
			}
			else
			{
				position = focus + new Vector3(0f, Fallback, -Fallback);
				rotation = Quaternion.LookRotation(focus - position);
				fov = 50f;
			}
			_wasOverride = _overrides.Count > 0;

			if (_snap || _blendLeft <= 0f)
			{
				_snap = false;
				_blendLeft = 0f;
				transform.SetPositionAndRotation(position, rotation);
				_camera.fieldOfView = fov;
				return;
			}

			_blendLeft -= Time.unscaledDeltaTime;
			var t = 1f - Mathf.Clamp01(_blendLeft / Mathf.Max(0.01f, BlendSeconds));
			t = t * t * (3f - 2f * t);
			transform.SetPositionAndRotation(Vector3.Lerp(_fromPosition, position, t), Quaternion.Slerp(_fromRotation, rotation, t));
			_camera.fieldOfView = Mathf.Lerp(_fromFov, fov, t);
		}
	}
}
