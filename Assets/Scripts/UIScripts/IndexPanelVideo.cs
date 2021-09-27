using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
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
	public Image thumbnailImage;
	public Button button;
	public RectTransform buttonHolder;

	private UnityWebRequest imageDownload;
	private VideoSerialize video;
	private IndexPanel indexPanel;

	private bool isLocal;

	public IEnumerator SetData(VideoSerialize video, bool local, IndexPanel indexPanel)
	{
		titleText.text = video.title;
		authorText.text = video.username;
		sizeText.text = MathHelper.FormatBytes(video.downloadsize);
		lengthText.text = MathHelper.FormatSeconds(video.length);
		timestampText.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		isLocal = local;
		this.video = video;
		this.indexPanel = indexPanel;

		if (video.title == "Corrupted file")
		{
			titleText.color = Color.red;
		}

		if (!video.compatibleVersion)
		{
			error.transform.parent.gameObject.SetActive(true);
			error.GetComponent<Text>().text = $"This project uses a version that's higher than the player's. Please update the player. [{video.id}]";
		}
		else
		{
			error.transform.parent.gameObject.SetActive(false);
		}

		if (isLocal)
		{
			imageDownload = UnityWebRequestTexture.GetTexture("file:///" + Path.Combine(Application.persistentDataPath, Path.Combine(video.id, SaveFile.thumbFilename)));
		}
		else
		{
			imageDownload = UnityWebRequestTexture.GetTexture(Web.thumbnailUrl + "?id=" + video.id);
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
	}

	public void OnPointerEnter()
	{
		buttonHolder.gameObject.SetActive(true);
	}

	public void OnPointerExit()
	{
		buttonHolder.gameObject.SetActive(false);
	}

	public void ShowVideoDetails()
	{
		indexPanel.ShowVideoDetails(video);
	}

	public void PlayVideo()
	{
		indexPanel.PlayVideo(video);
	}

	public void PlayVideoInVr()
	{
		indexPanel.PlayVideoVR(video);
	}
}
