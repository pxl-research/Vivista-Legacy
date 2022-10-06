using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoicePanelSphere : MonoBehaviour
{
	public Text question;
	public Button answerButton;
	public string[] answers;
	public int correctAnswer;
	public RectTransform answerPanel;

	public GameObject answerTogglePrefab;

	public Sprite crossImage;

	private ToggleGroup toggleGroup;
	private int selectedIndex;

	private readonly Color orangeColour = new Color(1, 0.8f, 0.42f);
	private readonly Color lightGreyColour = new Color(0.78f, 0.78f, 0.78f);
	private readonly Color darkGreyColour = new Color(0.48f, 0.48f, 0.48f);
	private readonly Color greenColour = new Color(0.19f, 0.39f, 0.15f);

	private int id;

	public void Init(string newQuestion, int correctAnswer, string[] newAnswers, int id)
	{
		toggleGroup = answerPanel.GetComponent<ToggleGroup>();
		question.text = newQuestion;
		this.id = id;

		if (newAnswers == null || newAnswers.Length <= 1)
		{
			return;
		}

		this.correctAnswer = correctAnswer;
		answers = newAnswers;

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

		answerButton.interactable = false;
		answerButton.onClick.AddListener(CheckAnswer);
	}

	public void ToggleValueChanged(GameObject toggleGo)
	{
		if (answerButton != null)
		{
			answerButton.interactable = toggleGroup.AnyTogglesOn();
		}

		//NOTE(Kristof): background image and checkmark image
		var childImages = toggleGo.transform.GetComponentsInChildren<Image>();
		var toggle = toggleGo.GetComponent<Toggle>();
		childImages[1].color = toggle.isOn ? orangeColour : lightGreyColour;
	}

	public void CheckAnswer()
	{
		var toggle = toggleGroup.ActiveToggles().ElementAt(0);
		selectedIndex = toggle.transform.GetSiblingIndex();

		var toggles = answerPanel.GetComponentsInChildren<Toggle>();
		for (int i = 0; i < toggles.Length; i++)
		{
			toggles[i].interactable = false;
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

		VideoResultTracker.RegisterQuestionResult(new QuestionResult
		{
			type = InteractionType.MultipleChoice,
			interactionId = id,
			answerChosen = selectedIndex,
			correctAnswer = correctAnswer
		});

		answerButton.interactable = false;
	}
}
