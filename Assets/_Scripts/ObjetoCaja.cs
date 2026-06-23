using UnityEngine;

public class ObjetoCaja : MonoBehaviour
{
    [Header("Datos")]
    public ProductoData datosProducto;

    [Header("Configuración de Cobro")]
    public float precioProducto = 4.99f;

    [HideInInspector]
    public bool estaEnZonaEspera = false;

    private void Start()
    {
        if (datosProducto != null)
        {
            precioProducto = datosProducto.precio;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ZonaEsperaCobro"))
        {
            estaEnZonaEspera = true;
            Debug.Log("Objeto listo en la mesa de espera.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ZonaEsperaCobro"))
        {
            estaEnZonaEspera = false;
        }
    }

    public void TeletransportarA(Vector3 posicionDestino)
    {
        transform.position = posicionDestino;
        estaEnZonaEspera = false;
    }
}
