using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PreAtaqueManager : MonoBehaviour
{
    public QTEManager qteManager;
    public PlayerController playerController;
    public MotorSequelHandler motorSequelHandler;

    public GameObject vignetteCanvas;
    public Image vignetteImage;
    public Image blinkImage;

    public AudioSource musicaNivel;

    public float duracionTotal = 5f;
    public float velocidadReducida = 3f;

    public int cantidadPestañeos = 3;
    public float duracionPestañeo = 0.08f;
    public float intervaloMinimo = 0.6f;
    public float intervaloMaximo = 1.4f;

    private bool eventoActivo = false;
    private bool yaActivado = false;

    private float walkSpeedOriginal;
    private float runSpeedOriginal;

    void Update()
    {
        if (qteManager == null || eventoActivo) return;

        if (yaActivado)
        {
            if (qteManager.currentTension < qteManager.maxTension / 2f)
                yaActivado = false;
            return;
        }

        if (qteManager.currentTension >= qteManager.maxTension / 2f)
        {
            StartCoroutine(EjecutarPreAtaque());
        }
    }

    IEnumerator EjecutarPreAtaque()
    {
        AudioManager.instance.sfxSource.PlayOneShot(AudioManager.instance.sonidoRespiracion, 4f);

        eventoActivo = true;
        yaActivado = true;

        if (musicaNivel != null) musicaNivel.Pause();

        walkSpeedOriginal = playerController.walkSpeed;
        runSpeedOriginal = playerController.runSpeed;
        playerController.walkSpeed = velocidadReducida;
        playerController.runSpeed = velocidadReducida;

        if (vignetteCanvas != null) vignetteCanvas.SetActive(true);
        if (vignetteImage != null) vignetteImage.enabled = true;
        if (blinkImage != null) blinkImage.enabled = false;

        StartCoroutine(GestionarPestañeos());

        yield return new WaitForSeconds(duracionTotal);

        StopCoroutine(GestionarPestañeos());

        if (blinkImage != null) blinkImage.enabled = false;
        if (vignetteImage != null) vignetteImage.enabled = false;
        if (vignetteCanvas != null) vignetteCanvas.SetActive(false);

        playerController.walkSpeed = walkSpeedOriginal;
        playerController.runSpeed = runSpeedOriginal;

        eventoActivo = false;

        if (motorSequelHandler != null) motorSequelHandler.ActivateSequel();
    }

    IEnumerator GestionarPestañeos()
    {
        while (true)
        {
            float espera = Random.Range(intervaloMinimo, intervaloMaximo);
            yield return new WaitForSeconds(espera);

            if (blinkImage != null) blinkImage.enabled = true;
            yield return new WaitForSeconds(duracionPestañeo);
            if (blinkImage != null) blinkImage.enabled = false;
        }
    }
}