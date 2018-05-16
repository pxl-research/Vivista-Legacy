using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceEditor : MonoBehaviour
{

	public Canvas canvas;
	public RectTransform resizePanelRectTransform;
	public Transform layoutPanelTransform;
	public InputField question;
	public GameObject answerPanelPrefab;
	public List<InputField> answerList;
	public Button addAnswer;
	public Button done;
	
	private static int answerCount;
	private string[] answers;

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		canvas.transform.rotation = Camera.main.transform.rotation;
	}

	public void Init(Vector3 position, string initialTitle, string[] initialAnswers, bool exactPos = false)
	{
		question.text = initialTitle;
		answerCount = initialAnswers.Length;
		answers = initialAnswers;
		AddQuestion("");
		Move(position, exactPos);
	}

	public void AddQuestion(string answer)
	{
		const int firstAnswerPanelIndex = 1;
		var answerPanel = Instantiate(answerPanelPrefab, layoutPanelTransform);

		var answerInput = answerPanel.transform.GetComponentInChildren<InputField>();
		answerInput.text = answer;
		answerList.Add(answerInput);

		answerPanel.transform.SetSiblingIndex(firstAnswerPanelIndex + answerCount);
		answerCount++;

		resizePanelRectTransform.sizeDelta += new Vector2(0, 45);
		transform.position += new Vector3(0, 5.6f, 0);
	}

	public void RemoveQuestion(GameObject answerPanel)
	{
		answerList.Remove(answerPanel.transform.GetComponentInChildren<InputField>());
		Destroy(answerPanel);
		answerCount--;

		resizePanelRectTransform.sizeDelta += new Vector2(0, -45);
		transform.position += new Vector3(0, -5.6f, 0);

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

}
