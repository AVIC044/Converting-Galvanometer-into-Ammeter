using UnityEngine;

public class QuizAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;

    public void PlayCorrect()
    {
        audioSource.PlayOneShot(correctClip);
    }

    public void PlayWrong()
    {
        audioSource.PlayOneShot(wrongClip);
    }
}