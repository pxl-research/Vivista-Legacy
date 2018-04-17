using UnityEngine;

public class AnimateProjector : MonoBehaviour
{
	public enum PlayStatus
	{
		Stopped,
		Playing
	}
	public Animator projector;
	public PlayStatus playStatus;

	public ParticleSystem[] part;

	//up = true, down = false
	public bool state = true;
	private Player player;

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
		if (state)
		{
			projector.SetTrigger("Down");
		}
		else
		{
			projector.SetTrigger("Up");
		}
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
}
