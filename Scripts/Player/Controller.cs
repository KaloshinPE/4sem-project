using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class Controller : MonoBehaviour {

    public Text countText;
    public Text winText;
	public Rigidbody Player;
	public Platforms_controller PC;
	public Movement motion;

	public bool AI;

    private int count;
    private int win_score;
	private Platform Current_Platform;
	private List<Vector3> Targets = new List<Vector3>();
	private List<Platform> Collisions = new List<Platform>();

    void Start ()
    {
        count = 0;
        winText.text = "";
		win_score = 0;
		Current_Platform = null;
    }

	void Update()
	{
		//Можем включать и отключать ИИ нажатием f1
		if (Input.GetKeyDown (KeyCode.F1)) {
			AI = !AI;
		} 	
		if (Collisions.Count > 0 && (Current_Platform == null || !Collisions.Contains(Current_Platform)))
			Current_Platform = Collisions [0];
		if (Current_Platform.Pick_ups_on_this_platform.Count == 0)
			foreach (Platform elem in Collisions)
				if (elem.Pick_ups_on_this_platform.Count != 0) {
					Current_Platform = elem;
					break;
				}


		PC.Paint_All_In_Default ();
		if (Current_Platform != null) {
			if (Current_Platform.self.gameObject.name != "Ground")
				Current_Platform.self.GetComponent<Renderer> ().material.color = Color.red;
			PC.Paint_List_In_Green (Current_Platform.Platforms_we_can_go_to);
			foreach (Platform elem in Current_Platform.Platforms_we_can_go_to)
				if (elem.Pick_ups_on_this_platform.Count > 0) {
					elem.self.GetComponent<Renderer> ().material.color = Color.yellow;
					break;
				}
		
		}


		if (AI) {
			//Если мы находимся на платформе, на которой можно что-нибудь собрать - собираем
			if (Current_Platform.Pick_ups_on_this_platform.Count > 0) {
				Targets.Clear ();
				foreach (GameObject elem in GameObject.FindGameObjectsWithTag ("Phantom"))
					elem.SetActive (false);
				Collect_Points_On_Current_Platform ();
			} else {
			//Если на данной платформе собирать нечего и дальнейший маршрут не сформирован - формируем
				if (Targets.Count == 0) {
					Vector3 start, destination;
					Platform to_go = Current_Platform.Platforms_we_can_go_to[0];
					foreach (Platform elem in Current_Platform.Platforms_we_can_go_to)
						if (elem.Pick_ups_on_this_platform.Count > 0) {
							to_go = elem;
							break;
						}
					Calculations.How_to_get_to_platform (Current_Platform, to_go, out start, out destination);
					Targets.Add (start);
					Targets.Add (destination);
					(Object.Instantiate (PC.PhantomPrefab, start, Quaternion.identity) as GameObject).GetComponent<Renderer>().material.color = Color.yellow;
					Object.Instantiate (PC.PhantomPrefab, destination, Quaternion.identity);
				}

				if (Targets.Count > 0) {
					if ((Player.transform.position - Targets [0]).magnitude < 0.1) {
						Targets.Remove (Targets [0]);
					}
					else
						motion.Move_to_destination (Targets [0]);
				}
			}
		}
	}

	public void SetWinScore()
	{
		GameObject[] PickUps = GameObject.FindGameObjectsWithTag ("Pick Up"); 
		if(PickUps.Length > win_score)
			win_score = PickUps.Length;
		SetCountText ();
	}
		

    void OnTriggerEnter(Collider other) 
    {
		if (other.gameObject.CompareTag ("Phantom")) {
			other.gameObject.SetActive (false);
		}
		
        if (other.gameObject.CompareTag ( "Pick Up")) {
			PC.Object_Peacked_Up (other.gameObject, Current_Platform);
        }
		if (other.gameObject.CompareTag ("Ground") && (other.gameObject.transform.position.y <= Player.transform.position.y || other.name == "Ground")) {
			Collisions.Add(PC.Find_Platform_by_Game_Object(other.gameObject));
			Targets.Clear ();
		}
    }

	void OnTriggerExit(Collider other)
	{
		Collisions.Remove(PC.Find_Platform_by_Game_Object(other.gameObject));
	}
	//Собрать объекты на текущей платформе. Траэкторию не просчитывает, вызывать через Update
	void Collect_Points_On_Current_Platform()
	{
		if (Current_Platform.Pick_ups_on_this_platform.Count != 0) {
			GameObject nearest = Current_Platform.Pick_ups_on_this_platform [0];
			foreach (GameObject elem in Current_Platform.Pick_ups_on_this_platform) {
				if ((elem.transform.position - Player.transform.position).magnitude < (nearest.transform.position - Player.transform.position).magnitude) {
					nearest = elem;
				}
			}
		
			motion.Move_to_destination (nearest.transform.position);
		} 
	}

	public void IncrScore()
	{
		count += 1;
	}
    

    public void SetCountText ()
    {
		countText.text = "Count: " + count.ToString () + " / " +win_score.ToString(); 
        if (win_score > 0 && count >= win_score)
        {
            winText.text = "You Win!";
        }
    }

}