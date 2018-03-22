using UnityEngine;

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

		Transform newParent;
		if (UnityEngine.XR.XRSettings.enabled)
		{
			newParent = Canvass.seekbar.transform;
			playerInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 40f);
		}
		else
		{
			newParent = Canvass.main.transform.Find("LayoutSplitter");
		}
		playerInfo.transform.SetParent(newParent, false);
		playerInfo.transform.SetAsFirstSibling();

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
