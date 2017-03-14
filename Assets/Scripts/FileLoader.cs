using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class FileLoader : MonoBehaviour 
{
	public GameObject Video360;
	public GameObject Video180;
	public GameObject Video;
	public GameObject Image360;
	public GameObject Image180;
	public GameObject Image;

	public GameObject Camera360;
	public GameObject Camera180;
	public GameObject Camera;

	public GameObject PlayerInfoGUI;

	public enum FileType {
		Video360,
		Video180,
		Video,
		Image360,
		Image180,
		Image
	}

	void Start () 
	{
		if (!Video360)	{ Debug.LogError(string.Format("Hey you forgot to hook up Video360 to the FileLoader script at {0}",	name)); }
		if (!Video180)	{ Debug.LogError(string.Format("Hey you forgot to hook up Video180 to the FileLoader script at {0}",	name)); }
		if (!Video)		{ Debug.LogError(string.Format("Hey you forgot to hook up Video to the FileLoader script at {0}",		name)); }
		if (!Image360)	{ Debug.LogError(string.Format("Hey you forgot to hook up Image360 to the FileLoader script at {0}",	name)); }
		if (!Image180)	{ Debug.LogError(string.Format("Hey you forgot to hook up Image180 to the FileLoader script at {0}",	name)); }
		if (!Image)		{ Debug.LogError(string.Format("Hey you forgot to hook up Image to the FileLoader script at {0}",		name)); }
		if (!Camera)	{ Debug.LogError(string.Format("Hey you forgot to hook up a Camera to the FileLoader script at {0}",	name)); }

		var fileType = FileType.Video;
		var fileName = @"C:\Users\20003613\Documents\Git\360video\Assets\Video\video2.mp4";

		GameObject newCamera = null;
		GameObject videoPlayer = null;

		switch (fileType)
		{
			case FileType.Image360:
			{
				Instantiate(Camera360);
				break;
			}
			case FileType.Image180:
			{
				Instantiate(Camera180);
				break;
			}
			case FileType.Image:
			{
				Instantiate(Camera);
				break;
			}



			case FileType.Video360:
			{
				Instantiate(Camera360);
				videoPlayer = Instantiate(Video360);
				break;
			}
			case FileType.Video180:
			{
				Instantiate(Camera180);
				videoPlayer = Instantiate(Video180);
				break;
			}
			case FileType.Video:
			{
				Instantiate(Camera);
				videoPlayer = Instantiate(Video);
				break;
			}



			default:
				throw new ArgumentOutOfRangeException();
		}

		if (fileType == FileType.Video || fileType == FileType.Video180 || fileType == FileType.Video360)
		{
			var player = videoPlayer.GetComponent<VideoPlayer>();
			player.url = fileName;
			player.waitForFirstFrame = true;
			player.Play();

			var canvas = FindObjectOfType<Canvas>();
			var playerInfo = Instantiate(PlayerInfoGUI);
			playerInfo.transform.SetParent(canvas.transform, false);

			var seekbar = playerInfo.GetComponentInChildren<Seekbar>();
			var controller = videoPlayer.GetComponent<VideoController>();
			seekbar.controller = controller;
			controller.seekbar = seekbar.transform.GetChild(0).GetComponent<RectTransform>();
			controller.timeText = seekbar.transform.parent.GetComponentInChildren<Text>();
		}
	}
	
	void Update () 
	{
		
	}
}
