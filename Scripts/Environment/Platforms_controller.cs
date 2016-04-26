using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Platforms_controller : MonoBehaviour {
	public GameObject PlatformPrefab;
	public GameObject PickUpPrefab;
	public Controller Score;
	public Movement motion;
	public int NumberOfPlatforms; // Число платформ на сцене

	private List<Platform> Platforms = new List<Platform>(); //Здесь храним список платформ на сцене
	private Color default_color;
	private const float g = 9.81f;

	void Start () {
		Platforms.Add(new Platform(GameObject.Find("Ground")));
		GenerateScene ();
		Make_Collocations ();
		GameObject plat = GameObject.FindGameObjectWithTag ("Ground");
		default_color = plat.GetComponent<Renderer> ().material.color;
		Score.SetWinScore ();
	}
	


	//Генерируем сцену. Создаем требуемое чило платформ, добавляем их в список Platforms
	void GenerateScene() {
		for (int i = 0; i < NumberOfPlatforms; i++) 
			Platforms.Add (GenerateRandomPlatform ());
	}

	//Генерируем случайную платформу. На пересечения не проверяем, если что - получаются платформы с более сложной геометрией
	Platform GenerateRandomPlatform() {
	//Задаем размеры сцены
		//пол, потолок, шаг (уровневая структура)
		float max_heigh = 30;
		float min_heigh = 5; 
		float heigh_step = 5;

		//Размеры по горизонтали
		GameObject Ground = GameObject.Find("Ground");
		float max_x = Ground.transform.localScale.x * 5;
		float min_x = -max_x;
		float max_z = Ground.transform.localScale.z * 5;
		float min_z = -max_z;

		//Масштаб
		float max_scale = 40;
		float min_scale = 1;

		//Толщина платформы
		float standart_thickness = 1;

		//Генерируем случайные положение и размеры
		float scale_x = Random.Range(min_scale, max_scale);
		float scale_z = Random.Range(min_scale, max_scale);

		float pos_x = Random.Range (min_x, max_x);
		float pos_z = Random.Range (min_z, max_z);
		float pos_y = min_heigh + heigh_step * Random.Range (0, (int)Mathf.Ceil((max_heigh - min_heigh)/heigh_step));

		//Создаем платформу
		Platform New_Platform = new Platform(PlatformPrefab, new Vector3(pos_x, pos_y, pos_z), new Vector3(scale_x, standart_thickness, scale_z));
		New_Platform.Generate_pickaple_objects (PickUpPrefab);
		return New_Platform;
		//return new Platform(PlatformPrefab, new Vector3(pos_x, pos_y, pos_z), new Vector3(scale_x, standart_thickness, scale_z));
	}

	//Функция, в которой будут просчитываться связи между платформами: куда мы можем попасть с данной конкретной плтформы?
	public void Make_Collocations() {
		/*
		 * Работать все будет так: у нас задана начальная вертикальная скорость в прыжке (motion.speed),
		 * следовательно, однозначно определяется время, за которое шар залетит на высоту H (разницу высот между платформами),
		 * минуя высшую точку. За это время приложенная горизонтальная сила (motion.moving_force) должна переместить
		 * шар на расстояние между платформами. Считаем пока, что шар стартует с нулевой скоростью из точки платформы отправления,
		 * ближайшей к платформе назначения
		 */
		float To = motion.jump_start_speed / g;
		float Ho = Mathf.Pow (motion.jump_start_speed, 2) / 2 / g; 
		foreach (Platform Start_Platform in Platforms) {
			foreach (Platform Checking in Platforms) {
				if ((Start_Platform != Checking) && ( Ho - 1 > (Checking.self.transform.position.y - Start_Platform.self.transform.position.y))) {
					
					//Выявляем платформы, проэкции которых накладываются друг на друга
					if (Check_if_platforms_cross (Start_Platform, Checking))
						Start_Platform.Platforms_we_can_go_to.Add (Checking);
					
					// Если не пересекаются, смотрим, можно ли допрыгнуть
					else { 
							float t = To + Mathf.Sqrt (2 * (Ho - (Checking.self.transform.position.y - Start_Platform.self.transform.position.y)) / g); // Время падения из высшей точки траэктории
							float can_go = (motion.moving_force / motion.mass) * Mathf.Pow (t, 2) / 2; // Сколько можем пролететь за t
							Vector3 start, stop;
							Find_nearest_points (Start_Platform, Checking, out start, out stop);
							if ((stop - start).magnitude < can_go)
								Start_Platform.Platforms_we_can_go_to.Add (Checking);
					}
				}
			}
		}
	}

	//проверка на пересечение проэкций платформ на xz
	public bool Check_if_platforms_cross(Platform p1, Platform p2){
		
		//координаты центров
		Vector3 pp1 = p1.self.transform.position; 
		Vector3 pp2 = p2.self.transform.position;
		//Масштаб
		Vector3 ps1 = p1.self.transform.localScale;
		Vector3 ps2 = p2.self.transform.localScale;

		//Костыль. У пола масштаб странный, подгоняем под нормальный
		if(p1.self.gameObject.name == ("Ground"))
			ps1 = ps1*10;
		if(p2.self.gameObject.name == ("Ground"))
			ps2 = ps2*10;
		
		if ((Mathf.Abs (pp1.x - pp2.x) <= (ps1.x + ps2.x) / 2 && Mathf.Abs (pp1.z - pp2.z) <= (ps1.z + ps2.z) / 2))
			return true;
		else
			return false;
	}

	void Find_nearest_points (Platform Start_platform, Platform Destination_plarform, out Vector3 Start1, out Vector3 Stop1) {
		Vector3 Start = new Vector3(), Stop = new Vector3();
		//координаты центров
		Vector3 pp1 = Start_platform.self.transform.position; 
		Vector3 pp2 = Destination_plarform.self.transform.position;
		//Масштаб
		Vector3 ps1 = Start_platform.self.transform.localScale;
		Vector3 ps2 = Destination_plarform.self.transform.localScale;

		//Костыль. У пола масштаб странный, подгоняем под нормальный
		if(Start_platform.self.gameObject.name == ("Ground"))
			ps1 = ps1*10;
		if(Destination_plarform.self.gameObject.name == ("Ground"))
			ps2 = ps2*10;
		
		Start.y = pp1.y + ps1.y / 2 + 0.5f;
		Stop.y = pp2.y + ps2.y / 2 + 0.5f;
		//Случай пересечения проэкций на ось х
		if (Mathf.Abs (pp1.x - pp2.x) < ps1.x / 2 + ps2.x / 2) {
			Start.x = pp1.x + (pp2.x - pp1.x) / 2;
			Stop.x = Start.x;

			int invert = 1;
			if (pp2.z < pp1.z)
				invert = -1;
			Start.z = pp1.z + invert*(ps1.z - 0.5f);
			Stop.z = pp2.z - invert*(ps2.z - 0.5f);
			Start1 = Start;
			Stop1 = Stop;
			return;
		}
		//Случай пересечения проэкций на ось z
		if (Mathf.Abs (pp1.z - pp2.z) < ps1.z / 2 + ps2.z / 2) {
			Start.z = pp1.z + (pp2.z - pp1.z) / 2;
			Stop.z = Start.z;

			int invert = 1;
			if (pp2.x < pp1.x)
				invert = -1;
			Start.x = pp1.x + invert*(ps1.x - 0.5f);
			Stop.x = pp2.x - invert*(ps2.x - 0.5f);
			Start1 = Start;
			Stop1 = Stop;
			return;
		}
		//Случай, когда проэкции вообще не пересекаются (ближайшие точки тогда - ближайшие углы)
		Vector3 fict_pos = (pp1 + pp2) / 2;
		Vector3 fict_scale = new Vector3 (Mathf.Abs (pp1.x - pp2.x), 0, Mathf.Abs(pp1.z - pp2.z));

		Vector3 ps1n = ps1; ps1n.x = -ps1n.x;
		Vector3 ps2n = ps2; ps2n.x = -ps2n.x;
		//Точка для 1ой платформы
		if(check_if_point_is_on_rect( fict_pos, fict_scale, pp1 + ps1/2 ))
		{
			Start.x = pp1.x + ps1.x / 2 - 0.5f;
			Start.z = pp1.z + ps1.z / 2 - 0.5f;
		}
		else if(check_if_point_is_on_rect( fict_pos, fict_scale, pp1 - ps1/2 ))
		{
			Start.x = pp1.x - ps1.x / 2 + 0.5f;
			Start.z = pp1.z - ps1.z / 2 + 0.5f;
		}
		else if(check_if_point_is_on_rect( fict_pos, fict_scale, pp1 + ps1n/2 ))
		{
			Start.x = pp1.x + ps1n.x / 2 + 0.5f;
			Start.z = pp1.z + ps1n.z / 2 - 0.5f;
		}
		else if(check_if_point_is_on_rect( fict_pos, fict_scale, pp1 - ps1n/2 ))
		{
			Start.x = pp1.x - ps1n.x / 2 - 0.5f;
			Start.z = pp1.z - ps1n.z / 2 + 0.5f;
		}
		//Точка для второй платформы
		if(check_if_point_is_on_rect( fict_pos, fict_scale, pp2 + ps2/2 ))
		{
			Stop.x = pp2.x + ps2.x / 2 - 0.5f;
			Stop.z = pp2.z + ps2.z / 2 - 0.5f;
		}
		else if(check_if_point_is_on_rect( fict_pos, fict_scale, pp2 - ps2/2 ))
		{
			Stop.x = pp2.x - ps2.x / 2 + 0.5f;
			Stop.z = pp2.z - ps2.z / 2 + 0.5f;
		}
		else if(check_if_point_is_on_rect( fict_pos, fict_scale, pp2 + ps2n/2 ))
		{
			Stop.x = pp2.x + ps2n.x / 2 + 0.5f;
			Stop.z = pp2.z + ps2n.z / 2 - 0.5f;
		}
		else if(check_if_point_is_on_rect( fict_pos, fict_scale, pp2 - ps2n/2 ))
		{
			Stop.x = pp2.x - ps2n.x / 2 - 0.5f;
			Stop.z = pp2.z - ps2n.z / 2 + 0.5f;
		}
		Start1 = Start;
		Stop1 = Stop;
	}


	public Platform Find_Platform_by_Game_Object(GameObject ToFind) {
		foreach (Platform elem in Platforms) {
			if (elem.self == ToFind) {
				return elem;
			}
		}
		return null;
	}
				

	public void Object_Peacked_Up(GameObject Object_Peacked_Up, Platform Current_Platform) {
		if (Current_Platform.Pick_ups_on_this_platform.Contains (Object_Peacked_Up)) {
			Current_Platform.Pick_ups_on_this_platform.Remove (Object_Peacked_Up);
			Object_Peacked_Up.SetActive (false);
			Score.IncrScore ();
			Score.SetCountText ();
		} else
			foreach (Platform elem in Platforms) {
				if (elem.Pick_ups_on_this_platform.Contains (Object_Peacked_Up)) {
					elem.Pick_ups_on_this_platform.Remove (Object_Peacked_Up);
					Object_Peacked_Up.SetActive (false);
					Score.IncrScore ();
					Score.SetCountText ();
				}
			}
	}

	//Проверяем, лежит ли точка в прямоугольнике (проэкции на плоскость xz)
	public bool check_if_point_is_on_rect(float xpos, float zpos, float xscale, float zscale, float xpoint, float zpoint){
		if (xpos - xscale / 2 < xpoint && xpos + xscale / 2 > xpoint && zpos - zscale / 2 < zpoint && zpos + zscale / 2 > zpoint) {
			return true;
		} else {
			return false;
		}
	}

	public bool check_if_point_is_on_rect(Vector3 position, Vector3 scale, Vector3 point){
		if (position.x - scale.x / 2 < point.x && position.x + scale.x / 2 > point.x && position.z - scale.z / 2 < point.z && position.z + scale.z / 2 > point.z) {
			return true;
		} else {
			return false;
		}
	}
	//Красим все платформы в стандартный цвет
	public void Paint_All_In_Default()
	{
		foreach (Platform elem in Platforms) {
			elem.self.GetComponent<Renderer> ().material.color = default_color;
		}
	}
	// Окрасить все платформы из List в зеленый
	public void Paint_List_In_Green(List<Platform> to_paint) {
		foreach (Platform elem in to_paint)
			elem.self.GetComponent<Renderer> ().material.color = Color.green;
	}

	//Возвращает две точки - начало и конец. Передавать должны стартовую платформу, и одну из платформ из списка Plarforms_we_can_go_to
	//Само перемещение будет осуществляться в movement
	/*public void How_to_go_to_platform(Platform Start, Platform Destination, out Vector3 start, out Vector3 destination) {
		
	//Разберем случай пересекающихся платформ
		if(Check_if_platforms_cross(Start, Destination)) {
			//Если на одной высоте:
			if (Start.self.transform.position.y == Destination.self.transform.position.y) {
				Find_nearest_points (Start, Destination, out start, out destination);
				if (check_if_point_is_on_rect (Destination.self.transform.position, Destination.self.transform.localScale, destination))
					destination = start;
			}
			//На разных высотах:
			else {
				if(Start.self.transform.position.y > Des
			}
		}
	}*/
}
	
// Класс, объектом которого будет являться каждая платформа. В экземпляре хранится информация о собираемых объектах, 
// доступных с этой платформы и о других платформах, куда можно добраться с этой 
public class Platform {
	public GameObject self; 
	public List<Platform> Platforms_we_can_go_to = new List<Platform>(); // Список платформ, на которые мы можем перепрыгнуть
	public List<GameObject> Pick_ups_on_this_platform = new List<GameObject>(); // Собираемые объекты, доступные с этой платформы 

	public Platform (GameObject PlatformPrefab, Vector3 position, Vector3 Scale)
	{
		self = Object.Instantiate (PlatformPrefab, position, Quaternion.identity) as GameObject;
		self.transform.localScale = Scale;
	}

	public Platform(GameObject new_platform)
	{
		self = new_platform;
	}

	public void How_much()
	{
		Debug.Log (self.transform.position);
		Debug.Log (Pick_ups_on_this_platform.Count);
	}

	public void Generate_pickaple_objects(GameObject PickUpPrefab)
	{
		int number_of_pick_ups = Random.Range (0, (int)(self.transform.localScale.x * self.transform.localScale.y / 2 + 1));  
		float xMin = self.transform.position.x - self.transform.localScale.x/2;
		float zMin = self.transform.position.z - self.transform.localScale.z/2; 
		float xMax = self.transform.position.x + self.transform.localScale.x/2;
		float zMax = self.transform.position.z + self.transform.localScale.z/2;
		for (int i = 0; i < number_of_pick_ups; i++) {
			float pos_x = Random.Range (xMin, xMax);
			float pos_z = Random.Range (zMin, zMax);
			float pos_y = (float)(self.transform.position.y + self.transform.localScale.y / 2 + 0.5);
			Pick_ups_on_this_platform.Add(Object.Instantiate (PickUpPrefab, new Vector3(pos_x, pos_y, pos_z), Quaternion.identity) as GameObject);
		}
	}
		
}

