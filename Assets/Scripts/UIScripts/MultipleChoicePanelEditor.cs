using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoicePanelEditor : MonoBehaviour
{

	public Transform layoutPanelTransform;
	public InputField question;
	public GameObject answerPanelPrefab;
	public RectTransform answerWrapper;
	public List<InputField> answerList;
	public Button addAnswer;
	public Button done;

	public bool answered;
	public string answerQuestion;
	public string[] answerAnswers;
	public int answerCorrect;

	private int answerCount;
	private const int MAXANSWERS = 6;

	void Update()
	{
		done.interactable = !(question.text.Length < 1);
	}

	public void Init(string initialTitle, string[] initialAnswers = null)
	{
		answerList = new List<InputField>();
		answerList.Clear();

		var answers = new string[0];
		if (initialAnswers == null)
		{
			answerCorrect = 0;
		}
		else
		{
			answerCorrect = Convert.ToInt32(initialAnswers[0]);
			answers = new string[initialAnswers.Length - 1];
			Array.Copy(initialAnswers, 1, answers, 0, answers.Length);
		}

		question.text = initialTitle;
		InitAnswers(answers);
	}

	private void InitAnswers(string[] initialAnswers)
	{
		foreach (var answer in initialAnswers)
		{
			AddAnswer(answer, answerCount == answerCorrect);
		}
	}

	public void AddAnswer()
	{
		AddAnswer("");
	}

	public void AddAnswer(string answer, bool isCorrect = false)
	{
		var answerPanel = Instantiate(answerPanelPrefab, answerWrapper).GetComponent<AnswerPanel>();
		answerPanel.editor = this;
		answerPanel.GetComponentInChildren<Toggle>().isOn = isCorrect;

		var answerInput = answerPanel.transform.GetComponentInChildren<InputField>();
		answerInput.text = answer;
		answerList.Add(answerInput);

		answerPanel.transform.SetAsLastSibling();
		answerCount++;

		if (answerCount == MAXANSWERS)
		{
			addAnswer.interactable = false;
		}
	}

	public void RemoveAnswer(GameObject answerPanel)
	{
		answerList.Remove(answerPanel.transform.GetComponentInChildren<InputField>());
		Destroy(answerPanel);
		answerCount--;

		if (answerCount < MAXANSWERS)
		{
			addAnswer.interactable = true;
		}
	}

	public void Answer()
	{
		answered = true;
		answerQuestion = question.text;

		//NOTE(Kristof): Converting InputTexts to array of strings
		answerAnswers = new string[answerList.Count];
		for (var index = 0; index < answerList.Count; index++)
		{
			answerAnswers[index] = answerList[index].text;
		}
		var toggle = layoutPanelTransform.GetComponent<ToggleGroup>().ActiveToggles().First();
		answerCorrect = toggle.transform.parent.GetSiblingIndex();
	}
}
