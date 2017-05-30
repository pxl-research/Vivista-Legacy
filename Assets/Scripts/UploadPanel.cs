using System;
using UnityEngine;
using UnityEngine.UI;

public class UploadPanel : MonoBehaviour 
{
	public RectTransform progressbarContainer;
	public RectTransform progressbar;
	public Text progressPercent;
	public Text progressMB;
	public Text progressTime;
	public Text progressSpeed;
	public float progressbarWidth;

	public float time;

	const int kilobyte = 1024;
	const int megabyte = 1024 * 1024;
	const int gigabyte = 1024 * 1024 * 1024;

	public void Start()
	{
		progressbarWidth = progressbarContainer.rect.width;
	}

	public void UpdatePanel(UploadStatus status)
	{
		var totalUploaded = (status.currentPart * status.partSize)
							+ (status.currentRequest != null ? status.currentRequest.uploadProgress : 0.0f * status.currentPartSize);
		var totalPercentage = totalUploaded / status.totalSize;

		var newestTiming = new Timing {time = Time.realtimeSinceStartup, totalUploaded = totalUploaded};
		status.timings.Enqueue(newestTiming);

		while (status.timings.Peek().time < Time.realtimeSinceStartup - 1)
		{
			status.timings.Dequeue();
		}

		var speed = status.timings.Count >= 2 ? (newestTiming.totalUploaded - status.timings.Peek().totalUploaded) / (newestTiming.time - status.timings.Peek().time) : float.NaN;
		var timeRemaining = (status.totalSize - totalUploaded) / speed;

		progressbar.offsetMax = new Vector2(-(progressbarWidth - (progressbarWidth * totalPercentage)), progressbar.offsetMax.y);
		progressPercent.text = String.Format("{0:F1}%", totalPercentage * 100);
		progressMB.text = String.Format("{0:F2}/{1:F2}MB", totalUploaded / megabyte, status.totalSize / megabyte);

		time += Time.deltaTime;

		if (time > 1/4f)
		{
			time %= 1/4f;
			progressTime.text = String.Format("{0:F0} seconds remaining", timeRemaining);
			progressSpeed.text = String.Format("{0:F2}MB/s", speed / megabyte);
		}
	}
}
