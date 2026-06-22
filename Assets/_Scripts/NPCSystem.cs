using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPCSystem : MonoBehaviour
{
    public GameObject npcPrefab;
    public float tiempoMin = 10f;
    public float tiempoMax = 25f;

    [Header("Rutas Generales")]
    public Transform zonaFilaCaja;
    public Transform mesaDeCobro;
    public Transform puntoSalida;

    [Header("El producto que dejan en la caja")]
    public GameObject objetoCajaPrefab;

    private float timer = 0f;
    private float tiempoObjetivo;

    void Start() { CalcularSiguienteCliente(); }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= tiempoObjetivo)
        {
            GameObject nuevoNPC = Instantiate(npcPrefab, transform.position, transform.rotation);

            NPCCliente ia = nuevoNPC.GetComponent<NPCCliente>();
            if (ia != null)
            {
                ia.zonaFilaCaja = this.zonaFilaCaja;
                ia.mesaDeCobro = this.mesaDeCobro;
                ia.puntoSalida = this.puntoSalida;
                ia.objetoCajaPrefab = this.objetoCajaPrefab;
            }
            CalcularSiguienteCliente();
        }
    }
    void CalcularSiguienteCliente() { tiempoObjetivo = Random.Range(tiempoMin, tiempoMax); timer = 0f; }
}