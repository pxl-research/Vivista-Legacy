using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class ToggleGroup2 : ToggleGroup
{
	//NOTE(Simon): Yeah it's reflection on a private field. But it's bad design by Unity
	public List<Toggle> GetAllToggles()
	{
		var field = typeof(ToggleGroup).GetField("m_Toggles", BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.IsTrue(field != null);
		return (List<Toggle>)field.GetValue(this);
	}
}
