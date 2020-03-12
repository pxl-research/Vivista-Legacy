using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleGroup2 : ToggleGroup
{
	public class ToggleGroupChangedEvent : UnityEvent<Toggle> { }

    private ToggleGroupChangedEvent onChange = new ToggleGroupChangedEvent();
	public ToggleGroupChangedEvent onToggleGroupChanged
	{
		get => onChange;
		set { onChange = value; RefreshToggleEvents(); }
	}

	//NOTE(Simon): Yeah it's reflection on a private field. But it's bad design by Unity
	public List<Toggle> GetAllToggles()
	{
		var field = typeof(ToggleGroup).GetField("m_Toggles", BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.IsTrue(field != null);
		return (List<Toggle>)field.GetValue(this);
	}

	public new void RegisterToggle(Toggle toggle)
	{
		base.RegisterToggle(toggle);
		RefreshToggleEvents();
	}

	private void RefreshToggleEvents()
	{
		var toggles = GetAllToggles();
		for (int i = 0; i < toggles.Count; i++)
		{
			toggles[i].onValueChanged.RemoveAllListeners();
			toggles[i].onValueChanged.AddListener(OnToggle);
		}
	}

	private void OnToggle(bool isSelected)
	{
		if (isSelected)
		{
			onChange?.Invoke(FirstActiveToggle());
		}
	}

	private Toggle FirstActiveToggle()
	{
		foreach (var t in ActiveToggles())
		{
			return t;
		}
		return null;
	}
}
