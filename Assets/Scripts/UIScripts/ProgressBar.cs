using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour 
{
	public Text progressPercent;
	public RectTransform progressbarContainer;
	public RectTransform progressbar;
	private float progressbarWidth;

	public void Start()
	{
		progressbarWidth = progressbarContainer.rect.width;
		var offset = progressbar.offsetMax;
		offset.x = -progressbarWidth;
		progressbar.offsetMax = offset;
	}

	public void SetProgress(float progress)
	{
		if (float.IsNaN(progress))
		{
			progress = 0;
		}

		progressbar.offsetMax = new Vector2(-(progressbarWidth - (progressbarWidth * progress)), progressbar.offsetMax.y);
		progressPercent.text = $"{progress * 100:F1}%";
	}
}
