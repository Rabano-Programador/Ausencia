using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCSystem : MonoBehaviour
{
    public GameObject npcPrefab;
    public float tiempoMin = 10f;
    public float tiempoMax = 25f;
    public float radioBusquedaNavMeshSpawn = 3f;

    public int maxNPCsEnEscena = 5;

    public Transform zonaFilaCaja;
    public Transform mesaDeCobro;
    public Transform puntoSalida;

    public GameObject objetoCajaPrefab;

    private float timer = 0f;
    private float tiempoObjetivo;

    private List<NPCCliente> npcsActivos = new List<NPCCliente>();

    void Start()
    {
        CalcularSiguienteCliente();
    }

    void Update()
    {
        npcsActivos.RemoveAll(n => n == null);

        timer += Time.deltaTime;

        if (timer >= tiempoObjetivo)
        {
            if (npcsActivos.Count < maxNPCsEnEscena)
            {
                SpawnearNPC();
            }
            else
            {
                Debug.Log($"<color=orange>NPC Spawner: Límite alcanzado ({npcsActivos.Count}/{maxNPCsEnEscena}). Esperando espacio.</color>");
            }

            CalcularSiguienteCliente();
        }
    }

    void SpawnearNPC()
    {
        NavMeshHit hitSpawn;
        Vector3 posicionSpawn = transform.position;

        if (!NavMesh.SamplePosition(transform.position, out hitSpawn, radioBusquedaNavMeshSpawn, NavMesh.AllAreas))
        {
            Debug.LogWarning("<color=orange>NPC Spawner: No encontré NavMesh cerca del punto de spawn. NPC cancelado.</color>");
            return;
        }

        posicionSpawn = hitSpawn.position;

        GameObject nuevoNPC = Instantiate(npcPrefab, posicionSpawn, transform.rotation);
        NPCCliente ia = nuevoNPC.GetComponent<NPCCliente>();

        if (ia != null)
        {
            ia.zonaFilaCaja = this.zonaFilaCaja;
            ia.mesaDeCobro = this.mesaDeCobro;
            ia.puntoSalida = this.puntoSalida;
            ia.objetoCajaPrefab = this.objetoCajaPrefab;

            npcsActivos.Add(ia);
            Debug.Log($"<color=green>NPC Spawner: Nuevo cliente spawneado en {posicionSpawn} ({npcsActivos.Count}/{maxNPCsEnEscena}).</color>");
        }
    }

    void CalcularSiguienteCliente()
    {
        tiempoObjetivo = Random.Range(tiempoMin, tiempoMax);
        timer = 0f;
    }
}
