using UnityEngine;

public class DownloadInProgressPanel : MonoBehaviour
{
	public delegate void AbortDownloadEvent();
	public AbortDownloadEvent OnAbortDownload;

	public delegate void ContinueEvent();
	public ContinueEvent OnContinue;

	public void AbortDownload()
	{
		OnAbortDownload();
	}

	public void Continue()
	{
		OnContinue();
	}
}
