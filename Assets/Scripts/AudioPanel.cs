using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.IO;
using System;

public class AudioPanel : MonoBehaviour
{

    public GameObject audioContainer;
    public GameObject controllButton;
    public Canvas canvas;
    public Texture iconPlay;
    public Texture iconPause;
    public Text title;

    public AudioSource audioSource;
    public AudioClip clip;

    public static bool keepFileNames;
    public string url;


    void Start()
    {
        //NOTE(Kristof): Initial rotation towards the camera 
        canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);

    }

    // Update is called once per frame
    void Update()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        controllButton.GetComponent<RawImage>().texture = !audioSource.isPlaying ? iconPause : iconPlay;

        // NOTE(Lander): Rotate the panels to the camera
        if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
        {
            canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
        }

    }

    private void OnDestroy()
    {
        if (keepFileNames || !audioSource) return;
        var filename = "file://" + Application.streamingAssetsPath + "/Sound/";

        //   var filename = audioSource.url;

        if (File.Exists(filename) && Path.GetExtension(filename) != "")
        {
            var newfilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            try
            {
                File.Move(filename, newfilename);
            }
            catch (IOException)
            {
                try
                {
                    Debug.LogFormat("File Already exists? deleting: {0}", newfilename);
                    File.Delete(filename);
                }
                catch (IOException e2)
                {
                    Debug.LogErrorFormat("Something went wrong while moving the file. Aborting. \n{0} ", e2.Message);
                }
            }
        }
    }
    public void Init(string newTitle, string fullPath, string guid, bool prepareNow = false)
    {
        audioSource = Instantiate(audioSource);

        if (Player.hittables != null)
        {
            GetComponentInChildren<Hittable>().enabled = true;
        }

        var folder = Path.Combine(Application.persistentDataPath, guid);

        if (!File.Exists(fullPath))
        {
            var pathNoExtension = Path.Combine(Path.Combine(folder, SaveFile.extraPath), Path.GetFileNameWithoutExtension(fullPath));
            if (!File.Exists(pathNoExtension))
            {
                Debug.LogErrorFormat("Cannot find extension-less audio file: {1} {0}", pathNoExtension, File.Exists(pathNoExtension));
                return;
            }

            try
            {
                File.Move(pathNoExtension, fullPath);
            }
            catch (IOException e)
            {
                Debug.LogErrorFormat("Cannot add extension to file: {0}\n{2}\n{1}", pathNoExtension, e.Message, fullPath);
                //return;
            }

        }

        //  audioSource.url = fullPath;
        title.text = newTitle;

        audioSource.playOnAwake = false;


    }
    public void TogglePlay()
    {
        audioSource.clip = clip;
        (audioSource.isPlaying ? (Action)audioSource.Pause : audioSource.Play)();
        audioSource.PlayOneShot(clip);
        controllButton.GetComponent<RawImage>().texture = audioSource.isPlaying ? iconPause : iconPlay;
    }
    private WWW GetAudioFromFile(string path, string filename)
    {
        string audioToLoad = string.Format(path + "{0}", filename);
        WWW request = new WWW(audioToLoad);
        return request;
    }

    // NOTE(Lander): copied from image panel
    public void Move(Vector3 position)
    {
        var newPos = position;
        newPos.y += 0.015f;
        canvas.GetComponent<RectTransform>().position = position;
    }
}
