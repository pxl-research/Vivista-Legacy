using System;
using UnityEngine;

public static class VersionedInteractionPoint {

	[Serializable]
	public class InteractionPointVersion1
	{
		public Vector3 position;
		public Quaternion rotation;
		public InteractionType type;
		public string title;
		public string body;
		public string filename;
		public double startTime;
		public double endTime;
		public Vector3 returnRayOrigin;
		public Vector3 returnRayDirection;

		public InteractionPointVersion1(InteractionpointSerializeCompat pointCompat)
		{
			position = pointCompat.position;
			rotation = pointCompat.rotation;
			type = pointCompat.type;
			title = pointCompat.title;
			body = pointCompat.body;
			filename = pointCompat.filename;
			startTime = pointCompat.startTime;
			endTime = pointCompat.endTime;
			returnRayOrigin = pointCompat.returnRayOrigin;
			returnRayDirection = pointCompat.returnRayDirection;
		}
	}
}
