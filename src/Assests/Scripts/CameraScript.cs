using UnityEngine;
using System.Collections.Generic;

public class CameraScript : MonoBehaviour {

    public Vector3 eye;
    public Vector3 lookAt;

    public float rotateSpeed = 1f;
    public float zoomSpeed = 1f;
    public float moveSpeed = 1f;
    public float gridScaling = 0.001f;

    private GridManager gridManager;

    private bool leftMouseDown;
    private bool rightMouseDown;
    private bool clickedButton;
    private Camera camera;

	// Use this for initialization
	public void NewEye(float size)
    {
        float scaling = 10f;
        transform.position = eye;
        transform.LookAt(lookAt);
        camera.orthographicSize = size;
        gridManager.startX = lookAt.x - size/scaling * gridManager.gridSizeX/2f;
        gridManager.startY = lookAt.y;
        gridManager.startZ = lookAt.z - size/ scaling * gridManager.gridSizeZ/2f;
        gridManager.scale = size/ scaling;

    }

    public void AddNear(Vector3 forward)
    {
        eye -= forward * camera.nearClipPlane;
    }
    
    void Awake()
    {
        leftMouseDown = false;
        rightMouseDown = false;
        clickedButton = false;
        camera = GetComponent<Camera>();
        gridManager = GetComponent<GridManager>();
    }

    public void PressedButton()
    {
        clickedButton = true;
        leftMouseDown = false;
    }

	// Update is called once per frame
	void Update()
    {
        if (clickedButton == false)
        {
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl))
                leftMouseDown = true;
            else if (Input.GetMouseButtonUp(0))
                leftMouseDown = false;

            if (Input.GetMouseButtonDown(1))
                rightMouseDown = true;
            else if (Input.GetMouseButtonUp(1))
                rightMouseDown = false;
        }
        else
        {
            clickedButton = false;
        }
        if (leftMouseDown)
            Rotate();
        if (rightMouseDown)
            Move();
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
            Zoom();

    }

    private void Move()
    {
        float h = Input.GetAxis("Mouse X");
        float v = Input.GetAxis("Mouse Y");

        float tmp = Time.deltaTime * moveSpeed * camera.orthographicSize;
        eye = eye - (transform.right * h  + transform.up * v) * tmp;
        lookAt = lookAt - (transform.right * h + transform.up * v) * tmp;
        transform.position = eye;
    }

    private void Rotate()
    {
        float h = Input.GetAxis("Mouse X");
        float v = Input.GetAxis("Mouse Y");

        float tmp = rotateSpeed * Time.deltaTime;

        eye = Quaternion.AngleAxis(h * tmp, transform.up) * (eye - lookAt) + lookAt;
        transform.position = eye;
        transform.LookAt(lookAt, transform.up);

        eye = Quaternion.AngleAxis(v * tmp, transform.right) * (eye - lookAt) + lookAt;
        transform.position = eye;
        Vector3 up = transform.up;
        transform.LookAt(lookAt, transform.up);
    }

    private void Zoom()
    {
        float v = Input.GetAxis("Mouse ScrollWheel");
        camera.orthographicSize = camera.orthographicSize - v * Time.deltaTime * zoomSpeed * camera.orthographicSize;
    }
}
