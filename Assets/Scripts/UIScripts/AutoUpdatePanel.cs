using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AutoUpdatePanel : MonoBehaviour
{
	public enum VivistaApplication
	{
		Player,
		Editor
	}

	private class VersionNumberResponse
	{
		public string version;
	}

	private UnityWebRequest downloadRequest;

	public Text title;
	public ProgressBar downloadProgress;
	public GameObject confirmUpdate;
	public Button confirmUpdateButton;

	private static string newVersion;
	private bool finished;
	private string installerPath;
	private static string installerFolder;
	private VivistaApplication application;

	private Action onCancelCallback;

	public void Start()
	{
		title.text = "intializing...";
		StartCoroutine(StartUpdate());
	}

	public void Init(VivistaApplication application, Action onCancelCallback)
	{
		this.application = application;
		this.onCancelCallback = onCancelCallback;

		switch(application)
		{
			case VivistaApplication.Player:
				confirmUpdateButton.GetComponentInChildren<Text>().text = "Install update";
				break;
			case VivistaApplication.Editor:
				confirmUpdateButton.GetComponentInChildren<Text>().text = "Save project and install update";
				break;
		}
	}

	public static IEnumerator IsUpdateAvailable(Action<bool> callback)
	{
		installerFolder = Path.Combine(Application.persistentDataPath, "installers");

		Directory.CreateDirectory(installerFolder);

		//NOTE(Simon): Delete old installers
		foreach (string file in Directory.GetFiles(installerFolder))
		{
			File.Delete(file);
		}

		var versionFilePath = Path.Combine(Application.streamingAssetsPath, "version.txt");
		var currentVersion = File.ReadAllText(versionFilePath);
		using (var request = UnityWebRequest.Get(Web.versionNumberUrl))
		{
			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				callback(false);
				yield break;
			}

			var response = JsonUtility.FromJson<VersionNumberResponse>(request.downloadHandler.text);
			newVersion = response.version;

			callback(response.version != currentVersion);
		}

	}

	public IEnumerator StartUpdate()
	{
		title.text = "Downloading...";

		UnityWebRequest UrlRequest;
		switch (application)
		{
			case VivistaApplication.Editor:
				UrlRequest = UnityWebRequest.Get(Web.latestEditorUrl);
				break;
			case VivistaApplication.Player:
				UrlRequest = UnityWebRequest.Get(Web.latestPlayerUrl);
				break;
			default:
				yield break;
		}

		using (UrlRequest)
		{
			yield return UrlRequest.SendWebRequest();

			if (UrlRequest.result != UnityWebRequest.Result.Success)
			{
				yield break;
			}

			var url = Web.wwwrootUrl + UrlRequest.downloadHandler.text.Replace("\"", "");

			using (downloadRequest = UnityWebRequest.Get(url))
			{
				switch (application)
				{
					case VivistaApplication.Editor:
						installerPath = Path.Combine(installerFolder, $"VivistaEditor-{newVersion}.exe");
						break;
					case VivistaApplication.Player:
						installerPath = Path.Combine(installerFolder, $"VivistaPlayer-{newVersion}.exe");
						break;
					default:
						yield break;
				}

				var downloadHandler = new DownloadHandlerFile(installerPath);
				downloadHandler.removeFileOnAbort = true;
				downloadRequest.downloadHandler = downloadHandler;

				yield return downloadRequest.SendWebRequest();

				if (downloadRequest.result != UnityWebRequest.Result.Success)
				{
					yield break;
				}
			}

			downloadRequest = null;
			finished = true;
		}
	}

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape) && downloadRequest == null)
		{
			Close();
		}

		if (downloadRequest != null)
		{
			downloadProgress.SetProgress(downloadRequest.downloadProgress);
		}

		if (finished)
		{
			downloadProgress.SetProgress(1f);
			confirmUpdate.SetActive(true);
		}
	}

	public void OnFinishUpdate()
	{
		if (application == VivistaApplication.Editor && UnsavedChangesTracker.Instance.unsavedChanges)
		{
			if (!Editor.Instance.SaveProject())
			{
				return;
			}
		}

		var proc = new Process 
		{ 
			StartInfo = 
			{ 
				FileName = installerPath, 
				Verb = "runas",
				Arguments = "/SP- /VERYSILENT"
			}
		};

		proc.Start();

		if (application == VivistaApplication.Editor)
		{
			UnsavedChangesTracker.Instance.ForceQuit();
		}
		else
		{
			Application.Quit();
		}
	}

	public void Close()
	{
		onCancelCallback();
		Destroy(gameObject);
	}
}
