using UnityEngine;

public class Machete : MonoBehaviour
{
    public string targetSocketID = ""; // Match this to the ToolSocket ID

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            var socket = other.GetComponent<ToolSocket>();

                // Access the parent (the tree) and disable it
                if (other.transform.parent != null)
                {
                    GameObject bushParent = other.transform.parent.gameObject;

                    Destroy(bushParent);
                }
        }
    }
}