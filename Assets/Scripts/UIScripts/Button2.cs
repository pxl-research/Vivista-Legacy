using System;
using UnityEngine.UI;

public enum SelectState
{
	Normal,
	Highlighted,
	Pressed,
	Disabled,
	Released,
	Selected
}

public class Button2 : Button 
{
	public SelectState state
	{ 
		get
		{
			switch (currentSelectionState)
			{
				case SelectionState.Normal:
					return SelectState.Normal;
				case SelectionState.Highlighted:
					return SelectState.Highlighted;
				case SelectionState.Pressed:
					return SelectState.Pressed;
				case SelectionState.Disabled:
					return SelectState.Disabled;
				case SelectionState.Selected:
					return SelectState.Selected;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
