// Responsible team member: Zhiyan Lin; Description: Keeps the main scene background music player alive across scene loads.
using UnityEngine;

[DisallowMultipleComponent]
public class MainSceneBgmPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private void Awake()
    {
        if (bgmClip == null)
        {
            return;
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = bgmClip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
}
