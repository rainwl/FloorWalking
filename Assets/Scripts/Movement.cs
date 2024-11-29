using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        var moveZ = Input.GetAxis("Vertical");
        var moveX = Input.GetAxis("Horizontal");
        var moveDirection = new Vector3(moveX, 0, moveZ);
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }
}