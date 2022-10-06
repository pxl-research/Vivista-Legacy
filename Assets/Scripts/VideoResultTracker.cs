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
	public int correctAnswer;
	//NOTE(Simon): In case of Find Area
	public int wrongAnswersTried;
}

public class Summary
{
	public List<QuestionResult> results;
	public int score;
	public int maximum;
}

public class VideoResultTracker
{
	private static List<QuestionResult> results;
	private static Guid id;
	private static int maximumScore;

	private static bool submitted;

	public static void StartResultSet(Guid videoId, List<InteractionPointPlayer> interactionPoints)
	{
		Assert.IsTrue(results == null || submitted, "Forgot to call SubmitResultSet() before starting a new ResultSet");

		id = videoId;
		submitted = false;
		results = new List<QuestionResult>();

		foreach (var point in interactionPoints)
		{
			maximumScore += MaxScoreForInteractionType(point.type);
		}
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
			Debug.Log("Submitting resultset");
			www.SetRequestHeader("Cookie", Web.formattedCookieHeader);
			yield return www.SendWebRequest();
			submitted = true;

			results = null;
		}
	}

	public static Summary GetSummary()
	{
		int score = 0;

		foreach (var question in results)
		{
			score += MaxScoreForInteractionType(question.type) * Convert.ToInt32(question.answerChosen == question.correctAnswer);
		}

		return new Summary
		{
			results = results,
			maximum = maximumScore,
			score = score
		};
	}

	private static int MaxScoreForInteractionType(InteractionType type)
	{
		switch (type)
		{
			case InteractionType.None:
			case InteractionType.Text:
			case InteractionType.Image:
			case InteractionType.Audio:
			case InteractionType.Video:
			case InteractionType.TabularData:
			case InteractionType.Chapter:
				return 0;
			case InteractionType.MultipleChoice:
			case InteractionType.MultipleChoiceArea:
			case InteractionType.MultipleChoiceImage:
				return 1;
			case InteractionType.FindArea:
				return 1;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
