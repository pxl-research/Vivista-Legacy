using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceImagePanelSphere : MonoBehaviour
{
	public Text question;
	public Button answerButton;
	public List<string> answers;
	public int correct;
	private int selected;
	public RectTransform imageList;
	public List<MultipleChoiceImageSphereEntry> entries;

	public GameObject multipleChoiceImageEntryPrefab;

	private readonly Color orangeColour = new Color(1, 0.8f, 0.42f);
	private readonly Color lightGreyColour = new Color(0.78f, 0.78f, 0.78f);
	private readonly Color greenColour = new Color(0.19f, 0.39f, 0.15f);

	private int id;

	public void OnEnable()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			StartCoroutine(entries[i].SetUrl(answers[i]));
		}
	}

	public void Init(string newQuestion, List<string> newAnswers, int newCorrect, int id)
	{
		question.text = newQuestion;
		correct = newCorrect;
		answers = newAnswers;
		this.id = id;

		for (int i = 0; i < answers.Count; i++)
		{
			var go = Instantiate(multipleChoiceImageEntryPrefab, imageList);
			var entry = go.GetComponent<MultipleChoiceImageSphereEntry>();
			int index = i;
			entry.button.targetGraphic.color = new Color(0, 0, 0, 0);
			entry.button.onClick.AddListener(() => OnSelectAnswer(index));
			entries.Add(entry);
		}

		answerButton.onClick.AddListener(OnAnswer);
	}

	public void OnSelectAnswer(int index)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			entries[i].button.targetGraphic.color = new Color(0, 0, 0, 0);
		}

		entries[index].button.targetGraphic.color = orangeColour;
		selected = index;
	}

	public void OnAnswer()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			entries[i].button.interactable = false;
			entries[i].button.targetGraphic.color = lightGreyColour;
		}

		if (selected == correct)
		{
			entries[selected].button.targetGraphic.color = greenColour;
		}
		else
		{
			entries[selected].button.targetGraphic.color = Color.red;
			entries[correct].button.targetGraphic.color = greenColour;
		}

		VideoResultTracker.RegisterQuestionResult(new QuestionResult
		{
			type = InteractionType.MultipleChoiceImage,
			interactionId = id,
			answerChosen = selected
		});
	}
}
