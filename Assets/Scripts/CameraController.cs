using UnityEngine;

public class CameraController : MonoBehaviour

{
    public float moveSpeed = 10f;

    public Camera cam;
    public float zoomSpeed = 5f;

    public float minZoom = 5f;
    public float maxZoom = 15f;

    public float rotateSpeed = 100f;

    void Update(){
        Move();
        Zoom();
        Rotate();
    }
    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        transform.position +=
            new Vector3(h, 0, v) * moveSpeed * Time.deltaTime;
    }

    void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 pos = cam.transform.localPosition;

        pos.y -= scroll * zoomSpeed;
        pos.z += scroll * zoomSpeed;

        pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);

        cam.transform.localPosition = pos;
    }
    
    void Rotate()
    {
        if(Input.GetKey(KeyCode.Q)){
            transform.Rotate(Vector3.up, - rotateSpeed * Time.deltaTime);
        }
        
        if(Input.GetKey(KeyCode.E)){
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }
    }
}