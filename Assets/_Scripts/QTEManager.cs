using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QTEManager : MonoBehaviour
{
    #region Variables
    public static QTEManager Instance;

    [Header("Referencias")]
    public PlayerController player;
    public GameObject blackScreenCanvas;
    public GameObject letterPrefab;
    public RectTransform spawnArea;
    public Transform voidDropPosition;

    [Header("Configuración de Tiempos")]
    public float maxAbsoluteTime = 35f;
    public float requiredActiveTime = 15f;

    private float currentAbsoluteTime = 0f;
    private float currentActiveTime = 0f;
    private bool isQTEActive = false;
    private Vector3 originalPlayerPos;

    [Header("Dificultad")]
    public float initialSpawnRate = 1.5f;
    public float minSpawnRate = 0.15f;
    private float currentSpawnRate;
    private float spawnTimer;

    private KeyCode[] possibleKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V };

    private class ActiveLetter
    {
        public GameObject gameObject;
        public KeyCode key;
        public float expirationTime;
    }
    private List<ActiveLetter> activeLettersOnScreen = new List<ActiveLetter>();

    // ==========================================
    // NUEVAS VARIABLES: SISTEMA DE TENSIÓN
    // ==========================================
    [Header("SISTEMA DE TENSIÓN (BARRA INVISIBLE)")]
    public float velocidadPasiva = 0.2f;       // Cuánta tensión sube por segundo de forma natural (muy despacio)
    public float tensionPorCorrer = 1.5f;      // Cuánta tensión extra sube por segundo al correr con Shift
    public float tensionPorReponer = 8.0f;     // Empujón directo a la barra al colocar una caja en estante
    public float tensionPorCobrar = 12.0f;     // Empujón directo a la barra al marcar un producto en la caja
    public float maxTension = 100f;            // Límite para gatillar el ataque
    public float delayAtaquePostCaja = 3.0f;   // El tiempo de espera (2 o 3 segundos) configurable en Inspector

    [Header("Monitoreo Debug (Inspector)")]
    [SerializeField] private float tensionActualVisual = 0f;

    private float currentTension = 0f;
    private bool ataqueEnEsperaPorCaja = false;
    private float timerDelayCaja = 0f;

    #endregion

    #region Awake e inicio del ataque
    private void Awake()
    {
        Instance = this;
        blackScreenCanvas.SetActive(false);
    }

    public void StartSeizure()
    {
        if (isQTEActive) return;
        isQTEActive = true;

        originalPlayerPos = player.transform.position;
        player.SetCanMove(false);
        player.TeleportTo(voidDropPosition.position);

        blackScreenCanvas.SetActive(true);
        currentAbsoluteTime = 0f;
        currentActiveTime = 0f;
        currentSpawnRate = initialSpawnRate;
        spawnTimer = 0f;
        activeLettersOnScreen.Clear();
    }
    #endregion

    #region Update
    private void Update()
    {
        // Si el QTE no está activo, procesamos la acumulación del medidor de tensión pasivo/activo
        if (!isQTEActive)
        {
            ManejarAcumulacionTension();
            return;
        }

        currentAbsoluteTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= currentSpawnRate)
        {
            SpawnRandomLetter();
            spawnTimer = 0f;
            currentSpawnRate = Mathf.Max(minSpawnRate, currentSpawnRate - 0.05f);
        }

        for (int i = activeLettersOnScreen.Count - 1; i >= 0; i--)
        {
            ActiveLetter currentLetter = activeLettersOnScreen[i];

            if (Input.GetKeyDown(currentLetter.key))
            {
                currentActiveTime += 0.5f;
                Destroy(currentLetter.gameObject);
                activeLettersOnScreen.RemoveAt(i);
            }
            else if (Time.time > currentLetter.expirationTime)
            {
                Destroy(currentLetter.gameObject);
                activeLettersOnScreen.RemoveAt(i);
            }
        }

        if (currentActiveTime >= requiredActiveTime || currentAbsoluteTime >= maxAbsoluteTime)
        {
            EndSeizure();
        }
    }

    private void SpawnRandomLetter()
    {
        GameObject newLetterObj = Instantiate(letterPrefab, spawnArea);
        RectTransform rect = newLetterObj.GetComponent<RectTransform>();

        float randomX = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
        float randomY = Random.Range(-spawnArea.rect.height / 2f, spawnArea.rect.height / 2f);
        rect.anchoredPosition = new Vector2(randomX, randomY);

        KeyCode randomKey = possibleKeys[Random.Range(0, possibleKeys.Length)];
        newLetterObj.GetComponent<TextMeshProUGUI>().text = randomKey.ToString();

        float timeToLive = Mathf.Clamp(currentSpawnRate * 2f, 0.5f, 3f);

        activeLettersOnScreen.Add(new ActiveLetter
        {
            gameObject = newLetterObj,
            key = randomKey,
            expirationTime = Time.time + timeToLive
        });
    }

    private void EndSeizure()
    {
        isQTEActive = false;
        blackScreenCanvas.SetActive(false);

        player.TeleportTo(originalPlayerPos);
        player.SetCanMove(true);

        foreach (ActiveLetter letter in activeLettersOnScreen)
        {
            Destroy(letter.gameObject);
        }
        activeLettersOnScreen.Clear();
    }
    #endregion

    #region Lógica interna de Tensión
    private void ManejarAcumulacionTension()
    {
        // SEGURO DEBUG: Si desactivas los ataques desde el PauseManager, no acumula ni ataca
        if (PauseManager.AtaquesDesactivados)
        {
            currentTension = 0f;
            tensionActualVisual = 0f;
            return;
        }

        if (player == null) return;

        // 1. Acumulación Pasiva Temporal
        currentTension += velocidadPasiva * Time.deltaTime;

        // 2. Acumulación Activa: Correr
        if (player.IsRunning)
        {
            currentTension += tensionPorCorrer * Time.deltaTime;
        }

        currentTension = Mathf.Clamp(currentTension, 0f, maxTension);
        tensionActualVisual = currentTension; // Sincroniza la barra para monitorearla en el Inspector

        // 3. Gestión de la Caja Registradora (Seguro de Zona Activa)
        if (player.EstaEnLaCaja)
        {
            if (currentTension >= maxTension)
            {
                ataqueEnEsperaPorCaja = true; // Queda en cola hasta que el jugador presione E para salir
            }
        }
        else
        {
            // Si el ataque se guardó mientras cobraba, procesa los 2-3 segundos de Delay configurados
            if (ataqueEnEsperaPorCaja)
            {
                timerDelayCaja += Time.deltaTime;
                if (timerDelayCaja >= delayAtaquePostCaja)
                {
                    GatillarAtaqueDefinitivo();
                }
            }
            // Si la barra se llena estando afuera caminando normalmente, el ataque es directo
            else if (currentTension >= maxTension)
            {
                GatillarAtaqueDefinitivo();
            }
        }
    }

    // Método público para mandarle empujones directos desde el PlayerController (Cobrar/Reponer)
    public void AcumularTension(float cantidad)
    {
        if (isQTEActive || PauseManager.AtaquesDesactivados) return;
        currentTension = Mathf.Clamp(currentTension + cantidad, 0f, maxTension);
    }

    private void GatillarAtaqueDefinitivo()
    {
        // SEGURO DE OBJETOS: Obliga al jugador a soltar lo que tenga en las manos para limpiar la pantalla
        player.ForzarSoltarItem();

        // Reseteo completo del medidor de tensión
        currentTension = 0f;
        ataqueEnEsperaPorCaja = false;
        timerDelayCaja = 0f;

        // Lanza el ataque
        StartSeizure();
    }
    #endregion
}