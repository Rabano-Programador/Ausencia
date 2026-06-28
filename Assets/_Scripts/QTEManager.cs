using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

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

    [Header("SISTEMA DE TENSIÓN (BARRA INVISIBLE)")]
    public float velocidadPasiva = 0.2f;
    public float tensionPorCorrer = 1.5f;
    public float tensionPorReponer = 8.0f;
    public float tensionPorCobrar = 12.0f;
    public float maxTension = 100f;
    public float delayAtaquePostCaja = 3.0f;

    [Header("EFECTO VIÑETEADO (ANTICIPACIÓN)")]
    public Image imagenViñeta;
    public float tiempoAnticipacion = 3.0f;
    [Range(0f, 1f)]
    public float puntoCorteAbrupto = 0.6f;

    [Header("ANIMACIÓN DE CÁMARA (CINEMACHINE)")]
    public Animator animatorPivotPendulo;

    [Header("Monitoreo Debug (Inspector)")]
    [SerializeField] private float tensionActualVisual = 0f;

    private float currentTension = 0f;
    private bool ataqueEnEsperaPorCaja = false;
    private float timerDelayCaja = 0f;

    private bool estaEnAnticipacion = false;
    private float timerAnticipacion = 0f;

    #endregion

    #region Awake e inicio del ataque
    private void Awake()
    {
        Instance = this;
        blackScreenCanvas.SetActive(false);

        if (imagenViñeta != null)
        {
            imagenViñeta.gameObject.SetActive(false);
            SetAlphaViñeta(0f);
        }

        Behaviour brain = GetCinemachineBrain();
        if (brain != null)
        {
            brain.enabled = false;
        }
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
        if (estaEnAnticipacion)
        {
            ManejarAnimacionViñeta();
            return;
        }

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

        if (imagenViñeta != null)
        {
            imagenViñeta.gameObject.SetActive(false);
            SetAlphaViñeta(0f);
        }

        player.TeleportTo(originalPlayerPos);
        player.SetCanMove(true);

        player.bloquearCamaraPorAtaque = false;

        Behaviour brain = GetCinemachineBrain();
        if (brain != null)
        {
            brain.enabled = false;
        }

        if (animatorPivotPendulo != null)
        {
            animatorPivotPendulo.SetTrigger("Levantarse");
        }

        foreach (ActiveLetter letter in activeLettersOnScreen)
        {
            Destroy(letter.gameObject);
        }
        activeLettersOnScreen.Clear();
    }
    #endregion

    #region Lógica de Tensión y Anticipación
    private void ManejarAcumulacionTension()
    {
        if (PauseManager.AtaquesDesactivados)
        {
            currentTension = 0f;
            tensionActualVisual = 0f;
            return;
        }

        if (player == null) return;

        currentTension += velocidadPasiva * Time.deltaTime;

        if (player.IsRunning)
        {
            currentTension += tensionPorCorrer * Time.deltaTime;
        }

        currentTension = Mathf.Clamp(currentTension, 0f, maxTension);
        tensionActualVisual = currentTension;

        if (player.EstaEnLaCaja)
        {
            if (currentTension >= maxTension)
            {
                ataqueEnEsperaPorCaja = true;
            }
        }
        else
        {
            if (ataqueEnEsperaPorCaja)
            {
                timerDelayCaja += Time.deltaTime;
                if (timerDelayCaja >= delayAtaquePostCaja)
                {
                    IniciarAnticipacionAtaque();
                }
            }
            else if (currentTension >= maxTension)
            {
                IniciarAnticipacionAtaque();
            }
        }
    }

    public void AcumularTension(float cantidad)
    {
        if (isQTEActive || estaEnAnticipacion || PauseManager.AtaquesDesactivados) return;
        currentTension = Mathf.Clamp(currentTension + cantidad, 0f, maxTension);
    }

    private void IniciarAnticipacionAtaque()
    {
        estaEnAnticipacion = true;
        timerAnticipacion = 0f;

        player.SetCanMove(false);
        player.ForzarSoltarItem();

        player.bloquearCamaraPorAtaque = true;

        Behaviour brain = GetCinemachineBrain();
        if (brain != null)
        {
            brain.enabled = true;
        }

        if (animatorPivotPendulo != null)
        {
            animatorPivotPendulo.SetTrigger("Caer");
        }

        if (imagenViñeta != null)
        {
            imagenViñeta.gameObject.SetActive(true);
            SetAlphaViñeta(0f);
        }

        currentTension = 0f;
        ataqueEnEsperaPorCaja = false;
        timerDelayCaja = 0f;
    }

    private void ManejarAnimacionViñeta()
    {
        timerAnticipacion += Time.deltaTime;
        float porcentajeTiempo = timerAnticipacion / tiempoAnticipacion;

        if (porcentajeTiempo < puntoCorteAbrupto)
        {
            float progresoSuave = porcentajeTiempo / puntoCorteAbrupto;
            SetAlphaViñeta(progresoSuave * 0.5f);
        }
        else
        {
            SetAlphaViñeta(1f);
        }

        if (timerAnticipacion >= tiempoAnticipacion)
        {
            estaEnAnticipacion = false;
            StartSeizure();
        }
    }

    private void SetAlphaViñeta(float alpha)
    {
        if (imagenViñeta != null)
        {
            Color c = imagenViñeta.color;
            c.a = alpha;
            imagenViñeta.color = c;
        }
    }

    private Behaviour GetCinemachineBrain()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            return mainCam.GetComponent("CinemachineBrain") as Behaviour;
        }
        return null;
    }
    #endregion
}