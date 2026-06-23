using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class MotorSequelHandler : MonoBehaviour
{
    [Header("Activacion")]
    [SerializeField] private Key activationKey = Key.K;

    [Header("Combinacion incomoda")]
    [SerializeField] private Key key1 = Key.LeftShift;
    [SerializeField] private Key key2 = Key.G;
    [SerializeField] private Key key3 = Key.N;

    [Header("UI")]
    [SerializeField] private Canvas sequelCanvas;
    [SerializeField] private RectTransform targetArea;
    [SerializeField] private RectTransform canvasRect;

    [Header("Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text resultText;

    [Header("Pulso de la barra")]
    [SerializeField] private float pulseStartThreshold = 5f;
    [SerializeField] private float pulseDangerThreshold = 3f;

    [Header("Movimiento del target")]
    [SerializeField] private float moveSpeed = 300f;
    [SerializeField] private float changeDirectionInterval = 1f;
    [SerializeField] private float margin = 140f;

    [Header("Duracion y dificultad")]
    [SerializeField] private float requiredHoldTime = 5f;
    [SerializeField] private float totalTimeLimit = 10f;

    [Header("Cursor (camara FPS)")]
    [SerializeField] private CursorLockMode lockModeAlSalir = CursorLockMode.Locked;
    [SerializeField] private bool ocultarCursorAlSalir = true;

    [Header("Bloqueo de camara/movimiento")]
    [SerializeField] private MonoBehaviour playerControllerToDisable;

    [Header("Eventos")]
    public UnityEvent onSequelSuccess;
    public UnityEvent onSequelFailed;

    private bool active;
    private float holdTimer;
    private float globalTimer;
    private Vector2 currentDirection;
    private float dirTimer;

    void Start()
    {
        if (sequelCanvas != null) sequelCanvas.gameObject.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current[activationKey].wasPressedThisFrame && !active)
            ActivateSequel();

        if (!active) return;

        globalTimer += Time.deltaTime;

        if (globalTimer >= totalTimeLimit)
        {
            EndSequel(false);
            return;
        }

        MoveTarget();

        bool k1 = Keyboard.current[key1].isPressed;
        bool k2 = Keyboard.current[key2].isPressed;
        bool k3 = Keyboard.current[key3].isPressed;
        bool keysHeld = k1 && k2 && k3;
        bool cursorOk = IsCursorInTarget();
        bool allOk = keysHeld && cursorOk;

        if (allOk)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= requiredHoldTime)
            {
                EndSequel(true);
                return;
            }
        }
        else
        {
            holdTimer = Mathf.Max(0f, holdTimer - Time.deltaTime * 0.5f);
        }

        UpdateFeedback(k1, k2, k3, cursorOk);
    }

    void UpdateFeedback(bool k1, bool k2, bool k3, bool cursorOk)
    {
        float restanteHold = Mathf.Max(0f, requiredHoldTime - holdTimer);
        float restanteGlobal = Mathf.Max(0f, totalTimeLimit - globalTimer);

        if (progressBar != null)
        {
            progressBar.maxValue = totalTimeLimit;
            progressBar.value = restanteGlobal;
        }

        if (progressBar != null)
        {
            if (restanteGlobal <= pulseStartThreshold)
            {
                float pulseSpeed = restanteGlobal <= pulseDangerThreshold ? 12f : 6f;
                float pulseAmount = restanteGlobal <= pulseDangerThreshold ? 0.08f : 0.04f;
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                progressBar.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else
            {
                progressBar.transform.localScale = Vector3.one;
            }
        }

        if (statusText != null)
        {
            string sShift = k1 ? "<color=#00FF66>SHIFT OK</color>" : "<color=#FF4444>SHIFT</color>";
            string sG = k2 ? "<color=#00FF66>G OK</color>" : "<color=#FF4444>G</color>";
            string sN = k3 ? "<color=#00FF66>N OK</color>" : "<color=#FF4444>N</color>";
            string sCursor = cursorOk ? "<color=#00FF66>CURSOR OK</color>" : "<color=#FF4444>APUNTA AL OBJETIVO</color>";

            string colorHold = restanteHold <= 1.5f ? "#00FF66" : "#FFFFFF";

            statusText.text =
                $"{sShift}   {sG}   {sN}\n" +
                $"{sCursor}\n" +
                $"<color={colorHold}>Te falta aguantar: {restanteHold:0.0}s</color>";
        }
    }

    void ActivateSequel()
    {
        active = true;
        holdTimer = 0f;
        globalTimer = 0f;

        if (sequelCanvas != null) sequelCanvas.gameObject.SetActive(true);
        if (resultText != null) resultText.gameObject.SetActive(false);

        if (playerControllerToDisable != null)
            playerControllerToDisable.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PickNewDirection();
    }

    void EndSequel(bool success)
    {
        active = false;

        if (success) onSequelSuccess?.Invoke();
        else onSequelFailed?.Invoke();

        StartCoroutine(ShowResultAndClose(success));
    }

    IEnumerator ShowResultAndClose(bool success)
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            if (success)
                resultText.text = "<color=#00FF66>TE RECUPERAS</color>";
            else
                resultText.text = "<color=#FF4444>ERROR LABORAL</color>";
        }

        if (statusText != null) statusText.text = "";
        if (progressBar != null) progressBar.value = 0f;
        if (progressBar != null) progressBar.transform.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(1.5f);

        if (sequelCanvas != null) sequelCanvas.gameObject.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);

        Cursor.lockState = lockModeAlSalir;
        Cursor.visible = !ocultarCursorAlSalir;

        if (playerControllerToDisable != null)
            playerControllerToDisable.enabled = true;
    }

    void MoveTarget()
    {
        if (targetArea == null || canvasRect == null) return;

        dirTimer += Time.deltaTime;
        if (dirTimer >= changeDirectionInterval)
            PickNewDirection();

        Vector2 pos = targetArea.anchoredPosition;
        pos += currentDirection * moveSpeed * Time.deltaTime;

        Vector2 size = canvasRect.rect.size;
        float halfW = size.x * 0.5f - margin;
        float halfH = size.y * 0.5f - margin;

        if (pos.x > halfW || pos.x < -halfW) currentDirection.x = -currentDirection.x;
        if (pos.y > halfH || pos.y < -halfH) currentDirection.y = -currentDirection.y;

        pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
        pos.y = Mathf.Clamp(pos.y, -halfH, halfH);

        targetArea.anchoredPosition = pos;
    }

    void PickNewDirection()
    {
        currentDirection = Random.insideUnitCircle.normalized;
        dirTimer = 0f;
    }

    bool IsCursorInTarget()
    {
        if (targetArea == null) return false;
        Vector2 mouse = Mouse.current.position.ReadValue();
        return RectTransformUtility.RectangleContainsScreenPoint(targetArea, mouse, null);
    }
}