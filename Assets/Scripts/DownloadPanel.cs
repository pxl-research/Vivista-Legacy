using UnityEngine;
using UnityEngine.UI;

public class DownloadPanel : MonoBehaviour 
{
	public ProgressBar Progressbar;
	public Text Title;
	public GameObject FailMessage;
	public bool ShouldRetry;
	public bool ShouldCancel;

	public void SetTitle(string newTitle)
	{
		Title.text = newTitle;
	}

	public void UpdatePanel(float progress)
	{
		Progressbar.SetProgress(progress);
	}

	public void Fail()
	{
		Progressbar.gameObject.SetActive(false);
		FailMessage.SetActive(true);
	}

	public void Retry()
	{
		ShouldRetry = true;
	}

	public void Cancel()
	{
		ShouldCancel = true;
	}

	public void Reset()
	{
		ShouldCancel = false;
		ShouldRetry = false;
		FailMessage.SetActive(false);
		Progressbar.gameObject.SetActive(true);
		Progressbar.SetProgress(0);
	}
}
