using UnityEngine;

public class AnimateProjector : MonoBehaviour 
{
	public Animator projector;
	public ParticleSystem[] part;

	//up = true, down = false
	bool state = true;

	void OnEnable()
	{
		EventManager.OnSpace += MenuAnimation;
	}

	void OnDisable ()
	{
		EventManager.OnSpace -= MenuAnimation;
	}

	void Start ()
	{
		foreach (var t in part)
		{
			t.Stop();
		}
	}

	public void MenuAnimation()
	{
		if (state) {
			projector.SetTrigger ("Down");
		} else {
			projector.SetTrigger ("Up");
		}
		state = !state;
	}

	public void SetParticles ()
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

	}

	public void Subscribe(Player player)
	{
		this.player = player;
	}
}
