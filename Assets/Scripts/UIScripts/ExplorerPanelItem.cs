using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExplorerPanelItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public Image background;
	public Button button;
	public RectTransform iconHolder;
	public RawImage icon;
	public Text filename;
	public Text date;
	public Text fileType;
	public Text fileSize;
	public Text fileResolution;
	public ExplorerPanel explorerPanel;

	private float timer;
	private bool counting;

	public void OnPointerEnter(PointerEventData eventData)
	{
		timer = 0;
		counting = true;
	}

	public void Update()
	{
		if (counting)
		{
			timer += Time.deltaTime;
			if (timer > 0.3f)
			{
				StartCoroutine(explorerPanel.ShowPreview(transform.GetSiblingIndex()));
				counting = false;
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		counting = false;

		explorerPanel.HidePreview();
	}
}
