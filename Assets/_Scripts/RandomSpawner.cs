using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    [Header("Configuración del Spawn")]
    public GameObject cajaPrefab; 

    public ProductoData[] posiblesProductos;

    [Header("Tiempos")]
    public float tiempoMinimo = 5f;
    public float tiempoMaximo = 15f;

    private float timer = 0f;
    private float tiempoObjetivo;

    void Start()
    {
        AsignarNuevoTiempo();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= tiempoObjetivo)
        {
            SpawnearCaja();
            AsignarNuevoTiempo();
        }
    }

    void SpawnearCaja()
    {
        GameObject nuevaCaja = Instantiate(cajaPrefab, transform.position, transform.rotation);
        ProductBox scriptCaja = nuevaCaja.GetComponent<ProductBox>();

        if (scriptCaja != null && posiblesProductos.Length > 0)
        {
            int indiceAleatorio = Random.Range(0, posiblesProductos.Length);
            scriptCaja.datosProducto = posiblesProductos[indiceAleatorio];
            scriptCaja.unidadesRestantes = 6;

            nuevaCaja.name = "Caja de " + scriptCaja.datosProducto.nombreProducto;
        }
    }

    void AsignarNuevoTiempo()
    {
        tiempoObjetivo = Random.Range(tiempoMinimo, tiempoMaximo);
        timer = 0f;
    }
}