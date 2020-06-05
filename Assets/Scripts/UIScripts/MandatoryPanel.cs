using UnityEngine;
using UnityEngine.UI;

public class MandatoryPanel : MonoBehaviour
{
	public Toggle toggle;
	public Text warning;
	public bool isMandatory;

	public void Init(bool mandatory)
	{
		toggle.isOn = mandatory;
		isMandatory = mandatory;
		warning.gameObject.SetActive(mandatory);
	}

	public void OnToggle(bool value)
	{
		isMandatory = value;
		warning.gameObject.SetActive(value);
	}
}
