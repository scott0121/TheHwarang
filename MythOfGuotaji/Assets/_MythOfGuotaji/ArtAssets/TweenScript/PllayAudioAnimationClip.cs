using UnityEngine;

public class PlayAudioAnimationClip : MonoBehaviour
{
    public AudioSource source;
    public AudioClip[] clips;
 
    public void PlayAnimationSound(int index)
    {
        if (source == null )
        {
            source = GetComponent<AudioSource>();
        }
        if (source != null && clips != null && index >= 0 && index < clips.Length)
        {
            source.PlayOneShot(clips[index]);
        }
    }

}


