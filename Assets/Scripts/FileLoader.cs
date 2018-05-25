using UnityEngine;
using UnityEngine.UI;

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

	public GameObject currentCamera;
	public GameObject videoController;
	public GameObject playerInfo;

	private VideoController controller;

	public FileType fileType = FileType.Video;
	public Perspective currentPerspective;

	public enum FileType {
		Video,
		Image
	}

	public void Start()
	{
		videoController = Instantiate(videoMesh);

		if (UnityEngine.XR.XRSettings.enabled)
		{
			//TODO(Kristof): This should all probably be done somewhere else
			playerInfo.GetComponent<RectTransform>().SetParent(Canvass.seekbar.transform, false);
			var t = playerInfo.GetComponent<RectTransform>();
			t.anchorMin = new Vector2(0, 0);
			t.anchorMax = new Vector2(1, 1);
			t.anchoredPosition = Vector2.zero;
			t.offsetMin = Vector2.zero;
			t.offsetMax = Vector2.zero;

			//NOTE(Kristof): These text changes don't look as good without VR
			var time = t.GetComponentInChildren<Text>().gameObject;
			time.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
			time.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
			time.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 100f);
			time.transform.localPosition = Vector3.zero;
			time.transform.localScale = new Vector3(0.2f, 0.2f, 1);
			time.GetComponent<Text>().fontSize = 75;
		}

		var seekbar = playerInfo.GetComponentInChildren<Seekbar>();
		controller = videoController.GetComponent<VideoController>();
		seekbar.controller = controller;
	}

	public void LoadFile(string filename)
	{
		if (fileType == FileType.Video)
		{
			controller.PlayFile(filename);
		}
	}
}
