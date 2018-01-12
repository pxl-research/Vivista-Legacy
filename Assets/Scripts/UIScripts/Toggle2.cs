using System;
using UnityEngine;
using UnityEngine.UI;

public class Toggle2 : Toggle
{
	private bool WasOn;
	
	public bool switchedOn;
	public bool switchedOff;

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
				default:
					throw new ArgumentOutOfRangeException("SelectionState", "Something changed in the Toggle API");
			}
		}
	}

	public void Update()
	{
		switchedOn = isOn && !WasOn;
		switchedOff = !isOn && WasOn;
		WasOn = isOn;
	}

	public void SetState(bool isOn)
	{
		this.isOn = isOn;
	}
}
