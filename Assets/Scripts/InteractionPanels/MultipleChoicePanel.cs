using System;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoicePanel : MonoBehaviour
{
	public Text question;
	public string[] answers;
	public int correctAnswer;
	public RectTransform answerPanel;

	public GameObject answerTogglePrefab;

	public Sprite crossImage;

	private ToggleGroup toggleGroup;
	private Button checkAnswerButton;
	private int selectedIndex;

	private readonly Color lightGreyColour = new Color(0.78f, 0.78f, 0.78f);
	private readonly Color darkGreyColour =  new Color(0.48f, 0.48f, 0.48f);
	private readonly Color greenColour = new Color(0.19f, 0.39f, 0.15f);

	public void Init(string newQuestion, string[] newAnswers)
	{
		var existingToggles = answerPanel.GetComponentsInChildren<Toggle>();

		for (int i = 0; i < existingToggles.Length; i++)
		{
			DestroyImmediate(existingToggles[i].gameObject);
		}

		toggleGroup = answerPanel.GetComponent<ToggleGroup>();
		question.text = newQuestion;
		correctAnswer = Convert.ToInt32(newAnswers[0]);
		answers = new string[newAnswers.Length - 1];

		//NOTE(Simon): newAnswers from index 1, because index 0 contains the correct answer
		Array.Copy(newAnswers, 1, answers, 0, answers.Length);

		for (var index = 0; index < answers.Length; index++)
		{
			var answer = answers[index];
			var toggleGo = Instantiate(answerTogglePrefab, answerPanel);
			var toggle = toggleGo.GetComponent<Toggle>();
			toggle.isOn = false;
			toggle.interactable = false;
			toggle.group = toggleGroup;

			//NOTE(Kristof): First child is answer number, second child is answer text
			var textComponents = toggleGo.transform.GetComponentsInChildren<Text>();
			textComponents[0].text = $"{index + 1})";
			textComponents[1].text = answer;
		}

		var toggles = answerPanel.GetComponentsInChildren<Toggle>();
		for (int i = 0; i < toggles.Length; i++)
		{
			if (i == correctAnswer)
			{
				toggles[i].transform.GetComponentInChildren<Image>().color = greenColour;
				toggles[i].transform.GetComponentsInChildren<Text>()[0].color = Color.white;
				toggles[i].transform.GetComponentsInChildren<Text>()[1].color = Color.white;
			}
			else
			{
				toggles[i].transform.GetComponentsInChildren<Image>()[0].color = lightGreyColour;
				toggles[i].transform.GetComponentsInChildren<Image>()[1].sprite = crossImage;
				toggles[i].transform.GetComponentsInChildren<Text>()[0].color = darkGreyColour;
				toggles[i].transform.GetComponentsInChildren<Text>()[1].color = darkGreyColour;
			}
		}

	}
}
