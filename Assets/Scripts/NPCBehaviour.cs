using UnityEngine;

public class NPCBehaviour : MonoBehaviour
{
    public float targetX = -4.08f;
    public float speed = 2f;

    void Update()
    {
        Vector3 pos = transform.position;

        // Move toward target X
        pos.x = Mathf.MoveTowards(pos.x, targetX, speed * Time.deltaTime);

        transform.position = pos;
    }
}