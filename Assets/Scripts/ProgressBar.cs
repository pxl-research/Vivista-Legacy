using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour 
{
	public Text progressPercent;
	public RectTransform progressbarContainer;
	public RectTransform progressbar;
	public float progressbarWidth;

	public void Start()
	{
		progressbarWidth = progressbarContainer.rect.width;
		var offset = progressbar.offsetMax;
		offset.x = -progressbarWidth;
		progressbar.offsetMax = offset;
	}

	public void SetProgress(float progress)
	{
		progressbar.offsetMax = new Vector2(-(progressbarWidth - (progressbarWidth * progress)), progressbar.offsetMax.y);
		progressPercent.text = string.Format("{0:F1}%", progress * 100);
	}
}
