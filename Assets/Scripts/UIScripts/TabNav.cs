using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//NOTE(Simon): Add this script somwhere in your window. Add all eligible UI elements to "inputs" (only elements inheriting from Selectable allowed). Script will cycle through them in the same order. Shift-tabbing works as well.
//NOTE(Simon): Also handles cleanup of tab characters in multiline inputs.
public class TabNav : MonoBehaviour
{
	public List<Selectable> inputs;

	void Start()
	{
		Debug.Assert(inputs != null && inputs.Count > 0, $"The TabNav of {gameObject.name} is not filled.");
		if (inputs != null)
		{
			inputs[0].Select();

			foreach (var input in inputs)
			{
				var field = input as InputField;
				if (field != null && field.lineType != InputField.LineType.SingleLine)
				{
					field.onValidateInput += RemoveTabs;
					
				}
			}
		}
	}

	private char RemoveTabs(string text, int charindex, char addedchar)
	{
		return addedchar == '\t' ? '\0' : addedchar;
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab) && inputs.Count > 1)
		{
			var currentGo = EventSystem.current.currentSelectedGameObject;
			if (currentGo != null)
			{
				var selectable = currentGo.GetComponent<Selectable>();
				int index = inputs.IndexOf(selectable);
				if (index > -1)
				{
					bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
					index = (index + inputs.Count + (shift ? -1 : 1)) % inputs.Count;
					inputs[index].Select();
				}
			}
		}
	}
}
