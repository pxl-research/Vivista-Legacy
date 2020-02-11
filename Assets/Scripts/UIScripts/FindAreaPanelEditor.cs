using System.Collections.Generic;
using UnityEngine;

public class FindAreaPanelEditor : MonoBehaviour
{
	public GameObject areaPickerPrefab;
	
	public GameObject resizePanel;
	private AreaPicker areaPicker;
	public RectTransform areaList;

	public bool answered;
	public List<Vector3> answerOrigins;
	public List<Vector3> answerDirections;

	public void Init()
	{

	}

	void Update()
	{
		if (areaPicker != null && areaPicker.answered)
		{
			answerOrigins = areaPicker.rayOrigins;
			answerDirections = areaPicker.rayDirections;

			areaPicker.Dispose();
			Destroy(areaPicker.gameObject);
			resizePanel.SetActive(true);
		}
	}

	public void OnAddArea()
	{
		var go = Instantiate(areaPickerPrefab, Canvass.main.transform);
		areaPicker = go.GetComponent<AreaPicker>();
		resizePanel.SetActive(false);
	}
}
