using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    public static QueueManager Instance;

    public Transform[] puntosDeEspera;

    public Transform puntoMesaCobro;
    public bool debugQueueManager = true;

    private Queue<NPCCliente> colaDeEspera = new Queue<NPCCliente>();
    private NPCCliente npcSiendoAtendido = null;

    private void Awake()
    {
        Instance = this;
    }

   
    public Vector3 UnirseACola(NPCCliente npc)
    {
        colaDeEspera.Enqueue(npc);
        LogDebug($"NPC '{npc.name}' entro a la cola. En cola: {colaDeEspera.Count}, atendiendo: {(npcSiendoAtendido != null ? npcSiendoAtendido.name : "nadie")}.");

        if (npcSiendoAtendido == null)
        {
            AvanzarCola();
            return puntoMesaCobro != null ? puntoMesaCobro.position : npc.transform.position;
        }

        return ObtenerPosicionEnCola(colaDeEspera.Count - 1);
    }

    public void NotificarAtendido(NPCCliente npc)
    {
        if (npcSiendoAtendido == npc)
        {
            LogDebug($"NPC '{npc.name}' fue notificado como atendido. Avanza la cola.");
            npcSiendoAtendido = null;
            AvanzarCola();
        }
    }

   
    public void DespachaNPCActual()
    {
        if (npcSiendoAtendido != null)
        {
            npcSiendoAtendido.RecibirPermisoDeSalir();
        }
    }

    private void AvanzarCola()
    {
        if (colaDeEspera.Count == 0) return;

        NPCCliente siguiente = colaDeEspera.Dequeue();
        npcSiendoAtendido = siguiente;
        LogDebug($"Nuevo NPC atendido: '{siguiente.name}'. Punto cobro: {(puntoMesaCobro != null ? puntoMesaCobro.name : "null")}.");

        siguiente.RecibirTurnoEnCaja(puntoMesaCobro != null ? puntoMesaCobro.position : siguiente.transform.position);

        int index = 0;
        foreach (NPCCliente npcEnCola in colaDeEspera)
        {
            npcEnCola.ActualizarPosicionEnCola(ObtenerPosicionEnCola(index));
            index++;
        }
    }

    private Vector3 ObtenerPosicionEnCola(int index)
    {
        if (puntosDeEspera != null && index < puntosDeEspera.Length && puntosDeEspera[index] != null)
        {
            return puntosDeEspera[index].position;
        }
        return transform.position + transform.forward * (index * -1.5f);
    }

    
    public void SalirDeCola(NPCCliente npc)
    {
        if (npcSiendoAtendido == npc)
        {
            npcSiendoAtendido = null;
            AvanzarCola();
        }
        else
        {
            List<NPCCliente> listaTemp = new List<NPCCliente>(colaDeEspera);
            if (listaTemp.Remove(npc))
            {
                colaDeEspera = new Queue<NPCCliente>(listaTemp);
                int index = 0;
                foreach (NPCCliente n in colaDeEspera)
                {
                    n.ActualizarPosicionEnCola(ObtenerPosicionEnCola(index));
                    index++;
                }
            }
        }
    }

    public int CantidadEnCola => colaDeEspera.Count + (npcSiendoAtendido != null ? 1 : 0);
    public bool HayNPCSiendoAtendido => npcSiendoAtendido != null;
    public NPCCliente NPCActualEnCaja => npcSiendoAtendido;

    private void LogDebug(string mensaje)
    {
        if (!debugQueueManager)
            return;

        Debug.Log($"<color=#FFD166>QueueManager: {mensaje}</color>");
    }
}
