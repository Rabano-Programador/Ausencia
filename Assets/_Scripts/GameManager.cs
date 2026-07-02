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
    }

    public void RegistrarVenta(float monto, bool cobroCorrecto)
    {
        if (cobroCorrecto)
        {
            dineroAcumulado += monto;
        }
        else
        {
            dineroAcumulado -= (monto / 2);
        }

        ActualizarUI();
    }

    public void SumarDineroDebug(float monto)
    {
        dineroAcumulado += monto;
        ActualizarUI();
    }

    public void AjustarTiempoRestanteDebug(float segundos)
    {
        tiempoRestante = Mathf.Max(0f, tiempoRestante + segundos);
        ActualizarUI();
    }

    private void ActualizarUI()
    {
        int minutos = Mathf.FloorToInt(tiempoRestante / 60F);
        int segundos = Mathf.FloorToInt(tiempoRestante - minutos * 60);
        if (textoReloj != null) textoReloj.text = string.Format("{0:00}:{1:00}", minutos, segundos);

        if (textoProductividad != null) textoProductividad.text = "Productividad: $" + dineroAcumulado.ToString("F2");
    }
}
