using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Setting
{

public class SoundManager : MonoBehaviour
{
    [Inject] private Global _global;
    private Sound _sound;

    [Header("UI References")] [SerializeField]
    private Slider soundSlider;
    [SerializeField] private Slider vibrationSlider;
    [SerializeField] private Button[] soundButtons;
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource buttonClickAudio;
    [SerializeField] private AudioSource bgSoundAudio;
    [SerializeField] private AudioSource moveAudio;
    
    
    private const string SOUND_PREFS_KEY = "SoundSettings";

    public void Init(bool isFirstStart)
    {
        if (isFirstStart)
        {
            _sound = LoadSettings();
            _global.Sound = _sound;
        }
        else
        {
            _sound = _global.Sound;
        }
        
        _sound.SetAudioSources(buttonClickAudio, bgSoundAudio, moveAudio);

        SetupSliders();
        SetupButtons();
        _sound.StartBgSound();
    }

    private void SetupSliders()
    {
        if (soundSlider != null)
        {
            soundSlider.minValue = 0;
            soundSlider.maxValue = 100;
            soundSlider.value = _sound.SoundVolume;
            soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        }

        if (vibrationSlider != null)
        {
            vibrationSlider.minValue = 0;
            vibrationSlider.maxValue = 100;
            vibrationSlider.value = _sound.VibrationVolume;
            vibrationSlider.onValueChanged.AddListener(OnVibrationVolumeChanged);
        }
    }

    private void SetupButtons()
    {
        if (soundButtons.Length == 0) return;

        foreach (var button in soundButtons)
        {
            button.onClick.AddListener(_sound.OnButtonClick);
        }
    }
    
    private Sound LoadSettings()
    {
        if (PlayerPrefs.HasKey(SOUND_PREFS_KEY))
        {
            string json = PlayerPrefs.GetString(SOUND_PREFS_KEY);
            return JsonUtility.FromJson<Sound>(json);
        }

        // Default values
        return new Sound
        {
            SoundVolume = 80, // 80% by default
            VibrationVolume = 50 // 50% by default
        };
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(_sound);
        PlayerPrefs.SetString(SOUND_PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    private void OnSoundVolumeChanged(float value)
    {
        _sound.SoundVolume = Mathf.RoundToInt(value);
        ApplySoundSettings();
    }

    private void OnVibrationVolumeChanged(float value)
    {
        _sound.VibrationVolume = Mathf.RoundToInt(value);
    }

    private void ApplySoundSettings()
    {
        AudioListener.volume = _sound.SoundVolume / 100f;
    }

    private void OnDestroy()
    {
        if (soundSlider != null)
            soundSlider.onValueChanged.RemoveListener(OnSoundVolumeChanged);

        if (vibrationSlider != null)
            vibrationSlider.onValueChanged.RemoveListener(OnVibrationVolumeChanged);
    }
}
}