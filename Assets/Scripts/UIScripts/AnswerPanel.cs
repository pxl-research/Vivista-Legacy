using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnswerPanel : MonoBehaviour
{
	private MultipleChoiceEditor multipleChoiceEditor;

	void Start()
	{
		multipleChoiceEditor = transform.root.gameObject.GetComponent<MultipleChoiceEditor>();
	}

	public void RemoveSelfFromAnswers()
	{
		multipleChoiceEditor.RemoveQuestion(gameObject);
	}
}
