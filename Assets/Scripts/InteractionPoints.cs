using UnityEngine;

public class InteractionPoints : MonoBehaviour
{
	void Start()
	{
		if (UnityEngine.XR.XRSettings.enabled)
		{
			gameObject.transform.localScale = new Vector3(2.5f,2.5f,1);
		} else
		{
			gameObject.transform.localScale = new Vector3(4, 4, 1);
		}
	}
}
