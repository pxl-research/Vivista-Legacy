using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExplorerPanelItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public RectTransform rect;
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
	//NOTE(Simon): This is an anchor point for x, and pixel value for y
	private static Vector2 itemMinSize = new Vector2(30, 24);
	private static float anchor = 0.05f;
	public static Vector2 thumbnailMaxSize = new Vector2(26, 20);

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

	public void SetHeight(float factor)
	{
		var newSize = itemMinSize * (1 + factor * 2f);

		//NOTE(Simon): Set height of entire item
		rect.sizeDelta = new Vector2(rect.sizeDelta.x, newSize.y);
		//NOTE(Simon): Set max size of thumbnail
		thumbnailMaxSize = newSize - new Vector2(4, 4);
		icon.rectTransform.sizeDelta = MathHelper.ScaleRatio(new Vector2(icon.texture.width, icon.texture.height), iconHolder.rect.size);
		

		//NOTE(Simon): Set anchor of thumbnail holder
		iconHolder.anchorMax = new Vector2(anchor * (1 + factor), iconHolder.anchorMax.y);
		//NOTE(Simon): Set anchor of filename holder (to accomodate space taken by thumbnail)
		filename.rectTransform.anchorMin = new Vector2(anchor * (1 + factor), filename.rectTransform.anchorMin.y);

	}

	public void OnPointerExit(PointerEventData eventData)
	{
		counting = false;

		explorerPanel.HidePreview();
	}
}
