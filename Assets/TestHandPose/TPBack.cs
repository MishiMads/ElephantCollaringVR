using UnityEngine;

public class TPBack : MonoBehaviour
{
    public GameObject cube;
    public Vector3 home;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        home = new Vector3(0.0599999987f, 1.00999999f, 0.370000005f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TPBackToHome()
    {
        Material mat = cube.GetComponent<Renderer>().material;
        mat.color = Color.green;
        cube.transform.position = home;
    }
}
