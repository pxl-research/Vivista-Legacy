using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[Serializable]
public struct ViewPeriod
{
	public float start;
	public float end;
}

public class VideoViewTracker
{
	private static List<ViewPeriod> results;
	private static Guid id;

	private static bool submitted;
	private static float currentStart;

	public static void Start(Guid videoId, double startTime)
	{
		if (SceneManager.GetActiveScene().name == "Player")
		{
			Debug.Log($"Starting new VideoViewTracking at {startTime}");
			Assert.IsTrue(results == null || submitted, "Forgot to call Submit() before starting a new Video View Tracking session");

			currentStart = (float)startTime;
			id = videoId;
			submitted = false;
			results = new List<ViewPeriod>();
		}
	}

	public static void StartNewPeriod(double endTimePreviousPeriod, double startTime)
	{
		if (SceneManager.GetActiveScene().name == "Player")
		{
			Debug.Log("Added new period to VideoViewTracking");
			Assert.IsNotNull(results, "Forgot to call StartResultSet() before registering a Result");

			results.Add(new ViewPeriod
			{
				start = currentStart,
				end = (float)endTimePreviousPeriod
			});

			currentStart = (float)startTime;
		}
	}

	public static IEnumerator Submit(double endTime)
	{
		if (SceneManager.GetActiveScene().name == "Player")
		{
			results.Add(new ViewPeriod
			{
				start = currentStart,
				end = (float)endTime
			});

			Assert.IsNotNull(results, "Forgot to call Start() before submitting");

			string json = JsonHelper.ToJson(results.ToArray());

			Debug.Log(JsonHelper.ToJson(results.ToArray(), true));

			using (var www = UnityWebRequest.Post(Web.videoViewApiUrl + $"?id={id}", json))
			{
				Debug.Log("Submitting ViewViewTracking Results");
				www.SetRequestHeader("Cookie", Web.formattedCookieHeader);
				yield return www.SendWebRequest();
				submitted = true;

				results = null;
			}
		}
	}
}
