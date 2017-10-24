using System;
using UnityEngine;
using UnityEngine.UI;

public class UploadPanel : MonoBehaviour 
{
	public Text progressMB;
	public Text progressTime;
	public Text progressSpeed;
	public ProgressBar progressBar;

	public float time;

	const int kilobyte = 1024;
	const int megabyte = 1024 * 1024;
	const int gigabyte = 1024 * 1024 * 1024;
	private const float timeBetweenUpdates = 1/10f;

	public void UpdatePanel(UploadStatus status)
	{
		time += Time.deltaTime;
		var timeRemaining = float.PositiveInfinity;
		var speed = 0.0f;

		if (status.request != null)
		{
			var totalUploaded = status.request.uploadProgress * status.totalSize;

			var newestTiming = new Timing {time = Time.realtimeSinceStartup, totalUploaded = totalUploaded};
			status.timings.Enqueue(newestTiming);
		
			while (status.timings.Count > 1 && status.timings.Peek().time < Time.realtimeSinceStartup - 1)
			{
				status.timings.Dequeue();
			}

			speed = status.timings.Count >= 2 ? (newestTiming.totalUploaded - status.timings.Peek().totalUploaded) / (newestTiming.time - status.timings.Peek().time) : float.NaN;
			timeRemaining = (status.totalSize - totalUploaded) / speed;
			progressBar.SetProgress(status.request.progress);

			//TODO(Simon): Show kB and GB when appropriate
			progressMB.text = String.Format("{0:F2}/{1:F2}MB", totalUploaded / megabyte, status.totalSize / megabyte);

			time += Time.deltaTime;
		}

		if (time > timeBetweenUpdates)
		{
			if (!float.IsInfinity(timeRemaining) && !float.IsNaN(timeRemaining))
			{
				time %= timeBetweenUpdates;
				progressTime.text = String.Format("{0:F0} seconds remaining", timeRemaining);
				progressSpeed.text = String.Format("{0:F2}MB/s", speed / megabyte);
			}
			else
			{
				time %= timeBetweenUpdates;
				progressTime.text = "Connecting...";
				progressSpeed.text = "Connecting...";
			}
		}
	}
}
