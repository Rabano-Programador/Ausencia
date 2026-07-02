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
    public float tiempoMinEntreParpadeos = 6f;
    public float tiempoMaxEntreParpadeos = 14f;
    public float duracionParpadeo = 0.25f;
    private float timerParpadeo;

    [Header("3. SISTEMA DE TIRITONES (TEMBLORES)")]
    public float intensidadTiriton = 0.04f;

    #region Propiedades Públicas (Para que otros scripts lean los estados)
    public bool CondicionStaminaActiva => ataquesCompletados >= ataquesParaStamina;
    public bool CondicionParpadeoActiva => ataquesCompletados >= ataquesParaParpadeo;
    public bool CondicionTiritonesActiva => ataquesCompletados >= ataquesParaTiritones;

    // Propiedad que usará el PlayerController para saber si puede correr
    public bool CanSprint => CondicionStaminaActiva ? !estaAgotado : true;
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        staminaActual = maxStamina;

        if (imagenParpadeo != null)
        {
            imagenParpadeo.gameObject.SetActive(false);
            SetAlphaParpadeo(0f);
        }

        ResetBlinkTimer();
    }

    // Esta función la llamaremos desde el QTEManager al ganar el minijuego
    public void IncrementarAtaquesCompletados()
    {
        ataquesCompletados++;
        Debug.Log("Fatiga Incrementada. Ataques totales sufridos: " + ataquesCompletados);
    }

    private void Update()
    {
        if (player == null) return;

        // 1. LÓGICA DE STAMINA (Correr)
        if (CondicionStaminaActiva)
        {
            ManejarStamina();
        }

        // 2. LÓGICA DE PARPADEO (Falta de sueño)
        if (CondicionParpadeoActiva)
        {
            ManejarTiemposParpadeo();
        }
    }

    // Usamos LateUpdate para los tiritones para que se apliquen DESPUÉS de que el PlayerController mueva la cámara
    private void LateUpdate()
    {
        if (player == null || !CondicionTiritonesActiva) return;

        // Verificar las dos condiciones de tiritón: Agarrando objeto O en la Caja Registradora
        bool agarrandoObjeto = player.GrabbedTransform != null;
        bool enLaCaja = player.EstaEnLaCaja;

        if (agarrandoObjeto || enLaCaja)
        {
            AplicarTiritonCamara();
        }
    }

    #region Mecánica 1: Stamina
    private void ManejarStamina()
    {
        if (player.IsRunning && player.CanMove) // Asumiendo que CanMove o variable similar valida el movimiento
        {
            staminaActual -= desgasteStamina * Time.deltaTime;
            if (staminaActual <= 0)
            {
                staminaActual = 0;
                estaAgotado = true; // Castigado: No puede correr hasta recuperarse
            }
        }
        else
        {
            staminaActual += recuperacionStamina * Time.deltaTime;
            if (staminaActual >= maxStamina)
            {
                staminaActual = maxStamina;
                estaAgotado = false; // Recuperado por completo
            }
        }

        staminaActual = Mathf.Clamp(staminaActual, 0f, maxStamina);
    }
    #endregion

    #region Mecánica 2: Parpadeos
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

        // Cierra ojos (Va a negro)
        float t = 0;
        while (t < duracionParpadeo / 2f)
        {
            t += Time.deltaTime;
            SetAlphaParpadeo(Mathf.Lerp(0f, 1f, t / (duracionParpadeo / 2f)));
            yield return null;
        }

        // Abre ojos (Vuelve transparente)
        t = 0;
        while (t < duracionParpadeo / 2f)
        {
            t += Time.deltaTime;
            SetAlphaParpadeo(Mathf.Lerp(1f, 0f, t / (duracionParpadeo / 2f)));
            yield return null;
        }

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
    #endregion

    #region Mecánica 3: Tiritones
    private void AplicarTiritonCamara()
    {
        Camera camaraPrincipal = Camera.main;
        if (camaraPrincipal != null)
        {
            // Genera un desfase sutil y aleatorio frame a frame en la posición local de la cámara
            Vector3 offsetTemblores = new Vector3(
                Random.Range(-intensidadTiriton, intensidadTiriton),
                Random.Range(-intensidadTiriton, intensidadTiriton),
                0f
            );

            camaraPrincipal.transform.localPosition += offsetTemblores;
        }
    }
    #endregion
}
