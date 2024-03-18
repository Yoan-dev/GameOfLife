using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float Speed = 1f;
    public float ZoomSpeed = 1f;
    public float SpeedMultiplier = 10f;

    private void Update()
    {
		Vector3 velocity = new Vector3();

		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
		{
			velocity.y += 1f;
		}
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
		{
			velocity.x -= 1f;
		}
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
		{
			velocity.y -= 1f;
		}
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
		{
			velocity.x += 1f;
		}

		transform.Translate(velocity * (Input.GetKey(KeyCode.LeftShift) ? SpeedMultiplier * Speed : Speed));

		Camera.main.orthographicSize -= Input.mouseScrollDelta.y * (Input.GetKey(KeyCode.LeftShift) ? SpeedMultiplier * ZoomSpeed : ZoomSpeed);
	}
}
