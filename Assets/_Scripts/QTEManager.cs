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
        if (!isQTEActive) return;

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
}
#endregion