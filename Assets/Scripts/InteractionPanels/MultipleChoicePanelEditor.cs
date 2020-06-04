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

	public bool answered;
	public string answerQuestion;
	public string[] answerAnswers;
	public int answerCorrect;

	private int answerCount;
	private const int MAXANSWERS = 6;
	private ToggleGroup2 toggleGroup;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
	}

	public void Init(string initialQuestion, string[] initialAnswers = null)
	{
		toggleGroup = layoutPanelTransform.GetComponent<ToggleGroup2>();
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

		question.text = initialQuestion;

		foreach (var answer in answers)
		{
			AddAnswer(answer, answerCount == answerCorrect);
		}

		question.onValueChanged.AddListener(_ => OnInputChangeColor(question));
		addAnswerButton.onClick.AddListener(() => OnButtonChangeColor(addAnswerButton));
	}

	public void AddAnswer()
	{
		AddAnswer("");
	}

	public void AddAnswer(string answer, bool isCorrect = false)
	{
		var answerPanel = Instantiate(answerPanelPrefab, answerWrapper);
		var toggle = answerPanel.GetComponentInChildren<Toggle>();
		toggle.isOn = isCorrect;
		toggle.group = toggleGroup;
		toggle.onValueChanged.AddListener(ResetTogglesColor);
		answerPanel.GetComponentInChildren<Button>().onClick.AddListener(delegate { RemoveAnswer(answerPanel.gameObject); });

		var answerInput = answerPanel.transform.GetComponentInChildren<InputField>();
		answerInput.text = answer;
		answerInputs.Add(answerInput);
		answerInput.onValueChanged.AddListener(delegate { OnInputChangeColor(answerInput); });

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
		bool errors = false;

		if (String.IsNullOrEmpty(question.text))
		{
			question.image.color = errorColor;
			errors = true;
		}

		if (answerInputs.Count < 2)
		{
			addAnswerButton.image.color = errorColor;
			errors = true;
		}

		foreach (var input in answerInputs)
		{
			if (String.IsNullOrEmpty(input.text))
			{
				input.image.color = errorColor;
				errors = true;
			}
		}
		
		var toggles = toggleGroup.GetAllToggles();
		if (!toggleGroup.AnyTogglesOn())
		{
			foreach (var toggle in toggles)
			{
				toggle.image.color = errorColor;
				errors = true;
			}
		}

		if (!errors)
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

	public void OnInputChangeColor(InputField input)
	{
		input.image.color = Color.white;
	}

	public void ResetTogglesColor(bool _)
	{
		var toggles = toggleGroup.GetAllToggles();
		foreach (var toggle in toggles)
		{
			toggle.image.color = Color.white;
		}
	}

	public void OnButtonChangeColor(Button button)
	{
		button.image.color = Color.white;
	}
}
