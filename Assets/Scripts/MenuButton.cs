using UnityEngine;
using UnityEngine.Events;


public class MenuButton : MonoBehaviour
{
    public Material materialIdle;
    public Material materialHover;

    private MeshRenderer _meshRenderer;

    public UnityEvent onClick;

    private AudioSource _audioSource;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _audioSource = GetComponent<AudioSource>();
    }

    public void OnHoverEnter()
    {
        PlayHoverSound();

        _meshRenderer.material = materialHover;

        var position = transform.position;
        position.z += 0.25f;
        transform.position = position;
    }

    public void OnHoverExit()
    {
        _meshRenderer.material = materialIdle;

        var position = transform.position;
        position.z -= 0.25f;
        transform.position = position;
    }

    public void PlayHoverSound()
    {
        _audioSource.clip = hoverSound;
        _audioSource.Play();
    }

    public void PlayClickSound()
    {
        _audioSource.clip = clickSound;
        _audioSource.Play(); 
    }
}
