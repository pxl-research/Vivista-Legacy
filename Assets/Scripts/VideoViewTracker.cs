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
			Assert.IsNotNull(results, "Forgot to call StartResultSet() before registering a Result");

			results.Add(new ViewPeriod
			{
				start = currentStart,
				end = (float)endTimePreviousPeriod
			});

			currentStart = (float)startTime;
		}
	}

	//NOTE(Simon): We ask for the video length at the end, because some platforms (e.g. OSX) don't have this info available at the moment we would like to call Start()
	public static IEnumerator Submit(double endTime, double videoLength)
	{
		if (SceneManager.GetActiveScene().name == "Player")
		{
			results.Add(new ViewPeriod
			{
				start = currentStart,
				end = (float)endTime
			});

			Assert.IsNotNull(results, "Forgot to call Start() before submitting");

			int[] simplified = Simplify(results, (float)videoLength);

			string json = JsonHelper.ToJson(simplified);

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

	private static int[] Simplify(List<ViewPeriod> data, float length)
	{
		var bins = 100;
		var simplified = new int[bins];
		float width = length / bins;

		for (int i = 0; i < data.Count; i++)
		{
			int startBin = Mathf.FloorToInt(data[i].start / width);
			int endBin = Mathf.FloorToInt(data[i].end / width);

			for (int bin = startBin; bin <= endBin; bin++)
			{
				simplified[bin]++;
			}
		}

		return simplified;
	}
}
