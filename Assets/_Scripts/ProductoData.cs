using UnityEngine;

[CreateAssetMenu(fileName = "NuevoProducto", menuName = "Producto/Producto")]
public class ProductoData : ScriptableObject
{
    public string nombreProducto;
    public Sprite iconoCaja;
    // public float precio; 
}