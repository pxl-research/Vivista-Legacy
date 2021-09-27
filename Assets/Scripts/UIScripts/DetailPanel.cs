using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DetailPanel : MonoBehaviour
{
	public bool shouldClose;

	public Text videoLength;
	public Image thumb;
	public Text title;
	public Text description;
	public Text author;
	public Text timestamp;
	public Text downloadSize;
	public VideoSerialize video;
	public Button playButton;
	public Button playInVRButton;
	public Button downloadButton;
	public Button deleteButton;

	public bool answered;
	public string answerVideoId;
	public bool answerEnableVR;

	private UnityWebRequest imageDownload;
	private GameObject indexPanel;
	private float time;
	private const float refreshTime = 1.0f;

	void Update()
	{
		time += Time.deltaTime;
		if (time > refreshTime)
		{
			Refresh();
			time = 0;
		}
	}

	public IEnumerator Init(VideoSerialize videoToDownload, GameObject indexPanel, bool isLocal)
	{
		this.indexPanel = indexPanel;
		indexPanel.GetComponent<IndexPanel>().modalBackground.SetActive(true);

		video = videoToDownload;

		videoLength.text = MathHelper.FormatSeconds(video.length);
		title.text = video.title;
		description.text = video.description;
		author.text = video.username;
		timestamp.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		downloadSize.text = MathHelper.FormatBytes(video.downloadsize);

		if (video.title == "Corrupted file")
		{
			playButton.interactable = false;
			playInVRButton.interactable = false;
		}

		if (isLocal)
		{
			imageDownload = UnityWebRequest.Get("file:///" + Path.Combine(Application.persistentDataPath, video.id, SaveFile.thumbFilename));
		}
		else
		{
			imageDownload = UnityWebRequest.Get(Web.thumbnailUrl + "?id=" + video.id);
		}

		yield return imageDownload.SendWebRequest();

		if (imageDownload.isNetworkError)
		{
			Debug.Log("Failed to download thumbnail: " + imageDownload.error);
			imageDownload.Dispose();
			imageDownload = null;
		}
		else if (imageDownload.isDone || imageDownload.downloadProgress >= 1f)
		{
			var texture = new Texture2D(1, 1);
			texture.LoadImage(imageDownload.downloadHandler.data);
			thumb.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			imageDownload.Dispose();
			imageDownload = null;
			thumb.color = Color.white;
		}

		Refresh();
	}
	
	public void Refresh()
	{
		bool downloaded = Directory.Exists(Path.Combine(Application.persistentDataPath, video.id));
		downloadButton.gameObject.SetActive(!downloaded);
		playButton.gameObject.SetActive(downloaded);
		playInVRButton.gameObject.SetActive(downloaded);
		deleteButton.gameObject.SetActive(downloaded);
	}

	public void Back()
	{
		indexPanel.SetActive(true);
		indexPanel.GetComponent<IndexPanel>().modalBackground.SetActive(false);
		shouldClose = true;
	}

	public void Play()
	{
		answered = true;
		answerVideoId = video.id;
		answerEnableVR = false;
		indexPanel.SetActive(true);
		indexPanel.GetComponent<IndexPanel>().modalBackground.SetActive(false);
	}

	public void PlayInVR()
	{
		answered = true;
		answerVideoId = video.id;
		answerEnableVR = true;
		indexPanel.SetActive(true);
		indexPanel.GetComponent<IndexPanel>().modalBackground.SetActive(false);
	}

	public void Delete()
	{
		string path = Path.Combine(Application.persistentDataPath, video.id);
		bool downloaded = Directory.Exists(path);
		if (downloaded)
		{
			Directory.Delete(path, true);
		}
		Refresh();
	}

	public void Download()
	{
		VideoDownloadManager.Main.AddDownload(video);
	}
}
