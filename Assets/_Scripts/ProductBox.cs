using UnityEngine;

public class ProductBox : MonoBehaviour
{
    [Header("Datos del Producto")]
    public ProductoData datosProducto;
    public int unidadesRestantes = 6;

    public bool PillarProducto()
    {
        if (unidadesRestantes > 0)
        {
            unidadesRestantes--;
            if (unidadesRestantes <= 0)
            {
                Destroy(gameObject);
            }
            return true;
        }
        return false;
    }
}