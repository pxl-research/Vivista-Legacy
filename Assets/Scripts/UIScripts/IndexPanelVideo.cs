using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.UI;

public class IndexPanelVideo : MonoBehaviour
{
	public GameObject error;

	public Text titleText;
	public Text authorText;
	public Text sizeText;
	public Text lengthText;
	public Text timestampText;
	public Text DownloadedText;
	public Image thumbnailImage;

	private UnityWebRequest imageDownload;
	private string uuid;

	private bool isLocal;

	public IEnumerator SetData(VideoSerialize video, bool local)
	{
		titleText.text = video.title;
		authorText.text = video.username;
		sizeText.text = MathHelper.FormatBytes(video.downloadsize);
		lengthText.text = MathHelper.FormatSeconds(video.length);
		timestampText.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		uuid = video.id;
		isLocal = local;

		if (video.title == "Corrupted file")
		{
			titleText.color = Color.red;
		}

		//TODO(Kristof): Prevent being able to open it without VR (will be fixed if we use raycasts from mouse instead of mouse events)
		if (!video.compatibleVersion)
		{
			gameObject.GetComponent<Hittable>().enabled = false;
			error.transform.parent.gameObject.SetActive(true);
			error.GetComponent<Text>().text = $"This project uses a version that's higher than the player's. Please update the player. [{uuid}]";
		}
		else
		{
			gameObject.GetComponent<Hittable>().enabled = true;
			error.transform.parent.gameObject.SetActive(false);
		}

		if (isLocal)
		{
			imageDownload = UnityWebRequestTexture.GetTexture("file:///" + Path.Combine(Application.persistentDataPath, Path.Combine(video.id, SaveFile.thumbFilename)));
		}
		else
		{
			imageDownload = UnityWebRequestTexture.GetTexture(Web.thumbnailUrl + "/" + uuid);
		}

		yield return imageDownload.SendWebRequest();

		if (imageDownload == null)
		{
			Assert.IsTrue(false, "This isn't supposed to happen. Investigate! " + imageDownload.url);
		}
		if (imageDownload.isNetworkError || imageDownload.isHttpError)
		{
			Debug.Log("Failed to download thumbnail " + imageDownload.url + ": " + imageDownload.error);
		}
		else if (imageDownload.isDone)
		{
			var texture = DownloadHandlerTexture.GetContent(imageDownload);
			thumbnailImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			thumbnailImage.color = Color.white;
		}
		else
		{
			Assert.IsTrue(false, "This isn't supposed to happen. Investigate! " + imageDownload.url);
		}

		imageDownload.Dispose();
		imageDownload = null;

		Refresh();
	}

	public void Refresh()
	{
		DownloadedText.enabled = Directory.Exists(Path.Combine(Application.persistentDataPath, uuid));
	}

	public void OnHit()
	{
		var indexPanel = Canvass.main.GetComponentInChildren<IndexPanel>();
		if (indexPanel != null)
		{
			indexPanel.answered = true;
			indexPanel.answerVideoId = uuid;
		}
	}

	public void OnHoverStart()
	{
		GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
	}

	public void OnHoverExit()
	{
		GetComponent<Image>().color = new Color(0, 0, 0, 0f);
	}
}
