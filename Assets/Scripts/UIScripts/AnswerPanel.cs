using UnityEngine;
using UnityEngine.UI;

public class AnswerPanel : MonoBehaviour
{
	private MultipleChoicePanelEditor multipleChoicePanelEditor;

	void Start()
	{
		multipleChoicePanelEditor = transform.root.gameObject.GetComponent<MultipleChoicePanelEditor>();
		transform.GetComponentInChildren<Toggle>().group = transform.parent.GetComponent<ToggleGroup>();
	}

	public void RemoveSelfFromAnswers()
	{
		multipleChoicePanelEditor.RemoveQuestion(gameObject);
	}
}
