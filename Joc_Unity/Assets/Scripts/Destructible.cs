using UnityEngine;

public class Destructible : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 1f);
    }
}
