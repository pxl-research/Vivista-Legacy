using UnityEngine;

public class UnsavedChangesPanel : MonoBehaviour
{
	public delegate void SaveEvent();
	public SaveEvent OnSave;

	public delegate void DiscardEvent();
	public SaveEvent OnDiscard;

	public delegate void CancelEvent();
	public SaveEvent OnCancel;

	public void Save()
	{
		OnSave();
	}

	public void Discard()
	{
		OnDiscard();
	}

	public void Cancel()
	{
		OnCancel();
	}
}
