using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Setting
{
[Serializable]
public class Sound
{
    [SerializeField] private int soundVolume;
    [SerializeField] private int vibrationVolume;

    // Не серіалізуємо AudioSource, але додаємо тимчасові посилання
    [NonSerialized] private AudioSource _buttonClick;
    [NonSerialized] private AudioSource _bgSound;
    [NonSerialized] private AudioSource _move;

    public int SoundVolume
    {
        get => soundVolume;
        set
        {
            soundVolume = Mathf.Clamp(value, 0, 100);
            UpdateAudioSourcesVolume();
        }
    }

    public int VibrationVolume
    {
        get => vibrationVolume;
        set => vibrationVolume = Mathf.Clamp(value, 0, 100);
    }

    public void SetAudioSources(AudioSource buttonClick, AudioSource bgSound, AudioSource move)
    {
        _buttonClick = buttonClick;
        _bgSound = bgSound;
        _move = move;
        UpdateAudioSourcesVolume();
    }

    private void UpdateAudioSourcesVolume()
    {
        float volume = soundVolume / 100f;
        if (_buttonClick != null) _buttonClick.volume = volume;
        if (_bgSound != null) _bgSound.volume = volume;
        if (_move != null) _move.volume = volume;
    }

    public void OnButtonClick()
    {
        if (_buttonClick != null) _buttonClick.Play();
    }

    public void OnMove()
    {
        if (_move == null)
        {
            Debug.LogError("OnMove null");
            return;
        }
        Debug.Log("OnMove");
        _move.Play();
    }

    public void StartBgSound()
    {
        if (_bgSound != null)
        {
            _bgSound.loop = true;
            _bgSound.Play();
        }
    }
}
}