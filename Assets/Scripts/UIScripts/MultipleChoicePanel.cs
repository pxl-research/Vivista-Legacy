using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class MultipleChoicePanel : MonoBehaviour
{

	public Text question;
	public string[] answers;
	public int correctAnswer;
	public Canvas canvas;
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

	// Use this for initialization
	void Start()
	{
		toggleGroup = answerPanel.GetComponent<ToggleGroup>();
	}

	// Update is called once per frame
	void Update()
	{
		if (!XRSettings.enabled)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			//NOTE(Kristof): Resetting toggle background colour
			{
				var toggles = answerPanel.GetComponentsInChildren<Toggle>();
				foreach (var tgl in toggles)
				{
					ToggleHoverEnd(tgl.gameObject);
				}
			}

			if (Physics.Raycast(ray, out hit, 150, LayerMask.GetMask("UI")))
			{
				var toggle = hit.transform.GetComponent<Toggle>();
				if (toggle)
				{
					ToggleHoverStart(toggle.gameObject);
				}

				if (Input.GetMouseButtonDown(0) && toggleGroup.AnyTogglesOn())
				{
					var button = hit.transform.GetComponent<Button>();
					if (button)
					{
						CheckAnswer();
					}
				}
			}
		}

		//NOTE(Kristof): Disable colliders on elements that aren't interactable

		foreach (var toggle in answerPanel.GetComponentsInChildren<Toggle>())
		{
			toggle.GetComponent<BoxCollider>().enabled = toggle.interactable;
		}
		checkAnswerButton.GetComponent<BoxCollider>().enabled = checkAnswerButton.interactable;
	}

	public void Init(string newQuestion, string[] newAnswers)
	{
		question.text = newQuestion;
		correctAnswer = Convert.ToInt32(newAnswers[0]);
		answers = new string[newAnswers.Length - 1];
		Array.Copy(newAnswers, 1, answers, 0, answers.Length);

		var canvasTectTransform = canvas.GetComponent<RectTransform>();

		var questionHeight = UIHelper.CalculateTextFieldHeight(question.text, question.font, question.fontSize, 400, 0);

		for (var index = 0; index < answers.Length; index++)
		{
			var answer = answers[index];
			var toggle = Instantiate(answerTogglePrefab, answerPanel);
			toggle.GetComponent<Toggle>().onValueChanged.AddListener(delegate { ToggleValueChanged(toggle); });
			toggle.GetComponent<Hittable>().onHit.AddListener(delegate { ToggleValueChangedHittable(toggle); });
			toggle.GetComponent<Hittable>().onHoverStart.AddListener(delegate { ToggleHoverStart(toggle); });
			toggle.GetComponent<Hittable>().onHoverEnd.AddListener(delegate { ToggleHoverEnd(toggle); });
			toggle.GetComponent<Toggle>().isOn = false;
			toggle.GetComponent<Toggle>().group = answerPanel.GetComponent<ToggleGroup>();

			//NOTE(Kristof): First child is question label, second child is question number
			var textComponents = toggle.transform.GetComponentsInChildren<Text>();
			var answerHeight =
				UIHelper.CalculateTextFieldHeight(textComponents[0].text, textComponents[0].font, textComponents[0].fontSize, 400, 0);
			textComponents[0].text = answer;
			textComponents[1].text = $"{index + 1})";

			toggle.GetComponent<RectTransform>().sizeDelta += new Vector2(0, answerHeight);
			answerPanel.sizeDelta += new Vector2(0, answerHeight);

			var col = toggle.AddComponent<BoxCollider>();
			if (XRSettings.enabled)
			{
				col.size = new Vector2(50, 50);
				col.center = new Vector3(35, 0, -1);
			}
			else
			{
				col.size = toggle.GetComponent<RectTransform>().sizeDelta;
				col.center = new Vector3(col.size.x / 2, 0, 0);
			}

		}

		var button = Instantiate(answerCheckButtonPrefab, answerPanel);
		checkAnswerButton = button.GetComponent<Button>();
		checkAnswerButton.interactable = false;
		button.GetComponent<Hittable>().onHit.AddListener(delegate { CheckAnswerHittable(checkAnswerButton); });

		answerPanel.sizeDelta += new Vector2(0, button.GetComponent<RectTransform>().sizeDelta.y + 10);
		canvasTectTransform.sizeDelta = new Vector2(canvasTectTransform.sizeDelta.x, questionHeight * 0.7f + answerPanel.sizeDelta.y + 10);
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}

	public void ToggleValueChanged(GameObject toggle)
	{
		if (checkAnswerButton != null)
		{
			checkAnswerButton.interactable = toggleGroup.AnyTogglesOn();
		}
		//NOTE(Kristof): background image and checkmark image
		var childImages = toggle.transform.GetComponentsInChildren<Image>();
		childImages[0].color = toggle.GetComponent<Toggle>().isOn ? orangeColour : Color.white;
		childImages[1].color = toggle.GetComponent<Toggle>().isOn ? Color.white : lightGreyColour;
	}

	public void ToggleValueChangedHittable(GameObject toggle)
	{
		if (!toggle.GetComponent<Toggle>().interactable)
		{ 
			return; 
		}
		toggle.GetComponent<Toggle>().isOn = true;
		ToggleValueChanged(toggle);
	}

	public void ToggleHoverStart(GameObject toggle)
	{
		if (!toggle.GetComponent<Toggle>().interactable)
		{
			return;
		}
		//NOTE(Kristof): background image and checkmark image
		var childImages = toggle.transform.GetComponentsInChildren<Image>();
		childImages[0].color = orangeColour;
		childImages[1].color = Color.white;
	}

	public void ToggleHoverEnd(GameObject toggle)
	{
		if (!toggle.GetComponent<Toggle>().interactable || toggle.GetComponent<Toggle>().isOn)
		{
			return;
		}
		//NOTE(Kristof): background image and checkmark image
		var childImages = toggle.transform.GetComponentsInChildren<Image>();
		childImages[0].color = Color.white;
		childImages[1].color = lightGreyColour;
	}

	public string[] GetBody()
	{
		var returnAnswers = new string[answers.Length + 1];

		returnAnswers[0] = correctAnswer.ToString();
		for (var index = 1; index <= answers.Length; index++)
		{
			returnAnswers[index] = answers[index - 1];
		}

		return returnAnswers;
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

	public void CheckAnswerHittable(Button button)
	{
		if (!button.GetComponent<Button>().interactable)
		{
			return;
		}
		CheckAnswer();
	}
}
