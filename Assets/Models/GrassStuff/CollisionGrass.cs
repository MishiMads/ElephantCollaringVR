using UnityEngine;

public class CollisionGrass : MonoBehaviour
{
    private CapsuleCollider capsuleCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        Shader.SetGlobalVector("_GameObjectPos", transform.position + Vector3.up * capsuleCollider.radius);
    }
}
