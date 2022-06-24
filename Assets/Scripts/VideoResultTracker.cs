using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

public class VideoResultTracker : MonoBehaviour
{
	private static Dictionary<int, int> results;
	private static Guid id;

	private static bool submitted;

	public static void StartResultSet(Guid videoId)
	{
		Assert.IsTrue(results != null && !submitted, "Forgot to call SubmitResultSet() before starting a new ResultSet");

		id = videoId;
		results = new Dictionary<int, int>();
	}

	public static void RegisterQuestionResult(int interactionId, int answer)
	{
		Assert.IsNotNull(results, "Forgot to call StartResultSet() before registering a Result");

		results.Add(interactionId, answer);
	}

	public static void RegisterQuestionResult(int interactionId, bool correct)
	{
		Assert.IsNotNull(results, "Forgot to call StartResultSet() before registering a Result");

		results.Add(interactionId, correct ? 1 : 0);
	}

	public static IEnumerator SubmitResultSet()
	{
		Assert.IsNotNull(results, "Forgot to call StartResultSet() before submitting a Result");

		string json = JsonHelper.ToJson(results);

		using (var www = UnityWebRequest.Post(Web.videoResultApiUrl + $"?id={id}", json))
		{
			www.SetRequestHeader("Cookie", Web.formattedCookieHeader);
			yield return www.SendWebRequest();
			submitted = true;
		}
	}
}
