using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LetterQTE : MonoBehaviour
{
    [Header("Referencias del Prefab")]
    public RectTransform circleTransform;
    public Image circleImage;
    public TextMeshProUGUI letterText;

    [Header("Tamanos del Circulo")]
    public float tamanoInicial = 250f;
    public float tamanoFinal = 80f;

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorAcierto = Color.green;
    public Color colorFallo = Color.red;

    [Header("Feedback Acierto")]
    public float duracionPop = 0.2f;
    public float escalaPop = 1.5f;

    [Header("Feedback Fallo")]
    public float duracionShake = 0.2f;
    public float intensidadShake = 10f;

    private KeyCode teclaAsignada;
    private float tiempoVida;
    private float tiempoTranscurrido;
    private bool muriendo = false;
    private Vector2 posicionOriginal;

    public KeyCode TeclaAsignada => teclaAsignada;

    public void Inicializar(KeyCode tecla, float tiempoDeVida)
    {
        teclaAsignada = tecla;
        tiempoVida = tiempoDeVida;
        tiempoTranscurrido = 0f;

        if (letterText != null)
            letterText.text = tecla.ToString();

        if (circleImage != null)
            circleImage.color = colorNormal;

        if (circleTransform != null)
            circleTransform.sizeDelta = new Vector2(tamanoInicial, tamanoInicial);

        posicionOriginal = GetComponent<RectTransform>().anchoredPosition;
    }

    private void Update()
    {
        if (muriendo) return;
        if (circleTransform == null) return;

        tiempoTranscurrido += Time.deltaTime;
        float progreso = Mathf.Clamp01(tiempoTranscurrido / tiempoVida);
        float tamanoActual = Mathf.Lerp(tamanoInicial, tamanoFinal, progreso);
        circleTransform.sizeDelta = new Vector2(tamanoActual, tamanoActual);
    }

    public void MorirBien()
    {
        if (muriendo) return;
        muriendo = true;
        StartCoroutine(AnimacionAcierto());
    }

    public void MorirMal()
    {
        if (muriendo) return;
        muriendo = true;
        StartCoroutine(AnimacionFallo());
    }

    private IEnumerator AnimacionAcierto()
    {
        if (circleImage != null) circleImage.color = colorAcierto;

        float t = 0f;
        Vector3 escalaInicio = transform.localScale;
        Vector3 escalaObjetivo = escalaInicio * escalaPop;

        while (t < duracionPop)
        {
            t += Time.deltaTime;
            float progreso = t / duracionPop;
            transform.localScale = Vector3.Lerp(escalaInicio, escalaObjetivo, progreso);

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f - progreso;

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator AnimacionFallo()
    {
        if (circleImage != null) circleImage.color = colorFallo;
        if (letterText != null) letterText.color = colorFallo;

        RectTransform rt = GetComponent<RectTransform>();
        float t = 0f;

        while (t < duracionShake)
        {
            t += Time.deltaTime;
            float offsetX = Random.Range(-intensidadShake, intensidadShake);
            float offsetY = Random.Range(-intensidadShake, intensidadShake);
            rt.anchoredPosition = posicionOriginal + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rt.anchoredPosition = posicionOriginal;
        Destroy(gameObject);
    }
}