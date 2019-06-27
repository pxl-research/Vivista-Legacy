using UnityEngine;

public class Canvass : MonoBehaviour
{ 
	public static Canvas main					=> _canvas				?? (_canvas = GameObject.Find("Canvas").GetComponent<Canvas>());
	private static Canvas _canvas;

	public static Canvas seekbar				=> _seekBarCanvas		?? (_seekBarCanvas = GameObject.Find("Seekbar Canvas").GetComponent<Canvas>());
	private static Canvas _seekBarCanvas;

	public static Canvas crosshair				=> _crosshairCanvas		?? (_crosshairCanvas = GameObject.Find("Crosshair Canvas").GetComponent<Canvas>());
	private static Canvas _crosshairCanvas;

	public static GameObject modalBackground	=> _modalBackground		?? (_modalBackground = main.transform.Find("ModalBackground").gameObject);
	private static GameObject _modalBackground;

	public static GameObject sphere				=> _sphere				?? (_sphere = GameObject.Find("SphereUI").gameObject);
	private static GameObject _sphere;
}
