using System.Collections;
using UnityEngine;

public class ElephantMove : MonoBehaviour
{
    public float speed = 1f; // Speed of the elephant movement
    public GameObject target; // Target object to move towards

    private bool isMoving = false; // Flag to control movement
    void Start()
    {
        StartCoroutine(waitSecond());
    }

    // Update is called once per frame
    void Update()
    {
       

        if (isMoving)
            {
                StartMoving();
        }
    }

    public void StartMoving()
    {
        
        isMoving = true;
        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, target.transform.position, speed * Time.deltaTime);
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == target)
        {
            Debug.Log("Target reached!");
            isMoving = false; // Stop moving when the target is reached
            gameObject.GetComponent<Animator>().SetBool("Walking", false);
        }
    }
    IEnumerator waitSecond()
    {
        yield return new WaitForSeconds(3f);
            isMoving = true;
        gameObject.GetComponent<Animator>().SetBool("Walking", true);
    }

}

