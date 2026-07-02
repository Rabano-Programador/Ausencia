using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FatigueManager : MonoBehaviour
{
    public static FatigueManager Instance;

    [Header("Referencias")]
    public PlayerController player;
    [Tooltip("Arrastra una imagen negra que ocupe toda la pantalla para el parpadeo")]
    public Image imagenParpadeo;

    [Header("CONFIGURACIÓN DE UMBRALES (INSPECTOR)")]
    [Tooltip("¿Cuántos ataques deben pasar para activar el límite de Stamina?")]
    public int ataquesParaStamina = 1;
    [Tooltip("¿Cuántos ataques deben pasar para activar los parpadeos de sueño?")]
    public int ataquesParaParpadeo = 2;
    [Tooltip("¿Cuántos ataques deben pasar para activar los tiritones de mano?")]
    public int ataquesParaTiritones = 3;

    [Header("Monitoreo de Estado (Debug)")]
    [SerializeField] private int ataquesCompletados = 0;

    [Header("1. SISTEMA DE STAMINA")]
    public float maxStamina = 100f;
    public float desgasteStamina = 25f;
    public float recuperacionStamina = 15f;
    [SerializeField] private float staminaActual;
    private bool estaAgotado = false;

    [Header("2. SISTEMA DE PARPADEO")]
    public float tiempoMinEntreParpadeos = 4f;
    public float tiempoMaxEntreParpadeos = 8f;
    public float duracionParpadeo = 0.3f;
    private float timerParpadeo;

    [Header("3. SISTEMA DE TIRITONES (TEMBLORES)")]
    public float intensidadTiriton = 0.03f;
    private Vector3 originalCamPos;
    private bool camaraGuardada = false;

    #region Propiedades Públicas
    public bool CondicionStaminaActiva => ataquesCompletados >= ataquesParaStamina;
    public bool CondicionParpadeoActiva => ataquesCompletados >= ataquesParaParpadeo;
    public bool CondicionTiritonesActiva => ataquesCompletados >= ataquesParaTiritones;

    public bool CanSprint => CondicionStaminaActiva ? !estaAgotado : true;
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        staminaActual = maxStamina;

        if (imagenParpadeo != null)
        {
            imagenParpadeo.color = new Color(0f, 0f, 0f, 0f);
            imagenParpadeo.gameObject.SetActive(false);
        }

        ResetBlinkTimer();
    }

    public void IncrementarAtaquesCompletados()
    {
        ataquesCompletados++;
        Debug.Log("[FatigueManager] Ataque sufrido. Total: " + ataquesCompletados);
    }

    private void Update()
    {
        if (player == null) return;

        if (CondicionStaminaActiva)
        {
            ManejarStamina();
        }

        if (CondicionParpadeoActiva)
        {
            ManejarTiemposParpadeo();
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Camera camaraPrincipal = Camera.main;
        if (camaraPrincipal == null) return;

        if (camaraGuardada)
        {
            camaraPrincipal.transform.localPosition = originalCamPos;
            camaraGuardada = false;
        }

        if (!CondicionTiritonesActiva) return;

        bool agarrandoObjeto = player.GrabbedTransform != null;
        bool enLaCaja = player.EstaEnLaCaja;

        if (agarrandoObjeto || enLaCaja)
        {
            originalCamPos = camaraPrincipal.transform.localPosition;
            camaraGuardada = true;

            Vector3 offsetTemblores = new Vector3(
                Random.Range(-intensidadTiriton, intensidadTiriton),
                Random.Range(-intensidadTiriton, intensidadTiriton),
                0f
            );

            camaraPrincipal.transform.localPosition += offsetTemblores;
        }
    }

    private void ManejarStamina()
    {
        if (player.IsRunning && player.CanMove)
        {
            staminaActual -= desgasteStamina * Time.deltaTime;
            if (staminaActual <= 0)
            {
                staminaActual = 0;
                estaAgotado = true;
            }
        }
        else
        {
            staminaActual += recuperacionStamina * Time.deltaTime;
            if (staminaActual >= maxStamina)
            {
                staminaActual = maxStamina;
                estaAgotado = false;
            }
        }

        staminaActual = Mathf.Clamp(staminaActual, 0f, maxStamina);
    }

    private void ManejarTiemposParpadeo()
    {
        timerParpadeo -= Time.deltaTime;
        if (timerParpadeo <= 0)
        {
            StartCoroutine(EjecutarParpadeoOjos());
            ResetBlinkTimer();
        }
    }

    private IEnumerator EjecutarParpadeoOjos()
    {
        if (imagenParpadeo == null) yield break;

        imagenParpadeo.gameObject.SetActive(true);
        SetAlphaParpadeo(0f);

        float t = 0;
        float mitadDuracion = duracionParpadeo / 2f;
        while (t < mitadDuracion)
        {
            t += Time.deltaTime;
            SetAlphaParpadeo(Mathf.Lerp(0f, 1f, t / mitadDuracion));
            yield return null;
        }
        SetAlphaParpadeo(1f);

        yield return new WaitForSeconds(0.05f);

        t = 0;
        while (t < mitadDuracion)
        {
            t += Time.deltaTime;
            SetAlphaParpadeo(Mathf.Lerp(1f, 0f, t / mitadDuracion));
            yield return null;
        }
        SetAlphaParpadeo(0f);

        imagenParpadeo.gameObject.SetActive(false);
    }

    private void ResetBlinkTimer()
    {
        timerParpadeo = Random.Range(tiempoMinEntreParpadeos, tiempoMaxEntreParpadeos);
    }

    private void SetAlphaParpadeo(float alpha)
    {
        if (imagenParpadeo != null)
        {
            Color c = imagenParpadeo.color;
            c.a = alpha;
            imagenParpadeo.color = c;
        }
    }
}
