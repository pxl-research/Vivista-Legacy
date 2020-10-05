using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceImagePanel : MonoBehaviour
{
	public Text question;
	public List<string> answers;
	public int correct;
	public RectTransform imageList;
	public List<MultipleChoiceImageEntry> entries;

	public GameObject multipleChoiceImageEntryPrefab;

	public void OnEnable()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			StartCoroutine(entries[i].SetUrl(answers[i], true));
		}
	}

	public void Init(string newQuestion, List<string> newAnswers, int newCorrect)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			Destroy(entries[i].gameObject);
		}

		entries.Clear();

		question.text = newQuestion;
		correct = newCorrect;
		answers = newAnswers;

		for (int i = 0; i < answers.Count; i++)
		{
			var entry = Instantiate(multipleChoiceImageEntryPrefab, imageList).GetComponent<MultipleChoiceImageEntry>();
			entry.toggle.interactable = false;

			entry.toggle.SetIsOnWithoutNotify(i == correct);
			entries.Add(entry);
		}
	}
}
