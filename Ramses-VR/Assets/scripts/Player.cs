using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public AudioSource audioSource;
    public Animator animator;

    private const string TALK_PARAM = "isTalking";

    void Start()
    {

        if (audioSource == null)
            Debug.LogWarning("AudioSource not found on Player.");

        if (animator == null)
            Debug.LogWarning("Animator not found on Player.");
    }

    void Update()
    {
        if (animator != null && audioSource != null)
        {

            bool isTalking = audioSource.isPlaying;
            animator.SetBool(TALK_PARAM, isTalking);
        }
    }
}


