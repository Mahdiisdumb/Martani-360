using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SoundTestManager : MonoBehaviour
{
    [Header("Audio List (fill this in inspector)")]
    public List<AudioClip> clips = new List<AudioClip>();
    public TextMeshProUGUI label;
    public TextMeshProUGUI label2;

    private List<string> labels = new List<string>();
    private AudioSource source;
    private int index = 0;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        GenerateLabels();
        UpdateUI();
    }

    void GenerateLabels()
    {
        labels.Clear();

        for (int i = 0; i < clips.Count; i++)
        {
            labels.Add(ConvertToSoundTestLabel(i));
        }
    }

    string ConvertToSoundTestLabel(int i)
    {
        // 00–09
        if (i < 10)
            return "0" + i;

        // 10–19
        if (i < 20)
            return i.ToString();

        // A0–Z9 style
        int letterIndex = (i - 20) / 10;
        int number = (i - 20) % 10;

        char letter = (char)('A' + letterIndex);
        return letter.ToString() + number.ToString();
    }

    public void Next()
    {
        if (clips.Count == 0) return;

        index = (index + 1) % clips.Count;
        Play();
    }

    public void Prev()
    {
        if (clips.Count == 0) return;

        index--;
        if (index < 0)
            index = clips.Count - 1;

        Play();
    }

    public void Play()
    {
        if (clips.Count == 0) return;

        source.clip = clips[index];
        source.Play();

        UpdateUI();

        Debug.Log($"Playing: {labels[index]} - {clips[index].name}");
    }

    public void Stop()
    {
        source.Stop();
        Debug.Log("Stopped");
    }

    public string GetCurrentLabel()
    {
        if (labels.Count == 0) return "--";
        return labels[index];
    }

    void UpdateUI()
    {
        if (labels.Count > 0)
            label.text = GetCurrentLabel();
        else
            label.text = "--";

        if (clips.Count > 0 && clips[index] != null)
            label2.text = clips[index].name;
        else
            label2.text = "--";
    }
}