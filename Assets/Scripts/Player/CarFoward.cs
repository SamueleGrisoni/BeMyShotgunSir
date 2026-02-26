using UnityEngine;

public class CarForward : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        transform.Translate(Vector3.forward * (speed * Time.deltaTime));
    }
}
