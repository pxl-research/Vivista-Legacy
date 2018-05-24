using UnityEngine;
using UnityEngine.UI;

public class AnswerPanel : MonoBehaviour
{
	private MultipleChoicePanelEditor multipleChoicePanelEditor;

	void Start()
	{
		multipleChoicePanelEditor = transform.root.gameObject.GetComponent<MultipleChoicePanelEditor>();
		transform.GetComponentInChildren<Toggle>().group = transform.parent.GetComponent<ToggleGroup>();
		transform.GetComponentInChildren<Button>().onClick.AddListener(delegate { multipleChoicePanelEditor.RemoveQuestion(gameObject);});
	}
}
