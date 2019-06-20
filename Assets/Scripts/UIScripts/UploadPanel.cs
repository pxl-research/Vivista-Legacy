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
		float timeRemaining = float.PositiveInfinity;
		float speed = 0.0f;

		if (status.request != null)
		{
			long totalUploaded = status.uploaded + (long)status.request.uploadedBytes;
			
			var newestTiming = new Timing {time = Time.realtimeSinceStartup, totalUploaded = totalUploaded};
			status.timings.Enqueue(newestTiming);
		
			while (status.timings.Count > 1 && status.timings.Peek().time < Time.realtimeSinceStartup - 1)
			{
				status.timings.Dequeue();
			}

			float currentSpeed = (newestTiming.totalUploaded - status.timings.Peek().totalUploaded) / (newestTiming.time - status.timings.Peek().time);
			speed = status.timings.Count >= 2 ? currentSpeed: float.NaN;
			timeRemaining = (status.totalSize - totalUploaded) / speed;
			progressBar.SetProgress(totalUploaded / (float)status.totalSize);

			//TODO(Simon): Show kB and GB when appropriate
			progressMB.text = $"{totalUploaded / megabyte:F2}/{status.totalSize / megabyte:F2}MB";

			time += Time.deltaTime;
		}

		if (time > timeBetweenUpdates)
		{
			if (!float.IsInfinity(timeRemaining) && !float.IsNaN(timeRemaining))
			{
				time %= timeBetweenUpdates;
				progressTime.text = $"{timeRemaining:F0} seconds remaining";
				progressSpeed.text = $"{speed / megabyte:F2}MB/s";
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