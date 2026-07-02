using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class NPCSystem : MonoBehaviour
{
    public GameObject[] npcPrefabs;
    public float tiempoMin = 10f;
    public float tiempoMax = 25f;
    public float radioBusquedaNavMeshSpawn = 3f;

    public int maxNPCsEnEscena = 5;

    public Transform zonaFilaCaja;
    [FormerlySerializedAs("mesaDeCobro")] public Transform puntoDespachoProductos;
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
            }

            CalcularSiguienteCliente();
        }
    }

    void SpawnearNPC()
    {
        if (npcPrefabs == null || npcPrefabs.Length == 0)
            return;

        NavMeshHit hitSpawn;
        Vector3 posicionSpawn = transform.position;

        if (!NavMesh.SamplePosition(transform.position, out hitSpawn, radioBusquedaNavMeshSpawn, NavMesh.AllAreas))
        {
            return;
        }

        posicionSpawn = hitSpawn.position;

        GameObject prefabElegido = npcPrefabs[Random.Range(0, npcPrefabs.Length)];

        if (prefabElegido == null)
            return;

        GameObject nuevoNPC = Instantiate(prefabElegido, posicionSpawn, transform.rotation);
        NPCCliente ia = nuevoNPC.GetComponent<NPCCliente>();

        if (ia != null)
        {
            ia.zonaFilaCaja = this.zonaFilaCaja;
            ia.puntoDespachoProductos = this.puntoDespachoProductos;
            ia.puntoSalida = this.puntoSalida;
            ia.objetoCajaPrefab = this.objetoCajaPrefab;

            npcsActivos.Add(ia);
        }
    }

    public void ForzarSpawnNPC()
    {
        npcsActivos.RemoveAll(n => n == null);

        if (npcsActivos.Count >= maxNPCsEnEscena)
        {
            return;
        }

        SpawnearNPC();
        CalcularSiguienteCliente();
    }

    void CalcularSiguienteCliente()
    {
        tiempoObjetivo = Random.Range(tiempoMin, tiempoMax);
        timer = 0f;
    }
}