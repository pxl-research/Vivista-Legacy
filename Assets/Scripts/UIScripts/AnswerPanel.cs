using UnityEngine;
using UnityEngine.UI;

public class AnswerPanel : MonoBehaviour
{
	public MultipleChoicePanelEditor editor;

	void Start()
	{
		transform.GetComponentInChildren<Toggle>().group = transform.parent.GetComponent<ToggleGroup>();
		transform.GetComponentInChildren<Button>().onClick.AddListener(delegate { editor.RemoveAnswer(gameObject);});
	}
}
