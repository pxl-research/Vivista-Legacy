using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Editor : MonoBehaviour 
{
	public bool editorActive = true;
	public GameObject interactionPointPrefab;
	public GameObject interactionPointTemp;
	public List<GameObject> interactionPoints;

	void Start () 
	{
		interactionPointTemp = Instantiate(interactionPointPrefab, new Vector3(1000, 1000, 1000), Quaternion.identity);
	}
	
	void Update () 
	{
		if (Input.GetKeyDown(KeyCode.F1))
		{
			editorActive = !editorActive;
			Debug.Log(string.Format("Editor active: {0}", editorActive));
		}

		if (editorActive)
		{
			if (Input.GetMouseButton(0))
			{
				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				var hit = new RaycastHit();
				ray.origin = ray.GetPoint(100);
				ray.direction = -ray.direction;

				if (Physics.Raycast(ray, out hit))
				{
					var drawLocation = hit.point + ray.direction.normalized / 50;
					interactionPointTemp.transform.position = drawLocation;
					interactionPointTemp.transform.rotation = Camera.main.transform.rotation;
				}
			}
			if (Input.GetMouseButtonUp(0))
			{

			}
		}
	}
}
