using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Horario Laboral")]
    public float duracionDelTurnoEnMinutos = 5f;
    private float tiempoRestante;
    public TextMeshProUGUI textoReloj;

    [Header("Productividad (KPI / Dinero)")]
    public float dineroAcumulado = 0f;
    public TextMeshProUGUI textoProductividad;

    [Header("Sistema de Estrés (Sobrecarga)")]
    public float nivelDeEstres = 0f;
    public float limiteParaAtaque = 100f;
    public TextMeshProUGUI textoEstres; // Opcional, para ver el % de estrés en pantalla

    private void Awake()
    {
        Instance = this;
        tiempoRestante = duracionDelTurnoEnMinutos * 60f;
    }

    private void Update()
    {
        if (tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            ActualizarUI();
        }
        else
        {
            Debug.Log("¡TURNO TERMINADO! Evaluar victoria/derrota.");
        }

        if (nivelDeEstres > 0)
        {
            nivelDeEstres -= Time.deltaTime * 1.5f; 
        }
    }

    public void RegistrarTrabajo(float estresAgregado)
    {
        nivelDeEstres += estresAgregado;
        ChequearAtaqueEpilepsia();
    }

    public void RegistrarVenta(float monto, bool cobroCorrecto)
    {
        if (cobroCorrecto)
        {
            dineroAcumulado += monto;
            nivelDeEstres += 10f; 
        }
        else
        {
            dineroAcumulado -= (monto / 2);
            nivelDeEstres += 25f; 
            Debug.Log("<color=red>¡Cobro incorrecto! Productividad reducida.</color>");
        }

        ChequearAtaqueEpilepsia();
    }

    private void ChequearAtaqueEpilepsia()
    {
        if (nivelDeEstres >= limiteParaAtaque)
        {
            Debug.Log("¡SOBRECARGA! Gatillando ataque...");
            nivelDeEstres = 0f;
            QTEManager.Instance.StartSeizure();
        }
    }

    private void ActualizarUI()
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60F);
        int segundos = Mathf.FloorToInt(tiempoRestante - minutos * 60);
        if (textoReloj != null) textoReloj.text = string.Format("{0:00}:{1:00}", minutos, segundos);

        if (textoProductividad != null) textoProductividad.text = "Productividad: $" + dineroAcumulado.ToString("F2");

        if (textoEstres != null) textoEstres.text = "Estrés: " + Mathf.Clamp(nivelDeEstres, 0, 100).ToString("F0") + "%";
    }
}