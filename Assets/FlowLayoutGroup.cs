//https://stackoverflow.com/questions/38336835/correct-flowlayoutgroup-in-unity3d-as-per-horizontallayoutgroup-etc
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Flow Layout Group", 153)]
public class FlowLayoutGroup : LayoutGroup
{
	private int numCellsX, numCellsY;
	private float totalWidth;
	private float totalHeight;

	private Vector2 _cellSize = new Vector2(100, 100);
	public Vector2 cellSize
	{
		get => rectChildren.Count > 0 ? rectChildren[0].sizeDelta : Vector2.zero;
		set => SetProperty(ref _cellSize, value);
	}

	[SerializeField] private Vector2 _spacing = Vector2.zero;
	public Vector2 spacing
	{
		get => _spacing;
		set
		{
			_spacing.x = Mathf.Max(spacing.x, Mathf.Epsilon);
			_spacing.y = Mathf.Min(spacing.y, Mathf.Epsilon);
			SetProperty(ref _spacing, value);
		}
	}

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		base.OnValidate();
	}
#endif

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();

		float minSpace = padding.horizontal + (cellSize.x + spacing.x) - spacing.x;
		SetLayoutInputForAxis(minSpace, minSpace, -1, 0);
	}

	public override void CalculateLayoutInputVertical()
	{
		float minSpace = padding.vertical + (cellSize.y + spacing.y) - spacing.y;
		SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
	}

	public override void SetLayoutHorizontal()
	{
		SetCellsAlongAxis();
	}

	public override void SetLayoutVertical()
	{
		SetCellsAlongAxis();
	}

	private void SetCellsAlongAxis()
	{
		// Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
		// and only vertical values when invoked for the vertical axis.
		// However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
		// Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
		// and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.

		float width = rectTransform.rect.size.x;
		float height = rectTransform.rect.size.y;

		int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x) / (cellSize.x + spacing.x)));
		int cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y) / (cellSize.y + spacing.y)));

		numCellsX = Mathf.Clamp(cellCountX, 1, rectChildren.Count);
		numCellsY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildren.Count / (float)cellCountX));

		var requiredSpace = new Vector2(numCellsX * cellSize.x + (numCellsX - 1) * spacing.x, 
										numCellsY * cellSize.y + (numCellsY - 1) * spacing.y);
		var startOffset = new Vector2(GetStartOffset(0, requiredSpace.x), GetStartOffset(1, requiredSpace.y));
		var flexSpaceLeft = width - requiredSpace.x;
		var flexSpacingPerchild = flexSpaceLeft / Mathf.Max(1, numCellsX - 1);

		totalWidth = 0;
		totalHeight = 0;
		for (int i = 0; i < rectChildren.Count; i++)
		{
			SetChildAlongAxis(rectChildren[i], 0, startOffset.x + totalWidth, rectChildren[i].rect.size.x);
			SetChildAlongAxis(rectChildren[i], 1, startOffset.y + totalHeight, rectChildren[i].rect.size.y);

			totalWidth += rectChildren[i].rect.width + spacing.x + flexSpacingPerchild;

			if (i < rectChildren.Count - 1 && totalWidth + rectChildren[i + 1].rect.width > width)
			{
				totalWidth = 0;
				totalHeight += rectChildren[i].rect.height + spacing.y;
			}
		}
	}
}