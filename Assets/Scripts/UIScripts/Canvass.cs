using UnityEngine;

public class Canvass : MonoBehaviour
{ 
	public static Canvas main						=> _main					??= GameObject.Find("Canvas").GetComponent<Canvas>();
	private static Canvas _main;

	public static Canvas seekbarVR					=> _seekbarVR				??= Seekbar.instances.Find(x => x.isVRSeekbar).GetComponent<Canvas>();
	private static Canvas _seekbarVR;

	public static GameObject modalBackground		=> _modalBackground			??= main.transform.Find("ModalBackground").gameObject;
	private static GameObject _modalBackground;

	public static GameObject sphereUIWrapper		=> _sphereUIWrapper			??= GameObject.Find("SphereUI");
	private static GameObject _sphereUIWrapper;
	
	public static GameObject sphereUICanvas			=>_sphereUICanvas			??= GameObject.Find("SphereUICanvas");
	private static GameObject _sphereUICanvas;

	public static GameObject sphereUIRenderer		=>_sphereUIRenderer			??= GameObject.Find("SphereUIRenderer");
	private static GameObject _sphereUIRenderer;

	public static GameObject sphereUIPanelWrapper	=> _sphereUIPanelWrapper	??= sphereUIWrapper.transform.Find("SphereUICanvas/PanelWrapper").gameObject;
	private static GameObject _sphereUIPanelWrapper;
}
