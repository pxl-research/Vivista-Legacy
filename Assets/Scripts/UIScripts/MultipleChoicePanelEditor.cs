using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoicePanelEditor : MonoBehaviour
{

	public Canvas canvas;
	public RectTransform resizePanelRectTransform;
	public Transform layoutPanelTransform;
	public InputField question;
	public GameObject answerPanelPrefab;
	public List<InputField> answerList;
	public Button addAnswer;
	public Button done;

	public bool answered;
	public string answerQuestion;
	public string[] answerAnswers;
	public int answerCorrect;

	private int answerCount;
	private const int MAXANSWERS = 6;
	private const int firstAnswerPanelIndex = 1;

	// Update is called once per frame
	void Update()
	{
		canvas.transform.rotation = Camera.main.transform.rotation;
		done.interactable = !(question.text.Length < 1);
	}

	public void Init(Vector3 position, string initialTitle, string[] initialAnswers = null, bool exactPos = false)
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
		Move(position, exactPos);
	}

	private void InitAnswers(string[] initialAnswers)
	{
		foreach (var answer in initialAnswers)
		{
			AddQuestion(answer, answerCount == answerCorrect);
		}
	}

	public void Move(Vector3 position, bool exactPos)
	{
		Vector3 newPos;

		if (exactPos)
		{
			newPos = position;
		}
		else
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
			newPos.y += 9.5f;
		}
		canvas.GetComponent<RectTransform>().position = newPos;
	}

	public void AddQuestion(string answer)
	{
		AddQuestion(answer, answerCount == answerCorrect);
	}

	public void AddQuestion(string answer, bool isCorrect = false)
	{
		var answerPanel = Instantiate(answerPanelPrefab, layoutPanelTransform);

		answerPanel.GetComponentInChildren<Toggle>().isOn = isCorrect;

		var answerInput = answerPanel.transform.GetComponentInChildren<InputField>();
		answerInput.text = answer;
		answerList.Add(answerInput);

		answerPanel.transform.SetSiblingIndex(firstAnswerPanelIndex + answerCount);
		answerCount++;

		if (answerCount == MAXANSWERS)
		{
			addAnswer.interactable = false;
		}

		resizePanelRectTransform.sizeDelta += new Vector2(0, 45);
		transform.position += new Vector3(0, 5.6f, 0);
	}

	public void RemoveQuestion(GameObject answerPanel)
	{
		answerList.Remove(answerPanel.transform.GetComponentInChildren<InputField>());
		Destroy(answerPanel);
		answerCount--;

		if (answerCount < MAXANSWERS)
		{
			addAnswer.interactable = true;
		}

		resizePanelRectTransform.sizeDelta += new Vector2(0, -45);
		transform.position += new Vector3(0, -5.6f, 0);

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
		var toggle = layoutPanelTransform.GetComponent<ToggleGroup>().ActiveToggles().ElementAt(0);
		answerCorrect = toggle.transform.parent.GetSiblingIndex() - firstAnswerPanelIndex;
	}

}
