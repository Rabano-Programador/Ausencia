using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    public static QueueManager Instance;

    public Transform[] puntosDeEspera;

    public Transform puntoMesaCobro;

    private Queue<NPCCliente> colaDeEspera = new Queue<NPCCliente>();
    private NPCCliente npcSiendoAtendido = null;

    private void Awake()
    {
        Instance = this;
    }

   
    public Vector3 UnirseACola(NPCCliente npc)
    {
        colaDeEspera.Enqueue(npc);
        return ObtenerPosicionEnCola(colaDeEspera.Count - 1);
    }

    public void NotificarAtendido(NPCCliente npc)
    {
        if (npcSiendoAtendido == npc)
        {
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
}