using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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

	private ToggleGroup toggleGroup;
	private Button checkAnswerButton;
	private int selectedIndex;

	private Color orangeColour = new Color(1, 0.8f, 0.42f);

	// Use this for initialization
	void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		if (!XRSettings.enabled)
		{
			canvas.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}

		toggleGroup = answerPanel.GetComponent<ToggleGroup>();
	}

	// Update is called once per frame
	void Update()
	{
		// NOTE(Kristof): Turning every frame only needs to happen in Editor
		if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		}

		//NOTE(Kristof): Interacting with the with mouse (Editor and Player without VR)
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

				if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("UI")))
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
		}
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
			textComponents[1].text = String.Format("{0})", index + 1);

			toggle.GetComponent<RectTransform>().sizeDelta += new Vector2(0, answerHeight);
			answerPanel.sizeDelta += new Vector2(0, answerHeight);

			var col = toggle.AddComponent<BoxCollider>();
			col.size = toggle.GetComponent<RectTransform>().sizeDelta;
			if (!XRSettings.enabled)
			{
				//NOTE(Kristof): The collider needs a Z value to work properly with Raycasts from moouse
				col.size = new Vector3(col.size.x, col.size.y, 100);
			}
			col.center = new Vector3(col.size.x / 2, 0, 0);
		}

		var button = Instantiate(answerCheckButtonPrefab, answerPanel);
		checkAnswerButton = button.GetComponent<Button>();
		checkAnswerButton.interactable = false;
		button.GetComponent<Hittable>().onHit.AddListener(delegate { CheckAnswerHittable(checkAnswerButton); });

		answerPanel.sizeDelta += new Vector2(0, button.GetComponent<RectTransform>().sizeDelta.y + 10);
		answerPanel.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
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
		toggle.GetComponentInChildren<Image>().color = toggle.GetComponent<Toggle>().isOn ? orangeColour : Color.white;
	}

	public void ToggleValueChangedHittable(GameObject toggle)
	{
		if (!toggle.GetComponent<Toggle>().interactable) return;
		toggle.GetComponent<Toggle>().isOn = true;
		ToggleValueChanged(toggle);
	}

	public void ToggleHoverStart(GameObject toggle)
	{
		if (!toggle.GetComponent<Toggle>().interactable) return;
		toggle.transform.GetComponentInChildren<Image>().color = orangeColour;
	}

	public void ToggleHoverEnd(GameObject toggle)
	{
		if (!toggle.GetComponent<Toggle>().interactable || toggle.GetComponent<Toggle>().isOn) return;
		toggle.transform.GetComponentInChildren<Image>().color = Color.white;
	}

	public string[] GetBodyStringArray()
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
				//NOTE(Kristof): Green colour
				toggles[index].transform.GetComponentInChildren<Image>().color = new Color(0.19f, 0.39f, 0.15f);
				toggles[index].transform.GetComponentsInChildren<Text>()[0].color = Color.white;
				toggles[index].transform.GetComponentsInChildren<Text>()[1].color = Color.white;
			}
			else
			{
				//NOTE(Kristof): Grey colour
				toggles[index].transform.GetComponentInChildren<Image>().color = new Color(0.78f, 0.78f, 0.78f);
				//NOTE(Kristof): Darker grey colour
				toggles[index].transform.GetComponentsInChildren<Text>()[0].color = new Color(0.48f, 0.48f, 0.48f);
				toggles[index].transform.GetComponentsInChildren<Text>()[1].color = new Color(0.48f, 0.48f, 0.48f);
			}
		}

		if (selectedIndex != correctAnswer)
		{
			foreach (var txt in toggles[selectedIndex].transform.GetComponentsInChildren<Text>())
			{
				txt.color = Color.red;
			}
		}

		checkAnswerButton.interactable = false;
	}

	public void CheckAnswerHittable(Button button)
	{
		if (!button.GetComponent<Button>().interactable) return;
		CheckAnswer();
	}
}
