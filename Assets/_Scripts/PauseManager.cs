using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }
    public static bool ModoPruebasActivo { get; private set; }

    [Header("UI Pausa")]
    [SerializeField] private GameObject panelPausa;

    [Header("Boton Modo Pruebas")]
    [SerializeField] private Graphic graficoBotonModoPruebas;
    [SerializeField] private TMP_Text textoBotonModoPruebas;
    [SerializeField] private TMP_Text textoAyudaModoPruebas;
    [SerializeField] private string textoModoPruebasActivo = "Modo pruebas: ON";
    [SerializeField] private string textoModoPruebasInactivo = "Modo pruebas: OFF";
    [SerializeField] private Color colorModoPruebasActivo = Color.green;
    [SerializeField] private Color colorModoPruebasInactivo = Color.red;

    [SerializeField] private float dineroDebugF5 = 100f;
    [SerializeField] private float estresDebugF6 = 25f;
    [SerializeField] private float segundosDebugF8 = 60f;

    private PlayerController player;
    private NPCSystem npcSystem;
    private RandomSpawner randomSpawner;
    private QueueManager queueManager;
    private GameManager gameManager;
    private QTEManager qteManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsPaused = false;
        ModoPruebasActivo = false;
        player = FindFirstObjectByType<PlayerController>();
        npcSystem = FindFirstObjectByType<NPCSystem>();
        randomSpawner = FindFirstObjectByType<RandomSpawner>();
        queueManager = FindFirstObjectByType<QueueManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        qteManager = FindFirstObjectByType<QTEManager>();

        if (panelPausa != null)
            panelPausa.SetActive(false);

        ActualizarVisualModoPruebas();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePausa();

        if (!ModoPruebasActivo)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
            ForzarSpawnNPC();
        else if (Input.GetKeyDown(KeyCode.F2))
            ForzarSpawnCaja();
        else if (Input.GetKeyDown(KeyCode.F3))
            DespacharNPCActual();
        else if (Input.GetKeyDown(KeyCode.F4))
            ForzarQTE();
        else if (Input.GetKeyDown(KeyCode.F5))
            AgregarDineroDebug();
        else if (Input.GetKeyDown(KeyCode.F6))
            AgregarEstresDebug();
        else if (Input.GetKeyDown(KeyCode.F7))
            ResetearEstresDebug();
        else if (Input.GetKeyDown(KeyCode.F8))
            SumarTiempoDebug();
        else if (Input.GetKeyDown(KeyCode.F9))
            RestarTiempoDebug();
    }

    public void TogglePausa()
    {
        if (IsPaused)
            ReanudarJuego();
        else
            PausarJuego();
    }

    public void PausarJuego()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (panelPausa != null)
            panelPausa.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ReanudarJuego()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            player.AplicarEstadoCursor();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ToggleModoPruebas()
    {
        ModoPruebasActivo = !ModoPruebasActivo;
        ActualizarVisualModoPruebas();
    }

    private void ActualizarVisualModoPruebas()
    {
        if (graficoBotonModoPruebas != null)
            graficoBotonModoPruebas.color = ModoPruebasActivo ? colorModoPruebasActivo : colorModoPruebasInactivo;

        if (textoBotonModoPruebas != null)
            textoBotonModoPruebas.text = ModoPruebasActivo ? textoModoPruebasActivo : textoModoPruebasInactivo;

        if (textoAyudaModoPruebas != null)
        {
            textoAyudaModoPruebas.text =
                "F1: Spawn NPC\n" +
                "F2: Spawn caja\n" +
                "F3: Despachar NPC actual\n" +
                "F4: Forzar QTE\n" +
                "F5: Sumar dinero\n" +
                "F6: Sumar estres\n" +
                "F7: Resetear estres\n" +
                "F8: Sumar 60s\n" +
                "F9: Restar 60s";

            textoAyudaModoPruebas.color = ModoPruebasActivo ? colorModoPruebasActivo : colorModoPruebasInactivo;
        }
    }

    private void ForzarSpawnNPC()
    {
        if (npcSystem == null)
            npcSystem = FindFirstObjectByType<NPCSystem>();

        if (npcSystem != null)
            npcSystem.ForzarSpawnNPC();
    }

    private void ForzarSpawnCaja()
    {
        if (randomSpawner == null)
            randomSpawner = FindFirstObjectByType<RandomSpawner>();

        if (randomSpawner != null)
            randomSpawner.ForzarSpawnCaja();
    }

    private void DespacharNPCActual()
    {
        if (queueManager == null)
            queueManager = FindFirstObjectByType<QueueManager>();

        if (queueManager != null)
            queueManager.DespachaNPCActual();
    }

    private void ForzarQTE()
    {
        if (qteManager == null)
            qteManager = FindFirstObjectByType<QTEManager>();

        if (qteManager != null)
            qteManager.StartSeizure();
    }

    private void AgregarDineroDebug()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.SumarDineroDebug(dineroDebugF5);
    }

    private void AgregarEstresDebug()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.AgregarEstresDebug(estresDebugF6);
    }

    private void ResetearEstresDebug()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.ResetearEstresDebug();
    }

    private void SumarTiempoDebug()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.AjustarTiempoRestanteDebug(segundosDebugF8);
    }

    private void RestarTiempoDebug()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.AjustarTiempoRestanteDebug(-segundosDebugF8);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            IsPaused = false;
        }
    }
}
