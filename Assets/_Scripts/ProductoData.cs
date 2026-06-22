using UnityEngine;

[CreateAssetMenu(fileName = "NuevoProducto", menuName = "Producto/Producto")]
public class ProductoData : ScriptableObject
{
    public string nombreProducto;
    public float precio;
    public Sprite iconoCaja;

    [Header("Modelo 3D Individual")]
    public GameObject prefabIndividual;
}