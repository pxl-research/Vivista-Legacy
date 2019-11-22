using UnityEngine;

public class Canvass : MonoBehaviour
{ 
	public static Canvas main						=> _main					?? (_main = GameObject.Find("Canvas").GetComponent<Canvas>());
	private static Canvas _main;

	public static Canvas seekbar					=> _seekbar					?? (_seekbar = GameObject.Find("Seekbar Canvas").GetComponent<Canvas>());
	private static Canvas _seekbar;

	public static Canvas crosshair					=> _crosshair				?? (_crosshair = GameObject.Find("Crosshair Canvas").GetComponent<Canvas>());
	private static Canvas _crosshair;

	public static GameObject modalBackground		=> _modalBackground			?? (_modalBackground = main.transform.Find("ModalBackground").gameObject);
	private static GameObject _modalBackground;

	public static GameObject sphereUIWrapper		=> _sphereUIWrapper			?? (_sphereUIWrapper = GameObject.Find("SphereUI"));
	private static GameObject _sphereUIWrapper;
	
	public static GameObject sphereUICanvas			=>_sphereUICanvas			?? (_sphereUICanvas = GameObject.Find("SphereUICanvas"));
	private static GameObject _sphereUICanvas;

	public static GameObject sphereUIRenderer		=>_sphereUIRenderer			?? (_sphereUIRenderer = GameObject.Find("SphereUIRenderer"));
	private static GameObject _sphereUIRenderer;

	public static GameObject sphereUIPanelWrapper	=> _sphereUIPanelWrapper	?? (_sphereUIPanelWrapper = sphereUIWrapper.transform.Find("SphereUICanvas/PanelWrapper").gameObject);
	private static GameObject _sphereUIPanelWrapper;
}
