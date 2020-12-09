using UnityEngine;

public class UIPanels : MonoBehaviour
{
	public static UIPanels Instance { get; private set; }

	//NOTE(Simon): Only allowed to add to this list if it is a UI panel with its own script
	public ExplorerPanel explorerPanel;
	public ImportPanel importPanel;
	public ProjectPanel projectPanel;
	public UploadPanel uploadPanel;
	public InteractionTypePicker interactionTypePicker;
	public LoginPanel loginPanel;
	public ExportPanel exportPanel;
	public TagPanel tagPanel;

	public TextPanel textPanel;
	public TextPanelEditor textPanelEditor;
	public AudioPanel audioPanel;
	public AudioPanelEditor audioPanelEditor;
	public ImagePanel imagePanel;
	public ImagePanelEditor imagePanelEditor;
	public VideoPanel videoPanel;
	public VideoPanelEditor videoPanelEditor;
	public MultipleChoicePanel multipleChoicePanel;
	public MultipleChoicePanelEditor multipleChoicePanelEditor;
	public FindAreaPanel findAreaPanel;
	public FindAreaPanelEditor findAreaPanelEditor;
	public MultipleChoiceAreaPanel multipleChoiceAreaPanel;
	public MultipleChoiceAreaPanelEditor multipleChoiceAreaPanelEditor;
	public MultipleChoiceImagePanel multipleChoiceImagePanel;
	public MultipleChoiceImagePanelEditor multipleChoiceImagePanelEditor;
	public TabularDataPanel tabularDataPanel;
	public TabularDataPanelEditor tabularDataPanelEditor;

	private void Awake()
	{
		Instance = this;
	}
}
