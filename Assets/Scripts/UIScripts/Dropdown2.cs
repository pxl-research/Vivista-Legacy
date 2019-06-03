using UnityEngine.UI;

public class Dropdown2 : Dropdown
{
	public bool isOpen()
	{
		//If the dropdown has 4 children (as opposed to 3), it is open
		return transform.childCount == 4;
	}
}
