using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class FileLoader : MonoBehaviour 
{
	public GameObject videoController;

	public GameObject video360;
	public GameObject video180;
	public GameObject video;
	public GameObject image360;
	public GameObject image180;
	public GameObject imageFlat;

	public GameObject camera360;
	public GameObject camera180;
	public GameObject cameraFlat;

	public GameObject playerInfoGUI;

	public FileType fileType = FileType.Video360;

	public enum FileType {
		Video360,
		Video180,
		Video,
		Image360,
		Image180,
		Image
	}

	public void LoadFile(string filename, FileType filetype)
	{
		videoController = null;

		switch (fileType)
		{
			case FileType.Image360:
			{
				Instantiate(camera360);
				break;
			}
			case FileType.Image180:
			{
				Instantiate(camera180);
				break;
			}
			case FileType.Image:
			{
				Instantiate(cameraFlat);
				break;
			}



			case FileType.Video360:
			{
				Instantiate(camera360);
				videoController = Instantiate(video360);
				break;
			}
			case FileType.Video180:
			{
				Instantiate(camera180);
				videoController = Instantiate(video180);
				break;
			}
			case FileType.Video:
			{
				Instantiate(cameraFlat);
				videoController = Instantiate(video);
				break;
			}



			default:
				throw new ArgumentOutOfRangeException();
		}

		if (fileType == FileType.Video || fileType == FileType.Video180 || fileType == FileType.Video360)
		{
			var player = videoController.GetComponent<VideoPlayer>();
			player.url = filename;

			player.time = 10;
			player.time = 0;
			player.Pause();
			player.errorReceived += VideoErrorHandler;

			var playerInfo = Instantiate(playerInfoGUI);
			var newParent = Canvass.main.transform.FindChild("LayoutSplitter");
			playerInfo.transform.SetParent(newParent, false);
			playerInfo.transform.SetAsFirstSibling();

			var seekbar = playerInfo.GetComponentInChildren<Seekbar>();
			var controller = videoController.GetComponent<VideoController>();
			seekbar.controller = controller;
			controller.seekbar = seekbar.transform.GetChild(0).GetComponent<RectTransform>();
			controller.timeText = seekbar.transform.parent.GetComponentInChildren<Text>();
		}
	}

	static void VideoErrorHandler(VideoPlayer player, string message)
	{
		Debug.Log(message);
	}
}
