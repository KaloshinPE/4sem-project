using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Movement : MonoBehaviour {

    public float moving_force; // сила, приклабываемая к шару чтобы двигать его с места
    public float jump_start_speed; //Скорость сразу после отрыва от земли
    public bool keyboard_control; //Можем ли мы управлять шаром с клавиатуры?
	public float mass;

    private Rigidbody rb;
	private List<GameObject> Collisions = new List<GameObject>();
	private const float g = 9.81f;
	private float timer;
	private bool motion_freeze = false;
	private float time_to_freeze = 0;
    
	void Start ()
    {
        rb = GetComponent<Rigidbody>();
		mass = rb.mass;
		timer = 0;
    }
	void Update() 
	{
		timer += Time.deltaTime;
	}

    
	void FixedUpdate ()
    {
		if (Input.GetKeyDown (KeyCode.F2))
			jump_start_speed *= 2.5f;
		if (Input.GetKeyDown (KeyCode.F3))
			jump_start_speed /= 2.5f;
		if (Input.GetKeyDown (KeyCode.F4))
			Cheat_stop ();
		if (keyboard_control) {
			float moveHorizontal = Input.GetAxisRaw ("Horizontal");
        
			float moveVertical = Input.GetAxisRaw ("Vertical");
        
			Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVertical);

			rb.AddForce (movement * moving_force);
        
			if (Input.GetKeyDown ("space")) {
				Jump();
			} 
		}
    }

	void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag ("Ground") && rb.transform.position.y >= collision.gameObject.transform.position.y + 0.9f)
			Collisions.Add (collision.gameObject);
	}

	void OnCollisionExit(Collision collision)
	{
		if (collision.gameObject.CompareTag ("Ground") && rb.transform.position.y >= collision.gameObject.transform.position.y + 0.9f) { 
			Collisions.Remove (collision.gameObject);
		}
		
	}

    // Переместиться в определенную точку и остановиться там. движемся по прямой, препятствия не учитываются. 
    // Рассчитывает требуемое приложение силы на каждой итерации (вызываться, соответственно, должно из Update())
    public void MoveToPoint(Vector3 destination)
    {
        
        Vector3 Direction = destination - rb.position;
		Vector3 Normal = new Vector3 (Direction.z, 0, -Direction.x);
		Normal = Normal.normalized;
		Direction.y = 0;
		Vector3 Velocity = rb.velocity;
		Velocity.y = 0;
		float Vel_tangent = Vector3.Dot (Velocity, Direction.normalized);
		float Vel_normal = Vector3.Dot (Normal, Velocity);
		float predicted_time = Mathf.Abs (Direction.magnitude / Vel_tangent);
		float Needed_normal_acceleration = 2 * Vel_normal / predicted_time;

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
	//Добраться до цели за время t. если t = 0, то время выбирается так, чтобы скорость по прибытию была нулевой
	// если t = -1, то добираемся настолько быстро, настолько возможно
	// prevent_falling - если мы находимся на платформе, то не можем с нее упасть
	public void move_in_time(Vector3 destination, float t, bool prevent_falling)
	{
		Vector3 direction = destination - rb.position;
		float distance = Calculations.xz_magnitude (direction); //Дистанция до цели в проэкции на xz
		direction.Normalize();
		Vector3 normal = new Vector3 (direction.z, 0, -direction.x);
		normal.Normalize ();

		Vector3 velocity = rb.velocity;
		bool harry_up = false;
		bool time_limited = false;
		if (t == -1)
			harry_up = true;
		if (t > 0)
			time_limited = true;

		velocity.y = 0;
		direction.y = 0;
		direction.Normalize ();
		float Vel_tangent = Vector3.Dot (velocity, direction);
		float Vel_normal = Vector3.Dot (velocity, normal);
		if (t <= 0 )
			t = Mathf.Abs(distance / Vel_tangent);
		float Needed_normal_acceleration = 2 * Vel_normal / t;
		Vector3 prevent_falling_force = new Vector3();
		if (Collisions.Count > 0  && prevent_falling) {
			prevent_falling_force = mass * Calculations.acceleration_to_prevent_falling (rb.position, velocity, Collisions [0]);
		}
		if (prevent_falling_force.magnitude > moving_force)
			prevent_falling_force = prevent_falling_force.normalized * moving_force;
		float moving_force_rest = moving_force - prevent_falling_force.magnitude;

		if (Mathf.Abs (Needed_normal_acceleration) >= moving_force_rest/mass && distance >= 1) {
			rb.AddForce (-normal * Mathf.Sign(Vel_normal) * moving_force_rest + prevent_falling_force);
		} else { 
			float Tangent_acceleration = Mathf.Sqrt (Mathf.Pow (moving_force_rest/mass, 2) - Mathf.Pow (Needed_normal_acceleration, 2));
			if (Mathf.Abs(Tangent_acceleration * distance/Vel_tangent) >= Vel_tangent || harry_up || (Vel_tangent*t < distance && time_limited)) {
				rb.AddForce (mass*(-normal * Needed_normal_acceleration + direction * Tangent_acceleration) + prevent_falling_force);
			} else {
				rb.AddForce (mass*(-normal * Needed_normal_acceleration - direction * Tangent_acceleration) + prevent_falling_force);
			}
		}
		return;
	}	


	public void Move_to_destination(Vector3 destination) {
		//print(Collisions.Count);
		//Задаем направление на цель и нормаль к нему
		if (motion_freeze) {
			if (timer >= time_to_freeze) {
				time_to_freeze = 0;
				motion_freeze = false;
			}
			return;
		}
		Vector3 direction = destination - rb.position;
		float distance = Calculations.xz_magnitude (direction); //Дистанция до цели в проэкции на xz
		direction.Normalize();
		Vector3 normal = new Vector3 (direction.z, 0, -direction.x);
		normal.Normalize ();
		Vector3 velocity = rb.velocity;

		//В скобках описана ситуация, когда пункт назначения на платформе прямо над нами или прямо под нами
		if (!(distance < 1 && Mathf.Abs(rb.position.y - destination.y) > 1)) {
			//Определяем, нужно ли нам прыгать
			//Нужно, если мы в контакте с землей и если непосредственно перед нами по направлению на цель 
			//платформа кончается и до цели в падении долететь мы не успеем

			//Если мы на земле и видим пропасть впереди
			if (Collisions.Count > 0 && !Calculations.point_above_the_ground (rb.position + new Vector3 (direction.x, 0, direction.z).normalized) && distance > 1) {
				//Если цель ниже
				if (direction.y < 0) {
					float t = Mathf.Sqrt (2 / g * Mathf.Abs (rb.position.y - destination.y)); // Время падения
					//Если не долетаем за время падения
					if (Vector3.Dot (velocity, direction) * t + moving_force / mass * Mathf.Pow (t, 2) / 2 < Calculations.xz_magnitude (destination - rb.position))
						Jump ();
					move_in_time (destination, 0, false);
				} else //Если цель выше или на одном уровне с текущим местоположением
					Jump ();
				return;
			}
			//если мы на земле (аналог move, но аккуратнее с краями платформы)
			if (Collisions.Count > 0) {
					move_in_time (destination, 0, true);
					return;
			} else { //Если в воздухе
				//Если игрок выше цели 
				if (rb.transform.position.y > destination.y) {
					move_in_time (destination, Calculations.falling_time (rb.position, destination, velocity), false);
					return;
				} else {
					//Если ниже	
					direction.y = 0;
					move_in_time (destination - direction.normalized * Mathf.Sqrt (2), Calculations.rising_time (rb.position, destination, velocity), true);  
					return;
				}
			}
		} else { //Если дистанция в проэкции на xz < 1
			// Если мы выше пункта назначения сваливаемся где можем
			if (rb.position.y > destination.y) {
				if (Collisions.Count > 0) {
					Vector3 to_fall = Calculations.find_direction_to_fall (rb.position);
					move_in_time (rb.position + to_fall, -1, false);
					if (rb.position.y > destination.y + 1) {
						print ("freeze");
						freeze_motion (0.2f);
					}
				}
				else move_in_time (destination, 0, false); // Если мы уже летим - ниоткуда падать не надо
			} else { // если ниже - огибаем верхнюю платформу
				Vector3 to_fall = Calculations.find_direction_to_fall(destination);
				if (Collisions.Count > 0) {
					if (Calculations.xz_magnitude (destination - rb.position) >= 0.1) {
						Vector3 point = destination;
						point.y = rb.position.y;
						move_in_time (point, 0, true);
						return;
					}
				}
				move_in_time (destination + to_fall, -1, false);
				if (Collisions.Count > 0)
					Jump ();
			}
		}
	}
    
	//Не пересчитывать движение в течение времени t
	void freeze_motion(float t) {
		timer = 0;
		motion_freeze = true;
		time_to_freeze = t;
	}
    //Тормозим в точке, в которой находимся на данный момент
    public void Stop()
    {
        Vector3 velocity = rb.velocity;
        rb.AddForce(-velocity.normalized*moving_force);
    }
	//Мгновенная остановка
	public void Cheat_stop() {
		rb.AddForce (-rb.velocity * mass, ForceMode.Impulse);
	}

	public void Jump()
	{
		if (Collisions.Count > 0) {
			rb.AddForce (Vector3.up * jump_start_speed*rb.mass, ForceMode.Impulse);
			Collisions.Clear ();
		}
		
	}
}
