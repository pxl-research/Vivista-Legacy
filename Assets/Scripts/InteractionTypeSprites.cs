using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class InteractionTypeSprites : MonoBehaviour
{
	public static InteractionTypeSprites Instance;

	public Sprite text;
	public Sprite image;
	public Sprite video;
	public Sprite multipleChoice;
	public Sprite audio;
	public Sprite findArea;
	public Sprite multipleChoiceArea;
	public Sprite multipleChoiceImage;
	public Sprite tabularData;
	public Sprite chapter;

	private static Dictionary<InteractionType, Sprite> sprites = new Dictionary<InteractionType, Sprite>
	{
		{InteractionType.Text, null}
	};

	private void Start()
	{
		Assert.IsTrue(Instance == null, "There are two isntances of this script. Only one is allowed");
		Instance = this;

		sprites.Clear();

		sprites.Add(InteractionType.None, null);
		sprites.Add(InteractionType.Text, text);
		sprites.Add(InteractionType.Image, image);
		sprites.Add(InteractionType.Video, video);
		sprites.Add(InteractionType.MultipleChoice, multipleChoice);
		sprites.Add(InteractionType.Audio, audio);
		sprites.Add(InteractionType.FindArea, findArea);
		sprites.Add(InteractionType.MultipleChoiceArea, multipleChoiceArea);
		sprites.Add(InteractionType.MultipleChoiceImage, multipleChoiceImage);
		sprites.Add(InteractionType.TabularData, tabularData);
		sprites.Add(InteractionType.Chapter, chapter);

		Assert.IsTrue(sprites.Count == Enum.GetValues(typeof(InteractionType)).Length, "You forgot to add the interaction sprite here");
	}

	public static Sprite GetSprite(InteractionType type)
	{
		if (sprites.ContainsKey(type))
		{
			return sprites[type];
		}
		else
		{
			Debug.Log($"Sprite for {type} not found");
			return null;
		}
	}
}
