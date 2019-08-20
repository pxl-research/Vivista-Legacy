using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{

    public AudioClip[] audioClips;
    private int currentTrack;
    private AudioSource source;

    // Use this for initialization
    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    public void PlayAudio()
    {
        if (source.isPlaying)
        {
            return;
        }

        currentTrack--;

        if (currentTrack < 0)
        {
            currentTrack = audioClips.Length - 1;
        }
        StartCoroutine(WaitForAudioEnd());
    }


    IEnumerator WaitForAudioEnd()
    {
        while (source.isPlaying)
        {
            yield return null;
        }
        NextTitle();
    }

    public void NextTitle()
    {
        source.Stop();
        currentTrack++;
        if (currentTrack > audioClips.Length - 1)
        {
            currentTrack = 0;
        }
        source.clip = audioClips[currentTrack];
        source.Play();
        StartCoroutine(WaitForAudioEnd());
    }

    public void PreviousTitle()
    {
        source.Stop();
        currentTrack--;
        if (currentTrack < 0 )
        {
            currentTrack = audioClips.Length - 1;
        }
        source.clip = audioClips[currentTrack];
        source.Play();
        StartCoroutine(WaitForAudioEnd());
    }

    public void StopMusic()
    {
        StopCoroutine("WaitForAudioEnd");
        source.Stop();
    }

    public void MuteMusic()
    {
        source.mute = !source.mute;
    }
}
