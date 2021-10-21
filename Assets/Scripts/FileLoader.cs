using UnityEngine;
using UnityEngine.UI;

public class FileLoader : MonoBehaviour 
{
	public GameObject videoMesh;
	public GameObject playerInfo;

	public VideoController controller;

	public void Start()
	{
		controller = Instantiate(videoMesh).GetComponent<VideoController>();
		Seekbar.instance.videoController = controller;
		Seekbar.instanceVR.videoController = controller;
	}

	public void LoadFile(string filename)
	{
		controller.PlayFile(filename);
	}
}
