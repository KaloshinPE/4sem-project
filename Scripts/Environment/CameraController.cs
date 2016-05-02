using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{

    public GameObject player;
	public float rotating_speed;

    private Vector3 offset;
	private Vector3 start_offset;
	private Quaternion rotation;

    void Start()
    {
        offset = transform.position - player.transform.position;
		start_offset = offset;
		rotation = transform.rotation;
    }

    void LateUpdate()
    {
        transform.position = player.transform.position + offset;
		if (Input.GetKey (KeyCode.J)) {
			offset = Quaternion.AngleAxis (rotating_speed, Vector3.up) * offset;
			transform.RotateAround (player.transform.position + offset, Vector3.up, rotating_speed);
		} 
		if (Input.GetKey (KeyCode.L)) {
			offset = Quaternion.AngleAxis (rotating_speed, Vector3.down) * offset;
			transform.RotateAround (player.transform.position + offset, Vector3.down, rotating_speed);
		} 
		if (Input.GetKey (KeyCode.K)) {
			Vector3 axis = offset;
			axis.y = 0;
			float z = axis.z;
			axis.z = axis.x;
			axis.x = -z;
			offset = Quaternion.AngleAxis (rotating_speed, - axis) * offset;
			transform.RotateAround (player.transform.position + offset, - axis, rotating_speed);
		}
		if (Input.GetKey (KeyCode.I)) {
			Vector3 axis = offset;
			axis.y = 0;
			float z = axis.z;
			axis.z = axis.x;
			axis.x = -z;
			offset = Quaternion.AngleAxis (rotating_speed, axis) * offset;
			transform.RotateAround (player.transform.position + offset, axis, rotating_speed);
		}

		if (Input.GetKey (KeyCode.U)) {
			offset *= 0.95f;
		}

		if (Input.GetKey (KeyCode.O)) {
			offset *= 1.05f; 
		}

		if (Input.GetKey (KeyCode.P)) {
			offset = start_offset;
			transform.rotation = rotation;
		}
    }
}