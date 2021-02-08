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

	public void StartTransition()
	{
		StartCoroutine(Transition(OnFinish));
	}

	public IEnumerator Transition(OnTransitionFinish onTransitionFinish)
	{
		float animTime = .5f;

		Debug.Log("FadeIn");
		yield return UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>(), animTime);

		Debug.Log("Waiting");
		yield return new WaitForSeconds(6 * animTime);

		Debug.Log("FadeOut");
		yield return UIAnimation.FadeOut(GetComponent<RectTransform>(), GetComponent<CanvasGroup>(), animTime);

		onTransitionFinish();
	}

	public void OnFinish()
	{
		Debug.Log("Finish");
	}
}
