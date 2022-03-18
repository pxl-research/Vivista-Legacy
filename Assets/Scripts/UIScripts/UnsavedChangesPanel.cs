using UnityEngine;

public class UnsavedChangesPanel : MonoBehaviour
{
	public delegate void SaveEvent();
	public SaveEvent OnSave;

	public delegate void DiscardEvent();
	public DiscardEvent OnDiscard;

	public delegate void CancelEvent();
	public CancelEvent OnCancel;

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
