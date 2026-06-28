using UnityEngine;

public class ObjetoCaja : MonoBehaviour
{
    [Header("Datos")]
    public ProductoData datosProducto;

    [Header("Configuración de Cobro")]
    public float precioProducto = 4.99f;

    [HideInInspector]
    public bool estaEnZonaEspera = false;
    [HideInInspector]
    public bool estaEnPuntoEntrega = false;
    [HideInInspector]
    public bool disponibleParaCobro = true;

    private void Start()
    {
        ActualizarPrecioDesdeDatos();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PuntoEntrega"))
        {
            estaEnPuntoEntrega = true;

            PuntoEntregaTrigger triggerEntrega = other.GetComponent<PuntoEntregaTrigger>();
            if (triggerEntrega != null)
                triggerEntrega.RegistrarObjeto(this);

            return;
        }

        if (other.CompareTag("ZonaEsperaCobro"))
        {
            estaEnZonaEspera = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("PuntoEntrega"))
        {
            estaEnPuntoEntrega = false;
            PuntoEntregaTrigger triggerEntrega = other.GetComponent<PuntoEntregaTrigger>();
            if (triggerEntrega != null)
                triggerEntrega.QuitarObjeto(this);
            return;
        }

        if (other.CompareTag("ZonaEsperaCobro"))
        {
            estaEnZonaEspera = false;
        }
    }

    public void TeletransportarA(Vector3 posicionDestino)
    {
        transform.position = posicionDestino;
        estaEnZonaEspera = false;
        disponibleParaCobro = false;
    }

    public void ConfigurarProducto(ProductoData producto)
    {
        datosProducto = producto;
        disponibleParaCobro = true;
        ActualizarPrecioDesdeDatos();
    }

    void ActualizarPrecioDesdeDatos()
    {
        if (datosProducto != null)
            precioProducto = datosProducto.precio;
    }
}
