using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugMenu : MonoBehaviour
{
	public void SetServerToLocal()
	{
		Web.SetDebugUrl();
		Editor.Instance.InitLoginPanel();
	}

	public void SetServerToProduction()
	{
		Web.SetDefaultUrl();
		Editor.Instance.InitLoginPanel();
	}
}
