using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

[Serializable]
public class QuestionResult
{
	public InteractionType type;
	public int interactionId;
	public int answerChosen;
	//NOTE(Simon): In case of Find Area
	public int wrongAnswersTried;
}

public class VideoResultTracker : MonoBehaviour
{
	private static List<QuestionResult> results;
	private static Guid id;

	private static bool submitted;

	public static void StartResultSet(Guid videoId)
	{
		Assert.IsTrue(results != null && !submitted, "Forgot to call SubmitResultSet() before starting a new ResultSet");

		id = videoId;
		results = new List<QuestionResult>();
	}

	public static void RegisterQuestionResult(QuestionResult result)
	{
		Assert.IsNotNull(results, "Forgot to call StartResultSet() before registering a Result");

		results.Add(result);
	}

	public static IEnumerator SubmitResultSet()
	{
		Assert.IsNotNull(results, "Forgot to call StartResultSet() before submitting a Result");

		string json = JsonHelper.ToJson(results.ToArray());

		using (var www = UnityWebRequest.Post(Web.videoResultApiUrl + $"?id={id}", json))
		{
			www.SetRequestHeader("Cookie", Web.formattedCookieHeader);
			yield return www.SendWebRequest();
			submitted = true;

			results = null;
		}
	}
}
