using System.Collections.Generic;
using UnityEngine;

namespace ITKamianets.Engine.FixedCamera
{
	/// <summary>
	/// A box that says which fixed shot to use while the subject is inside it. It has no physics: the
	/// <see cref="FixedViewCamera"/> asks every volume whether the subject is inside, so the choice is
	/// the same in tests and in the game.
	/// </summary>
	public sealed class CameraShotVolume : MonoBehaviour
	{
		public static readonly List<CameraShotVolume> All = new List<CameraShotVolume>();

		public string ShotId = "shot";
		/// <summary>Where the camera sits. It looks at the subject from here.</summary>
		public Transform Anchor;
		public float Fov = 50f;
		/// <summary>When volumes overlap, the higher priority wins.</summary>
		public int Priority;
		/// <summary>Jump to this shot at once instead of gliding.</summary>
		public bool Cut;
		/// <summary>Size of the box in metres, centred on this transform.</summary>
		public Vector3 Size = Vector3.one * 4f;

		void OnEnable() => All.Add(this);
		void OnDisable() => All.Remove(this);

		/// <summary>True when the point is inside the box grown by <paramref name="margin"/> metres on every side (up and down are generous).</summary>
		public bool Contains(Vector3 point, float margin = 0f)
		{
			var local = transform.InverseTransformPoint(point);
			var half = Size * 0.5f;
			return Mathf.Abs(local.x) <= half.x + margin && Mathf.Abs(local.y) <= half.y + margin && Mathf.Abs(local.z) <= half.z + margin;
		}

		/// <summary>
		/// The shot for a point. The current volume is kept while the point is within <paramref name="hysteresis"/>
		/// of it, unless a volume with a higher priority contains the point. Otherwise the highest priority wins,
		/// and among equals the one whose centre is nearest.
		/// </summary>
		public static CameraShotVolume Select(IReadOnlyList<CameraShotVolume> volumes, Vector3 point, CameraShotVolume current, float hysteresis)
		{
			CameraShotVolume best = null;
			var bestDistance = float.MaxValue;
			for (var i = 0; i < volumes.Count; i++)
			{
				var volume = volumes[i];
				if (volume == null || !volume.isActiveAndEnabled || !volume.Contains(point)) continue;
				var distance = (volume.transform.position - point).sqrMagnitude;
				if (best == null || volume.Priority > best.Priority || (volume.Priority == best.Priority && distance < bestDistance))
				{
					best = volume;
					bestDistance = distance;
				}
			}

			if (current != null && current.isActiveAndEnabled && current.Contains(point, hysteresis) && (best == null || best == current || best.Priority <= current.Priority))
				return current;
			return best ?? current;
		}

		void OnDrawGizmosSelected()
		{
			Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.35f);
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(Vector3.zero, Size);
		}
	}
}
