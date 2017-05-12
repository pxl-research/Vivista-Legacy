using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class FileLoader : MonoBehaviour 
{
	public GameObject videoMesh;
	public GameObject image360;
	public GameObject image180;
	public GameObject imageFlat;

	public Mesh Mesh360;
	public Mesh Mesh180;
	public Mesh MeshFlat;

	public Material Material360;
	public Material Material180;
	public Material MaterialFlat;

	public GameObject camera360;
	public GameObject camera180;
	public GameObject cameraFlat;

	public GameObject currentCamera;
	public GameObject videoController;
	public GameObject playerInfo;

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

	public void Start()
	{
		videoController = Instantiate(videoMesh);
		currentCamera = Instantiate(cameraFlat);

		playerInfo = Instantiate(playerInfoGUI);
		var newParent = Canvass.main.transform.FindChild("LayoutSplitter");
		playerInfo.transform.SetParent(newParent, false);
		playerInfo.transform.SetAsFirstSibling();

		var seekbar = playerInfo.GetComponentInChildren<Seekbar>();
		var controller = videoController.GetComponent<VideoController>();
		seekbar.controller = controller;
		controller.seekbar = seekbar.transform.GetChild(0).GetComponent<RectTransform>();
		controller.timeText = seekbar.transform.parent.GetComponentInChildren<Text>();
	}

	public void SetPerspective(Perspective perspective)
	{
		Destroy(currentCamera);

		switch(perspective)
		{
			case Perspective.Perspective360:
			{
				currentCamera = Instantiate(camera360);
				videoController.GetComponent<MeshFilter>().sharedMesh = Mesh360;

				Destroy(videoController.GetComponent<BoxCollider>());
				videoController.AddComponent<SphereCollider>();

				videoController.GetComponent<MeshRenderer>().material = Material360;
				videoController.transform.localScale = Vector3.one;
				videoController.transform.position = Vector3.zero;
				break;
			}
			case Perspective.Perspective180:
			{
				currentCamera = Instantiate(camera180);
				videoController.GetComponent<MeshFilter>().sharedMesh = Mesh180;

				Destroy(videoController.GetComponent<BoxCollider>());
				videoController.AddComponent<SphereCollider>();

				videoController.GetComponent<MeshRenderer>().material = Material180;
				videoController.transform.localScale = Vector3.one;
				videoController.transform.position = Vector3.zero;
				break;
			}
			case Perspective.PerspectiveFlat:
			{
				currentCamera = Instantiate(cameraFlat);
				videoController.GetComponent<MeshFilter>().sharedMesh = MeshFlat;

				Destroy(videoController.GetComponent<BoxCollider>());
				videoController.AddComponent<BoxCollider>();

				videoController.GetComponent<MeshRenderer>().material = MaterialFlat;
				videoController.transform.localScale = new Vector3(1.536f, 0.864f, 1);
				videoController.transform.position = new Vector3(0, 0, 1);
				break;
			}
		}
	}

	public void LoadFile(string filename)
	{
		var seekbar = playerInfo.GetComponentInChildren<Seekbar>();
		var controller = videoController.GetComponent<VideoController>();
		seekbar.controller = controller;
		controller.seekbar = seekbar.transform.GetChild(0).GetComponent<RectTransform>();
		controller.timeText = seekbar.transform.parent.GetComponentInChildren<Text>();

		if (fileType == FileType.Video || fileType == FileType.Video180 || fileType == FileType.Video360)
		{
			var player = videoController.GetComponent<VideoPlayer>();
			player.url = filename;

			player.time = 10;
			player.time = 0;
			player.Pause();
			player.errorReceived += VideoErrorHandler;
		}
	}

	static void VideoErrorHandler(VideoPlayer player, string message)
	{
		Debug.Log(message);
	}
}
