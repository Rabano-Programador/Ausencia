using UnityEngine;

public class TriggerAtaque : MonoBehaviour
{
    [Range(0, 100)]
    public float probabilidadDeAtaque = 25f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float roll = Random.Range(0f, 100f);

            if (roll <= probabilidadDeAtaque)
            {
                QTEManager.Instance.StartSeizure();
            }
            Destroy(gameObject);
        }
    }
}