using UnityEngine;

[CreateAssetMenu(fileName = "NuevoProducto", menuName = "Producto/Producto")]
public class ProductoData : ScriptableObject
{
    public string nombreProducto;
    public float precio;
    public Sprite iconoCaja;

    [Header("Modelo 3D Individual")]
    public GameObject prefabIndividual;

    [Header("Ajuste Visual en Estanteria")]
    public Vector3 escalaEnEstanteria = Vector3.one;
    public Vector3 rotacionEnEstanteria = Vector3.zero;

    [Header("Ajuste Visual en Caja/Spawn")]
    public Vector3 escalaEnCaja = Vector3.one;
    public Vector3 rotacionEnCaja = Vector3.zero;
}
