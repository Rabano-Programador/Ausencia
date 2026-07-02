using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Canales de Audio")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Musica")]
    public AudioClip musicaNivel;
    public AudioClip musicaAtaque;

    [Header("Sonidos del Jugador")]
    public AudioClip sonidoPasosJugador;
    public AudioClip sonidoRespiracion;
    public AudioClip sonidoPasosJugador2;
    public AudioClip sonidoRecogerItem;
    public AudioClip sonidoAgacharse;



    [Header("Sonidos de Caja")]
    public AudioClip sonidoScannerCaja;
    public AudioClip sonidoCobroCaja;
    public AudioClip sonidoBotonTransbank;
    

        [Header("Sonidos de NPCS")]
    public AudioClip sonidoMurmulloNPC;

    [Header("UI de Audio")]
    public AudioMixer audioMixer;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider masterSlider;

    void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        if (instance != this) return;

        if (bgmSource != null && musicaNivel != null)
        {
            bgmSource.clip = musicaNivel;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
    }

    public void ReproducirSonido(AudioClip clip)
    {
        if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip);
    }


    public void CambiarMusicaNormal()//De aqui se cambia la musica normal a la musica de ataque
    {
        if (bgmSource != null && musicaAtaque != null && bgmSource.clip != musicaAtaque)
        {
            StartCoroutine(TransicionMusica(musicaAtaque));
        }
    }

    public void CambiarMusicaAtaque() //De aqui se cambia la musica del ataque a la musica normal
    {
        if (bgmSource != null && musicaNivel != null && bgmSource.clip != musicaNivel)
        {
            StartCoroutine(TransicionMusica(musicaNivel));
        }
    }

    private IEnumerator TransicionMusica(AudioClip nuevaMusica)
    {
        float volumenOriginal = bgmSource.volume;
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(volumenOriginal, 0, t);
            yield return null;
        }

        bgmSource.clip = nuevaMusica;
        bgmSource.Play();
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, volumenOriginal, t);
            yield return null;
        }
        bgmSource.volume = volumenOriginal;
    }

    public void SetBGMVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("BGMVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("SFXVolume", volume);
    }
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null) audioMixer.SetFloat("MasterVolume", volume);
    }
}