using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This attribute ensures that an AudioSource component is always present on the GameObject.
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Tooltip("List of music tracks to be played.")]
    public List<AudioClip> musicTracks;

    [Tooltip("The duration in seconds for a track to fade in and out.")]
    [SerializeField] private float fadeDuration = 2.0f;
    
    [Tooltip("The maximum volume the music should reach.")]
    [SerializeField] [Range(0.0f, 1.0f)] private float maxVolume = 0.8f;

    private AudioSource _audioSource;
    private List<AudioClip> _shuffledTracks;
    private int _currentTrackIndex = -1;

    // Start is called before the first frame update
    void Start()
    {
        // Get the AudioSource component attached to this GameObject.
        _audioSource = GetComponent<AudioSource>();
        // Ensure the audio doesn't loop through the AudioSource component itself.
        _audioSource.loop = false; 

        // Check if there are any tracks to play.
        if (musicTracks == null || musicTracks.Count == 0)
        {
            Debug.LogWarning("MusicManager: No music tracks have been assigned.");
            return;
        }

        // Start the main music playing coroutine.
        StartCoroutine(PlayShuffledMusic());
    }
    
    private IEnumerator PlayShuffledMusic()
    {
        // Initial shuffle of the tracks.
        ShuffleTracks();

        // The main loop to continuously play music.
        while (true)
        {
            // Move to the next track in the shuffled list.
            _currentTrackIndex++;

            // If we've reached the end of the list, reshuffle and start from the beginning.
            if (_currentTrackIndex >= _shuffledTracks.Count)
            {
                _currentTrackIndex = 0;
                ShuffleTracks();
                Debug.Log("Playlist finished. Reshuffling tracks.");
            }

            // Get the next clip to play.
            AudioClip nextClip = _shuffledTracks[_currentTrackIndex];

            // Start the fade-in process for the new track.
            yield return StartCoroutine(FadeIn(nextClip));
            
            // Wait for the clip to almost finish playing before starting the fade-out.
            // We subtract the fade duration to ensure a smooth transition.
            yield return new WaitForSeconds(nextClip.length - fadeDuration);

            // Start the fade-out process.
            yield return StartCoroutine(FadeOut());
        }
    }
    
    private void ShuffleTracks()
    {
        // Create a copy of the original list to avoid modifying it.
        _shuffledTracks = new List<AudioClip>(musicTracks);
        
        // Fisher-Yates shuffle algorithm.
        for (int i = _shuffledTracks.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            // Swap elements.
            AudioClip temp = _shuffledTracks[i];
            _shuffledTracks[i] = _shuffledTracks[randomIndex];
            _shuffledTracks[randomIndex] = temp;
        }
    }
    
    /// <param name="clip">The AudioClip to play and fade in.</param>
    private IEnumerator FadeIn(AudioClip clip)
    {
        _audioSource.clip = clip;
        _audioSource.Play();
        
        float timer = 0f;
        while (timer < fadeDuration)
        {
            // Linearly interpolate the volume from 0 to maxVolume over the fade duration.
            _audioSource.volume = Mathf.Lerp(0, maxVolume, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame.
        }
        // Ensure the volume is set to maxVolume at the end.
        _audioSource.volume = maxVolume;
    }
    private IEnumerator FadeOut()
    {
        float startVolume = _audioSource.volume;
        float timer = 0f;
        
        while (timer < fadeDuration)
        {
            // Linearly interpolate the volume from its current level to 0.
            _audioSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame.
        }
        
        // Ensure the volume is 0 at the end and stop the playback.
        _audioSource.volume = 0;
        _audioSource.Stop();
    }
}