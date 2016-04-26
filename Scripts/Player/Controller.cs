using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    void Start ()
    {
        count = 0;
        winText.text = "";
		win_score = 0;
    }

	void Update()
	{
//		if(AI)
			Collect_Points_On_Current_Platform ();
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
		
        if (other.gameObject.CompareTag ( "Pick Up")) {
			PC.Object_Peacked_Up (other.gameObject, Current_Platform);
        }
		if (other.gameObject.CompareTag ("Ground") && other.gameObject.transform.position.y < Player.transform.position.y) {
			Current_Platform = PC.Find_Platform_by_Game_Object(other.gameObject);
			PC.Paint_All_In_Default ();
			if(Current_Platform.self.gameObject.name != "Ground")
				Current_Platform.self.GetComponent<Renderer> ().material.color = Color.red;
			PC.Paint_List_In_Green(Current_Platform.Platforms_we_can_go_to);
		}
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
		
			motion.MoveToPoint (nearest.transform.position);
		} /*else if (Current_Platform.Platforms_we_can_go_to.Count != 0) {
			foreach (Platform elem in Current_Platform.Platforms_we_can_go_to) {
				if (elem.Pick_ups_on_this_platform.Count != 0) {
					motion.MoveToPoint (elem.self.transform.position);
					break;
				}
			}
		}*/
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