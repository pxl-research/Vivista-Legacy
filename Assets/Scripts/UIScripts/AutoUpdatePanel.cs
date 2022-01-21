using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AutoUpdatePanel : MonoBehaviour
{
	private class VersionNumberResponse
	{
		public string version;
	}

	private UnityWebRequest downloadRequest;

	public Text title;
	public ProgressBar downloadProgress;
	public GameObject confirmUpdate;

	private static string newVersion;
	bool finished;
	string installerPath;

	public void Start()
	{
		title.text = "intializing...";
		StartCoroutine(StartUpdate());
	}

	public static IEnumerator IsUpdateAvailable(System.Action<bool> callback)
	{
		var versionFilePath = Path.Combine(Application.dataPath, "version.txt");
		var currentVersion = File.ReadAllText(versionFilePath);
		using (var request = UnityWebRequest.Get(Web.versionNumberUrl))
		{
			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				callback(false);
			}

			var response = JsonUtility.FromJson<VersionNumberResponse>(request.downloadHandler.text);
			newVersion = response.version;

			callback(response.version == currentVersion);
		}
	}

	public IEnumerator StartUpdate()
	{
		title.text = "Downloading...";
		using (var request = UnityWebRequest.Get(Web.latestEditorUrl))
		{
			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				yield return null;
			}

			var url = Web.baseUrlPublicFiles + request.downloadHandler.text.Replace("\"", "");

			using (downloadRequest = UnityWebRequest.Get(url))
			{
				installerPath = Path.Combine(Application.persistentDataPath, "installers", $"VivistaEditor-{newVersion}.exe");
				var downloadHandler = new DownloadHandlerFile(installerPath);
				downloadHandler.removeFileOnAbort = true;
				downloadRequest.downloadHandler = downloadHandler;

				yield return downloadRequest.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					yield return null;
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
		if (UnsavedChangesTracker.Instance.unsavedChanges)
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
				Verb = "runas" 
			}
		};

		proc.Start();

		UnsavedChangesTracker.Instance.ForceQuit();
	}

	public void Close()
	{
		Destroy(this);
	}
}
