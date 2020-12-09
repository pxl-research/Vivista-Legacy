using UnityEngine;
using UnityEngine.UI;

public class TabularDataPanelSphere : MonoBehaviour
{
	public Text title;
	public Text pageNumber;
	public string[] tabularData;
	public RectTransform tabularDataWrapper;
	public RectTransform tabularDataCellPrefab;
	public RectTransform rowNumbers;
	public Button backButton;
	public Button nextButton;

	private int currentColumns;
	private int currentRows;
	private int currentPage;
	private int maxPages;

	private const int MAXROWSPAGE = 7;
	private const float MIN_GRID_SIZE_X = 50;
	private const float MIN_GRID_SIZE_Y = 52.5f;

	public void Init(string newTitle, int rows, int columns, string[] newTabularData)
	{
		backButton.onClick.AddListener(BackButtonClick);
		nextButton.onClick.AddListener(NextButtonClick);

		ClearTable();

		title.text = newTitle;
		pageNumber.text = $"{ currentPage + 1 }";
		tabularData = newTabularData;

		if (newTabularData != null && newTabularData.Length > 0)
		{
			currentRows = rows;
			currentColumns = columns;
			maxPages = Mathf.CeilToInt((float)currentRows / MAXROWSPAGE);
		}
		else
		{
			Toasts.AddToast(5, "File is corrupt");
			return;
		}

		SetButtonStates();
		PopulateTable();
		SetRowNumbers();

		tabularDataWrapper.GetComponent<GridLayoutGroup>().constraintCount = currentColumns;
		float cellSizeX = Mathf.Max(tabularDataWrapper.rect.width / currentColumns, MIN_GRID_SIZE_X);
		float cellSizeY = Mathf.Max(tabularDataWrapper.rect.height / currentRows, MIN_GRID_SIZE_Y);

		tabularDataWrapper.GetComponent<GridLayoutGroup>().cellSize = new Vector2(cellSizeX, cellSizeY);
	}
	
	private void PopulateTable()
	{
		for (int row = 0; row < currentRows; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				var dataCell = Instantiate(tabularDataCellPrefab, tabularDataWrapper);
				if (row >= MAXROWSPAGE)
				{
					dataCell.gameObject.SetActive(false);
				}

				var cellText = dataCell.transform.GetComponentInChildren<InputField>();
				cellText.interactable = false;

				cellText.text = tabularData[row * currentColumns + column];
				cellText.textComponent.fontSize = 16;
				cellText.textComponent.color = Color.black;

				dataCell.transform.SetAsLastSibling();
			}
		}
	}

	private void ClearTable()
	{
		int rowLimit = (currentPage + 1) * MAXROWSPAGE;
		if (rowLimit > currentRows)
		{
			rowLimit = currentRows;
		}

		for (int row = currentPage * MAXROWSPAGE; row < rowLimit; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				tabularDataWrapper.GetChild(row * currentColumns + column).gameObject.SetActive(false);
			}
		}
	}

	private void NextButtonClick()
	{
		ClearTable();

		currentPage++;

		SetButtonStates();
		ActivateTableChildren();
		SetRowNumbers();
	}

	private void BackButtonClick()
	{
		ClearTable();

		currentPage--;

		SetButtonStates();
		ActivateTableChildren();
		SetRowNumbers();
	}

	private void ActivateTableChildren()
	{
		pageNumber.text = $"{ currentPage + 1 }";

		int rowLimit = (currentPage + 1) * MAXROWSPAGE;
		if (rowLimit > currentRows)
		{
			rowLimit = currentRows;
		}
		for (int row = currentPage * MAXROWSPAGE; row < rowLimit; row++)
		{
			for (int column = 0; column < currentColumns; column++)
			{
				tabularDataWrapper.GetChild(row * currentColumns + column).gameObject.SetActive(true);
			}
		}
	}

	private void SetButtonStates()
	{
		backButton.interactable = currentPage > 0;
		nextButton.interactable = currentPage < maxPages - 1;
	}

	private void SetRowNumbers()
	{
		//NOTE(Jitse): Calculate how many rows are in current page.
		int rowLimit = (currentPage + 1) * MAXROWSPAGE;
		if (rowLimit > currentRows)
		{
			rowLimit = currentRows;
		}

		int rowsInPage = rowLimit - currentPage * MAXROWSPAGE;

		for (int i = 0; i < MAXROWSPAGE; i++)
		{
			var rowNumberText = rowNumbers.GetChild(i).GetComponent<Text>();
			if (i < rowsInPage)
			{
				int rowNumber = currentPage * MAXROWSPAGE + i + 1;
				rowNumberText.text = $"{rowNumber}";
				rowNumberText.gameObject.SetActive(true);
			}
			else
			{
				rowNumberText.text = "";
				rowNumberText.gameObject.SetActive(false);
			}
		}
	}
}
