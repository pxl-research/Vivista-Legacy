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
		Physics.queriesHitBackfaces = true;
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
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			ray.origin = ray.GetPoint(10);
			ray.direction = -ray.direction;

			Debug.DrawLine(ray.origin, ray.GetPoint(10));

			foreach(var point in interactionPoints)
			{
				point.GetComponent<MeshRenderer>().material.color = Color.white;
			}

			if (Input.GetMouseButton(0))
			{
				if (Physics.Raycast(ray, out hit, 10))
				{
					var drawLocation = hit.point + ray.direction.normalized / 50;
					interactionPointTemp.transform.position = drawLocation;
					//Rotate to match sphere's normal
					interactionPointTemp.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
				}
			}

			if (!Input.GetMouseButton(0))
			{
				if (Physics.Raycast(ray, out hit, 10, 1 << LayerMask.NameToLayer("interactionPoints")))
				{
					hit.collider.GetComponentInParent<MeshRenderer>().material.color = Color.red;
				}
			}


			if (Input.GetMouseButtonUp(0))
			{
				var newPoint = Instantiate(interactionPointPrefab, interactionPointTemp.transform.position, interactionPointTemp.transform.rotation);
				interactionPoints.Add(newPoint);
				interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
			}
		}
	}
}
