using UnityEngine;

public class FileLoader : MonoBehaviour 
{
	public GameObject videoMesh;
	public GameObject playerInfo;

	public VideoController controller;

	public void Start()
	{
		controller = Instantiate(videoMesh).GetComponent<VideoController>();
		foreach (var instance in Seekbar.instances)
		{
			instance.videoController = controller;
		}
	}

	public void LoadFile(string filename)
	{
		controller.PlayFile(filename);
	}
}
