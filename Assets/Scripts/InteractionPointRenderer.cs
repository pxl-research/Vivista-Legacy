using UnityEngine;

public class InteractionPointRenderer : MonoBehaviour
{
	void Start()
	{
		gameObject.transform.localScale = UnityEngine.XR.XRSettings.enabled ? new Vector3(2.5f,2.5f,2.5f) : new Vector3(10f, 10f, 10f);
	}
}
