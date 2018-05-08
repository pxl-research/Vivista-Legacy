using UnityEngine;

public class InteractionPoints : MonoBehaviour
{
	void Start()
	{
		if (UnityEngine.XR.XRSettings.enabled)
		{
			gameObject.transform.localScale = new Vector3(2.5f,2.5f,2.5f);
		} else
		{
			gameObject.transform.localScale = new Vector3(5.5f, 5.5f, 5.5f);
		}
	}
}
