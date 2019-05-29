using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class ImagePanel : MonoBehaviour
{
	public Text title;
	public List<string> imageURLs;
	public Canvas canvas;
	public ScrollRect imageScrollRect;
	public RectTransform imagePanel;
	public List<ImagePanelImage> images;
	public Button prevButton;
	public Button nextButton;

	private int imageIndex;

	public GameObject imagePanelImagePrefab;

	void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		if (!XRSettings.enabled)
		{
			canvas.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}
	}

	void Update()
	{
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
	}

	void OnEnable()
	{
		imageIndex = 0;
	}

	public void Init(string newTitle, List<string> urls)
	{
		title.text = newTitle;
		imageURLs = urls;
		foreach (var url in imageURLs)
		{
			AddNewImage(url);
		}
		if (images.Count > 0)
		{
			images[0].LoadImage();
		}
		imageIndex = 0;
		EnableButtons();

		//NOTE(Simon): resize title to fit all text
		var titleRect = title.GetComponent<RectTransform>();
		float newHeight = UIHelper.CalculateTextFieldHeight(title.text, title.font, title.fontSize, titleRect.sizeDelta.x, 30);
		titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);
	}

	private void AddNewImage(string url)
	{
		var newImage = Instantiate(imagePanelImagePrefab, imagePanel, false);
		var script = newImage.GetComponent<ImagePanelImage>();
		script.SetURL(url);
		images.Add(script);
	}

	public void NextImage()
	{
		if (imageIndex < images.Count - 1)
		{
			imageIndex++;
		}
		images[imageIndex].LoadImage();
		ScrollTo(images[imageIndex].GetComponent<RectTransform>());
	}
	
	public void PrevImage()
	{
		if (imageIndex > 0)
		{
			imageIndex--;
		}
		images[imageIndex].LoadImage();
		ScrollTo(images[imageIndex].GetComponent<RectTransform>());
	}

	private void ScrollTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();

		imagePanel.anchoredPosition =
			(Vector2)imageScrollRect.transform.InverseTransformPoint(imagePanel.position)
			- (Vector2)imageScrollRect.transform.InverseTransformPoint(target.position);
		EnableButtons();
	}

	private void EnableButtons()
	{
		prevButton.gameObject.SetActive(imageIndex != 0);
		nextButton.gameObject.SetActive(imageIndex != images.Count - 1);
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}
}
