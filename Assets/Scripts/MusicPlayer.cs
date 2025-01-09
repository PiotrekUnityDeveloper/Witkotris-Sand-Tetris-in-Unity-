using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer instance;


    // Start is called before the first frame update
    void Start()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this);
        if(GetComponent<AudioSource>() == null) PlayRandomSong();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<AudioClip> clips = new List<AudioClip>();
    private AudioSource globalSrc;
    public void PlayRandomSong()
    {
        AudioSource src = this.AddComponent<AudioSource>();
        globalSrc = src;
        int rand = UnityEngine.Random.Range(0, clips.Count);
        src.clip = clips[rand];
        src.Play();

        if(songqueue != null)
        {
            StopCoroutine(songqueue);
            songqueue = StartCoroutine(NextSong(clips[rand].length));
        }
        else
        {
            songqueue = StartCoroutine(NextSong(clips[rand].length));
        }
    }

    public void PlaySong(int index)
    {
        AudioSource src = this.AddComponent<AudioSource>();
        globalSrc = src;
        src.clip = clips[index];
        src.Play();

        if (songqueue != null)
        {
            StopCoroutine(songqueue);
            songqueue = StartCoroutine(NextSong(clips[index].length));
        }
        else
        {
            songqueue = StartCoroutine(NextSong(clips[index].length));
        }
    }

    private Coroutine songqueue;
    private IEnumerator NextSong(float wait)
    {
        yield return new WaitForSeconds(wait);
        StopMusic();
        PlayRandomSong();
    }

    public void StopMusic()
    {
        if(GetComponent<AudioSource>() != null) globalSrc.Stop();
        globalSrc = null;
        Destroy(globalSrc);
    }

    public void ToggleMusic()
    {
        if (GetComponent<AudioSource>() != null) { StopMusic(); } else
        {
            PlayRandomSong();
        }
    }

    public int lastSong = 0;

    public void DifferentSong()
    {
        if(GetComponent<AudioSource>() != null) { StopMusic(); }

        lastSong += 1;
        if(lastSong > clips.Count - 1)
        {
            lastSong = 0;
        }

        PlaySong(lastSong);
    }
}
