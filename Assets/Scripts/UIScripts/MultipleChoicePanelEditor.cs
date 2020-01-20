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
	public List<InputField> answerInputs;
	public Button addAnswerButton;
	public Button doneButton;

	public bool answered;
	public string answerQuestion;
	public string[] answerAnswers;
	public int answerCorrect;

	private int answerCount;
	private const int MAXANSWERS = 6;
	private ToggleGroup toggleGroup;

	void Update()
	{
		bool questionFilled = question.text.Length > 0;
		bool answersCount = answerInputs.Count > 1;
		bool correctAnswerSelected = toggleGroup.ActiveToggles().Any();
		bool answersFilled = true;

		for (int i = 0; i < answerInputs.Count; i++)
		{
			if (answerInputs[i].text.Length == 0)
			{
				answersFilled = false;
				break;
			}
		}

		doneButton.interactable =  questionFilled && answersCount && correctAnswerSelected && answersFilled;
	}

	public void Init(string initialTitle, string[] initialAnswers = null)
	{
		toggleGroup = layoutPanelTransform.GetComponent<ToggleGroup>();
		answerInputs = new List<InputField>();

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

		foreach (var answer in answers)
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
		var answerPanel = Instantiate(answerPanelPrefab, answerWrapper);
		answerPanel.GetComponentInChildren<Toggle>().isOn = isCorrect;
		answerPanel.GetComponentInChildren<Toggle>().group = toggleGroup;
		answerPanel.GetComponentInChildren<Button>().onClick.AddListener(delegate { RemoveAnswer(answerPanel.gameObject); });

		var answerInput = answerPanel.transform.GetComponentInChildren<InputField>();
		answerInput.text = answer;
		answerInputs.Add(answerInput);

		answerPanel.transform.SetAsLastSibling();
		answerCount++;

		if (answerCount == MAXANSWERS)
		{
			addAnswerButton.interactable = false;
		}
	}

	public void RemoveAnswer(GameObject answerPanel)
	{
		answerInputs.Remove(answerPanel.transform.GetComponentInChildren<InputField>());
		Destroy(answerPanel);
		answerCount--;

		if (answerCount < MAXANSWERS)
		{
			addAnswerButton.interactable = true;
		}
	}

	public void Answer()
	{
		answered = true;
		answerQuestion = question.text;

		//NOTE(Kristof): Converting InputTexts to array of strings
		answerAnswers = new string[answerInputs.Count];
		for (var index = 0; index < answerInputs.Count; index++)
		{
			answerAnswers[index] = answerInputs[index].text;
		}
		var toggle = toggleGroup.ActiveToggles().First();
		answerCorrect = toggle.transform.parent.GetSiblingIndex();
	}
}
