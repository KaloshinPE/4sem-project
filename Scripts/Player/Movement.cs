using UnityEngine;
using System.Collections;

public class Movement : MonoBehaviour {

    public float moving_force; // сила, приклабываемая к шару чтобы двигать его с места
    public float jump_start_speed; //Скорость сразу после отрыва от земли
    public bool keyboard_control; //Можем ли мы управлять шаром с клавиатуры?
	public float mass;

    private Rigidbody rb;
    private int JumpEnable = 0; 
    private float timer = 0;
    
	void Start ()
    {
        rb = GetComponent<Rigidbody>();
		mass = rb.mass;
    }
    
    void Update()
    {
        timer += Time.deltaTime;

    }
    
	void FixedUpdate ()
    {
		if (keyboard_control) {
			float moveHorizontal = Input.GetAxisRaw ("Horizontal");
        
			float moveVertical = Input.GetAxisRaw ("Vertical");
        
			Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVertical);

			rb.AddForce (movement * moving_force);
        
			if (Input.GetKeyDown ("space")) {
				Jump();
			} 
		}
		Debug.Log ((rb.transform.position.y - 0.5f).ToString () + ":" + (Mathf.Pow (jump_start_speed, 2) / 2 / 9.81).ToString ());
    }

	void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag("Ground"))
			JumpEnable++;
	}

	void OnCollisionExit(Collision collision)
	{
		if (collision.gameObject.CompareTag ("Ground"))
			JumpEnable--;
	}

    // Переместиться в определенную точку и остановиться там. движемся по прямой, препятствия не учитываются. 
    // Рассчитывает требуемое приложение силы на каждой итерации (вызываться, соответственно, должно из Update())
    public void MoveToPoint(Vector3 destination)
    {
        
        Vector3 Direction = destination - rb.position;
		if (Direction.y > 1  && Direction.y <= 20 && Mathf.Sqrt(Mathf.Pow (Direction.x, 2) + Mathf.Pow (Direction.z, 2)) < 0.1)
			Jump ();
		Vector3 Normal = new Vector3 (Direction.z, 0, -Direction.x);
		Normal = Normal.normalized;
		Direction.y = 0;
		Vector3 Velocity = rb.velocity;
		Velocity.y = 0;
		float Vel_tangent = Vector3.Dot (Velocity, Direction.normalized);
		float Vel_normal = Vector3.Dot (Normal, Velocity);
		float predicted_time = Mathf.Abs (Direction.magnitude / Vel_tangent);
		float Needed_normal_acceleration = 2 * Vel_normal / predicted_time;
		//Debug.Log (Vel_tangent.ToString() + " : " + Vel_normal.ToString());

		if (Mathf.Abs (Needed_normal_acceleration) >= moving_force && Direction.magnitude >=1) {
			rb.AddForce (-Normal * moving_force);
		}
		else { 
			float Tangent_acceleration = Mathf.Sqrt (Mathf.Pow (moving_force, 2) - Mathf.Pow (Needed_normal_acceleration, 2));
			if (Tangent_acceleration * predicted_time >= Vel_tangent) {
				rb.AddForce (-Normal * Needed_normal_acceleration + Direction.normalized * Tangent_acceleration);
			} else {
				rb.AddForce (-Normal * Needed_normal_acceleration - Direction.normalized * Tangent_acceleration);
			}
		}
    }
    
    //Тормозим в точке, в которой находимся на данный момент
    public void Stop()
    {
        Vector3 velocity = rb.velocity;
        rb.AddForce(-velocity.normalized*moving_force);
    }

	public void Jump()
	{
		if (JumpEnable>0) {
			rb.AddForce (Vector3.up * jump_start_speed*rb.mass, ForceMode.Impulse);
			//JumpEnable--;
		}
		
	}
}
