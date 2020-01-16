using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultipleChoicePanel : MonoBehaviour
{

	public Text question;
	public string[] answers;
	public int correctAnswer;
	public RectTransform answerPanel;

	public GameObject answerTogglePrefab;
	public GameObject answerCheckButtonPrefab;

	public Sprite crossImage;

	private ToggleGroup toggleGroup;
	private Button checkAnswerButton;
	private int selectedIndex;

	private readonly Color orangeColour = new Color(1, 0.8f, 0.42f);
	private readonly Color lightGreyColour = new Color(0.78f, 0.78f, 0.78f);
	private readonly Color darkGreyColour =  new Color(0.48f, 0.48f, 0.48f);
	private readonly Color greenColour = new Color(0.19f, 0.39f, 0.15f);

	//TODO(Simon): show question solution at init
	void Start()
	{
		toggleGroup = answerPanel.GetComponent<ToggleGroup>();
	}

	void Update()
	{
		if (SceneManager.GetActiveScene().name == "Editor")
		{
			GetComponent<Canvas>().transform.rotation = Camera.main.transform.rotation;
		}
	}

	public void Init(string newQuestion, string[] newAnswers)
	{
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
			toggle.onValueChanged.AddListener(delegate { ToggleValueChanged(toggleGo); });
			toggle.isOn = false;
			toggle.group = toggleGroup;

			//NOTE(Kristof): First child is answer number, second child is answer text
			var textComponents = toggleGo.transform.GetComponentsInChildren<Text>();
			textComponents[0].text = $"{index + 1})";
			textComponents[1].text = answer;
		}

		var button = Instantiate(answerCheckButtonPrefab, answerPanel);
		checkAnswerButton = button.GetComponent<Button>();
		checkAnswerButton.interactable = false;
		checkAnswerButton.onClick.AddListener(CheckAnswer);
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		GetComponent<Canvas>().GetComponent<RectTransform>().position = position;
	}

	public void ToggleValueChanged(GameObject toggleGo)
	{
		if (checkAnswerButton != null)
		{
			checkAnswerButton.interactable = toggleGroup.AnyTogglesOn();
		}

		//NOTE(Kristof): background image and checkmark image
		var childImages = toggleGo.transform.GetComponentsInChildren<Image>();
		var toggle = toggleGo.GetComponent<Toggle>();
		childImages[0].color = toggle.isOn ? orangeColour : Color.white;
		childImages[1].color = toggle.isOn ? Color.white : lightGreyColour;
	}

	public void CheckAnswer()
	{
		var toggle = toggleGroup.ActiveToggles().ElementAt(0);
		selectedIndex = toggle.transform.GetSiblingIndex();
		
		var toggles = answerPanel.GetComponentsInChildren<Toggle>();
		for (var index = 0; index < toggles.Length; index++)
		{
			toggles[index].interactable = false;
			if (index == correctAnswer)
			{
				toggles[index].transform.GetComponentInChildren<Image>().color = greenColour;
				toggles[index].transform.GetComponentsInChildren<Text>()[0].color = Color.white;
				toggles[index].transform.GetComponentsInChildren<Text>()[1].color = Color.white;
			}
			else
			{
				toggles[index].transform.GetComponentsInChildren<Image>()[0].color = lightGreyColour;
				toggles[index].transform.GetComponentsInChildren<Image>()[1].sprite = crossImage;
				toggles[index].transform.GetComponentsInChildren<Text>()[0].color = darkGreyColour;
				toggles[index].transform.GetComponentsInChildren<Text>()[1].color = darkGreyColour;
			}
		}

		if (selectedIndex != correctAnswer)
		{
			foreach (var txt in toggles[selectedIndex].transform.GetComponentsInChildren<Text>())
			{
				txt.color = Color.red;
			}
			toggles[selectedIndex].transform.GetComponentsInChildren<Image>()[1].color = Color.red;
		}
		else
		{
			toggles[selectedIndex].transform.GetComponentsInChildren<Image>()[1].color = greenColour;
		}

		checkAnswerButton.interactable = false;
	}
}
