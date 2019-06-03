using UnityEngine;

public class AnimateProjector : MonoBehaviour
{
	public enum PlayStatus
	{
		Stopped,
		Playing
	}

	public PlayStatus playStatus;
	public Animator projector;
	public Player player;
	public GameObject previousPage;
	public GameObject nextPage;

	public ParticleSystem[] part;

	//up = true, down = false
	public bool state = true;

	void OnEnable()
	{
		EventManager.OnSpace += MenuAnimation;
	}

	void OnDisable()
	{
		EventManager.OnSpace -= MenuAnimation;
	}

	void Start()
	{
		foreach (var t in part)
		{
			t.Stop();
		}
	}

	public void MenuAnimation()
	{
		projector.SetTrigger(state ? "Down" : "Up");
		state = !state;
	}

	public void SetParticles()
	{
		foreach (var t in part)
		{
			if (t.isPlaying)
			{
				t.Stop();
			}
			else if (t.isStopped)
			{
				t.Play();
			}
		}
	}

	public void StartPillarRender()
	{

	}

	public void StopPillarRender()
	{

	}

	public void HologramUp()
	{
		player.OnVideoBrowserHologramUp();
	}

	public void HologramDown()
	{

	}

	public void AnimStart()
	{
	}

	public void AnimStop()
	{
		player.OnVideoBrowserAnimStop();
	}

	public void Subscribe(Player player)
	{
		this.player = player;
	}

	public void TogglePageButtons(GameObject indexPanel)
	{
		previousPage.SetActive(indexPanel.transform.Find("Panel").Find("Previous").gameObject.activeSelf);
		nextPage.gameObject.SetActive(indexPanel.transform.Find("Panel").Find("Next").gameObject.activeSelf);
	}
}
