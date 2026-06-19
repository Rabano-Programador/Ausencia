using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    /* ===================================================================================
    PROPUESTA DE SISTEMA: TIEMPO, PRODUCTIVIDAD Y FATIGA (ESTRÉS)
    ===================================================================================
    Revisen esta lógica. La idea es quitar los triggers aleatorios invisibles
    y hacer que el ataque dependa de cuánto ha trabajado el jugador.

    Scripts que tendriamos que modificar si lo hicieramos:
    RestockShelf
    CajaRegistradora
    QTEManager
    
    public static GameManager Instance;

    [Header("Horario Laboral")]
    public float duracionDelTurnoEnMinutos = 5f; // El turno dura 5 minutos reales
    private float tiempoRestante;
    public TextMeshProUGUI textoReloj; // Para mostrar ej: "14:00 PM"

    [Header("Sistema de Estrés (Sobrecarga)")]
    public float nivelDeEstres = 0f;
    public float limiteParaAtaque = 100f; // Si llega a 100, el próximo cobro da ataque

    private void Awake()
    {
        Instance = this;
        tiempoRestante = duracionDelTurnoEnMinutos * 60f;
    }

    private void Update()
    {
        // 1. EL RELOJ DEL JUEGO
        // El tiempo va bajando. Si llega a 0, se termina el turno (Victoria o Derrota del día)
        if (tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            ActualizarRelojUI();
        }
        else
        {
            TerminarTurno();
        }

        // 2. EL ESTRÉS BAJA LENTAMENTE SI DESCANSAS (Opcional)
        // Si el jugador se queda quieto, el estrés podría bajar, pero pierde tiempo de trabajo.
        if (nivelDeEstres > 0)
        {
            nivelDeEstres -= Time.deltaTime * 2f; // Baja 2 puntos por segundo
        }
    }

    // 3. ESTO LO LLAMA LA ESTANTERÍA AL GUARDAR UNA CAJA
    public void RegistrarTrabajoRealizado(float cantidadEstres)
    {
        nivelDeEstres += cantidadEstres;
        Debug.Log("Trabajaste. Nivel de estrés actual: " + nivelDeEstres);
    }

    // 4. ESTO LO LLAMA LA CAJA REGISTRADORA ANTES DE ATENDER A UN CLIENTE
    public bool IntentarAtenderCaja()
    {
        if (nivelDeEstres >= limiteParaAtaque)
        {
            Debug.Log("¡SOBRECARGA SENSORIAL! Gatillando ataque en la caja...");
            
            // Reiniciamos el estrés para que no tenga 2 ataques seguidos
            nivelDeEstres = 0f; 
            
            // Llamamos al sistema que hicimos antes
            QTEManager.Instance.StartSeizure(); 
            
            return false; // Retorna falso porque NO te deja atender la caja
        }
        
        // Si no está estresado, le sumamos un poco de estrés por atender a la persona
        RegistrarTrabajoRealizado(20f); 
        return true; // Retorna true para que empiece el minijuego de matemáticas
    }

    private void ActualizarRelojUI()
    {
        // Matemáticas simples para convertir los segundos restantes en un formato "HH:MM" simulado
        // (Podemos pulirlo después)
    }

    private void TerminarTurno()
    {
        // Aquí conectamos las condiciones de victoria y derrota del GDD
        Debug.Log("Se acabó el día. Evaluando la productividad...");
    }
    ===================================================================================
    */
}