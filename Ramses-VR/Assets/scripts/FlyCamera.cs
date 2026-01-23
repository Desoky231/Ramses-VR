using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float lookSpeed = 2f;

    float yaw = 0f;
    float pitch = 0f;

    void Update()
    {
        // Look rotation
        if (Input.GetMouseButton(1)) // Right mouse held
        {
            yaw += lookSpeed * Input.GetAxis("Mouse X");
            pitch -= lookSpeed * Input.GetAxis("Mouse Y");
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        // Movement
        Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (Input.GetKey(KeyCode.E)) dir.y += 1;
        if (Input.GetKey(KeyCode.Q)) dir.y -= 1;
        transform.Translate(dir * movementSpeed * Time.deltaTime, Space.Self);
    }
}
