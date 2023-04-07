using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

public struct GazePoint
{
	public Vector3 direction;
	public float time;
}

public class GazeTracking
{
	private static List<GazePoint> points;
	private static Guid id;

	private static bool submitted;

	public static void Start(Guid videoId)
	{
		Assert.IsTrue(points == null || submitted, "Forgot to call Submit() before starting a new Video View Tracking session");

		id = videoId;
		submitted = false;
		points = new List<GazePoint>();
	}

	void RegisterGazePoint(GazePoint point)
	{
		Assert.IsNotNull(points, "Forgot to call Start() before registering a gaze point");

		point.time = Mathf.Round(point.time);

		points.Add(point);
	}


	public static IEnumerator Submit()
	{
		Assert.IsNotNull(points, "Forgot to call StartResultSet() before submitting a Result");

		string json = JsonHelper.ToJson(points.ToArray());

		using (var www = UnityWebRequest.Post(Web.gazeTrackingApiUrl + $"?id={id}", json))
		{
			Debug.Log("Submitting resultset");
			www.SetRequestHeader("Cookie", Web.formattedCookieHeader);
			yield return www.SendWebRequest();
			submitted = true;

			points = null;
		}
	}
}
