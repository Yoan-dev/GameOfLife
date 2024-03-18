using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float Speed = 1f;
    public float ZoomSpeed = 1f;
    public float SpeedMultiplier = 10f;
	public float MinZoom = 21f;

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

		float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? SpeedMultiplier : 1;

		transform.Translate(velocity * Speed * speedMultiplier);

		Camera.main.orthographicSize = Mathf.Max(MinZoom, Camera.main.orthographicSize - Input.mouseScrollDelta.y * ZoomSpeed * speedMultiplier);
	}
}
