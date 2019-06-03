using UnityEngine;

public class InteractionPoints : MonoBehaviour
{
	void Start()
	{
		gameObject.transform.localScale = UnityEngine.XR.XRSettings.enabled ? new Vector3(2.5f,2.5f,2.5f) : new Vector3(5.5f, 5.5f, 5.5f);
	}
}
