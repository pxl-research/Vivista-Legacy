using UnityEngine;

public class InteractionPoints : MonoBehaviour
{
	void Start()
	{
		if (UnityEngine.XR.XRSettings.enabled)
		{
			gameObject.transform.localScale = new Vector3(2,2,1);
		} else
		{
			gameObject.transform.localScale = new Vector3(3, 3, 1);
		}
	}
}
