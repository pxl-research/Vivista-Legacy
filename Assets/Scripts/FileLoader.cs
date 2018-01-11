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

	public GameObject playerInfoGUI;

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

		playerInfo = Instantiate(playerInfoGUI);
		var newParent = Canvass.main.transform.Find("LayoutSplitter");
		playerInfo.transform.SetParent(newParent, false);
		playerInfo.transform.SetAsFirstSibling();

		var seekbar = playerInfo.GetComponentInChildren<Seekbar>();
		controller = videoController.GetComponent<VideoController>();
		seekbar.controller = controller;
		controller.seekbar = seekbar.transform.GetChild(0).GetComponent<RectTransform>();
		controller.timeText = seekbar.transform.parent.GetComponentInChildren<Text>();
	}

	public void LoadFile(string filename)
	{
		if (fileType == FileType.Video)
		{
			controller.PlayFile(filename);
		}
	}
}
