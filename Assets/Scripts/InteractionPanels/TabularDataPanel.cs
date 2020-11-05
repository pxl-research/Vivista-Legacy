using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TabularDataPanel : MonoBehaviour
{
	public Text title;
	public List<string> tabularData;
	public RectTransform tabularDataWrapper;
	public RectTransform tabularDataCellPrefab;
	public RectTransform rowNumbersWrapper;
	public RectTransform rowNumberTextPrefab;
	public ScrollRect tabularDataScroller;

	private int currentColumns;
	private int currentRows;
	private const float GRID_CELL_SIZE_X = 448;
	private const float GRID_CELL_SIZE_Y = 220;
	private const float MIN_GRID_SIZE_X = 50;
	private const float MIN_GRID_SIZE_Y = 50;

	public void Init(string newTitle, int rows, int columns, List<string> newTabularData)
	{
		for (int i = tabularData.Count - 1; i >= 0; i--)
		{
			Destroy(tabularDataWrapper.GetChild(i).gameObject);
		}

		title.text = newTitle;
		tabularData = newTabularData;

		if (newTabularData != null && newTabularData.Count > 0)
		{
			currentRows = rows;
			currentColumns = columns;
		}

		for (int row = 0; row < currentRows; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				var dataCell = Instantiate(tabularDataCellPrefab, tabularDataWrapper);
				var cellText = dataCell.transform.GetComponentInChildren<InputField>();
				cellText.interactable = false;

				cellText.text = tabularData[row * currentColumns + column];

				dataCell.transform.SetAsLastSibling();
			}

			//NOTE(Jitse): Add row numbers
			var rowNumberText = Instantiate(rowNumberTextPrefab, rowNumbersWrapper);
			rowNumberText.GetComponent<Text>().text = $"{row + 1}";
		}
		float cellSizeX = Mathf.Max(GRID_CELL_SIZE_X / currentColumns, MIN_GRID_SIZE_X);
		float cellSizeY = Mathf.Max(GRID_CELL_SIZE_Y / currentRows, MIN_GRID_SIZE_Y);

		tabularDataWrapper.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellSizeX, cellSizeY);
		tabularDataScroller.verticalNormalizedPosition = 1;
	}

	public void OnEnable()
	{
		tabularDataScroller.verticalNormalizedPosition = 1;
	}
}
