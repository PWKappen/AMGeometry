using UnityEngine;
using System.Collections;

public class PlaneController : MonoBehaviour {
	
    public float moveSpeed;
    public float rotateSpeed;
    public Loader loader;
	// Update is called once per frame
	void Update () {
	    if(Input.GetKey(KeyCode.W))
        {
            transform.position += transform.up * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.up * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            transform.Rotate(-Vector3.forward * Time.deltaTime * rotateSpeed, Space.World);
        }
        if (Input.GetKey(KeyCode.X))
        {
            transform.Rotate(Vector3.forward * Time.deltaTime * rotateSpeed, Space.World);
        }
        if(Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(-Vector3.right * Time.deltaTime * rotateSpeed, Space.World);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(Vector3.right * Time.deltaTime * rotateSpeed, Space.World);
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            loader.ShowNewPlane(transform.up, transform.position, transform.position + transform.right, transform.position + transform.forward);
        }
    }
}
