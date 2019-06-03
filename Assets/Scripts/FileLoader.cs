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
		seekbar.controller = controller;
	}

	public void LoadFile(string filename)
	{
		controller.PlayFile(filename);
	}
}
