using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChapterTransitionPanel : MonoBehaviour
{
	public delegate void OnTransitionFinish();

	public Text text1;
	public Text text2;
	public Text text3;
	public Text text4;

	public void SetChapter(Chapter chapter)
	{
		string text = $"{chapter.name} \n{chapter.description}";
		text1.text = text;
		text2.text = text;
		text3.text = text;
		text4.text = text;
	}

	public void StartTransition(OnTransitionFinish OnFinish)
	{
		Canvass.sphereUIWrapper.SetActive(true);
		Canvass.sphereUIRenderer.SetActive(true);
		Canvass.sphereUIPanelWrapper.SetActive(false);

		StartCoroutine(Transition(OnFinish));
	}

	public IEnumerator Transition(OnTransitionFinish onTransitionFinish)
	{
		float animTime = .5f;

		yield return UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>(), animTime, .99f);

		yield return new WaitForSeconds(10 * animTime);

		yield return UIAnimation.FadeOut(GetComponent<RectTransform>(), GetComponent<CanvasGroup>(), animTime);

		onTransitionFinish();
		Canvass.sphereUIWrapper.SetActive(false);
		Canvass.sphereUIRenderer.SetActive(false);
		Canvass.sphereUIPanelWrapper.SetActive(true);
	}
}
