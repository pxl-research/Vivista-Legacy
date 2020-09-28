using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour
{
	public Text title;
	public List<string> imageURLs;
	public ScrollRect imageScrollRect;
	public RectTransform imagePanelContent;
	public List<ImagePanelImage> images;
	public Button prevButton;
	public Button nextButton;

	private int imageIndex;

	public GameObject imagePanelImagePrefab;

	public void OnEnable()
	{
		//HACK(Simon): Fixes a bug where no image is loaded on first opening of this panel.
		if (images.Count > 0)
		{
			StartCoroutine(images[imageIndex].LoadImage());
		}
	}

	public void Init(string newTitle, List<string> urls)
	{
		prevButton.onClick.AddListener(PrevImage);
		nextButton.onClick.AddListener(NextImage);

		title.text = newTitle;
		imageURLs = urls;
		foreach (var url in imageURLs)
		{
			AddNewImage(url);
		}
		if (images.Count > 0)
		{
			SetIndex(0);
		}
		imageIndex = 0;
		EnableButtons();

		//NOTE(Simon): resize title to fit all text
		var titleRect = title.GetComponent<RectTransform>();
		float newHeight = UIHelper.CalculateTextFieldHeight(title.text, title.font, title.fontSize, titleRect.sizeDelta.x, 30);
		titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);
		
		imagePanelContent.offsetMin = new Vector2(imagePanelContent.offsetMin.x, 0);
		imagePanelContent.offsetMax = new Vector2(imagePanelContent.offsetMax.x, 0);
	}

	private void AddNewImage(string url)
	{
		var newImage = Instantiate(imagePanelImagePrefab, imagePanelContent, false);
		var script = newImage.GetComponent<ImagePanelImage>();
		script.SetMaxSize(imagePanelContent.rect.size);
		script.SetURL(url);
		images.Add(script);
	}

	public void SetIndex(int index)
	{
		if (gameObject.activeInHierarchy)
		{ 
			StartCoroutine(images[index].LoadImage());
			ScrollTo(images[index].GetComponent<RectTransform>());
			EnableButtons();
		}
	}

	public void NextImage()
	{
		if (imageIndex < images.Count - 1)
		{
			SetIndex(++imageIndex);
		}
	}
	
	public void PrevImage()
	{
		if (imageIndex > 0)
		{
			SetIndex(--imageIndex);
		}
	}

	private void ScrollTo(RectTransform target)
	{
		imagePanelContent.anchoredPosition =
			(Vector2)imageScrollRect.transform.InverseTransformPoint(imagePanelContent.position)
			- (Vector2)imageScrollRect.transform.InverseTransformPoint(target.position);
	}

	private void EnableButtons()
	{
		prevButton.gameObject.SetActive(imageIndex != 0);
		nextButton.gameObject.SetActive(imageIndex != images.Count - 1);
	}
}
