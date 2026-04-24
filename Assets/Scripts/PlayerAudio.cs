using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField]
    private AudioSource moveSource;

    [SerializeField]
    private AudioClip moveSFX;

    public void PlayMove()
    {
        if (moveSource != null && moveSFX != null)
            moveSource.PlayOneShot(moveSFX);
    }
}
