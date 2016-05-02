using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Platforms_controller : MonoBehaviour {
	public bool test_mode; // Если true, то новые платформы не создаем, а лишь добавляем в список существующие 
	public GameObject PlatformPrefab;
	public GameObject PickUpPrefab;
	public GameObject PhantomPrefab;
	public Controller Score;
	public Movement motion;
	public int NumberOfPlatforms; // Число платформ на сцене
	public bool Platforms_can_cross;

	private List<Platform> Platforms = new List<Platform>(); //Здесь храним список платформ на сцене
	private Color default_color;
	private const float g = 9.81f;

	void Start () {
		if (test_mode) 
			foreach (GameObject elem in GameObject.FindGameObjectsWithTag("Ground")) {
				Platform new_platform = new Platform (elem);
				if (elem.gameObject.name != "Ground")
					new_platform.Generate_pickaple_objects (PickUpPrefab, 1);
				Platforms.Add (new_platform);
		} else {
			foreach (GameObject elem in GameObject.FindGameObjectsWithTag("Ground"))
				if (elem.gameObject.name != "Ground")
					elem.gameObject.SetActive (false);
			Platforms.Add (new Platform (GameObject.Find ("Ground")));
			GenerateScene ();
		}
		Make_Collocations ();
		GameObject plat = GameObject.FindGameObjectWithTag ("Ground");
		default_color = plat.GetComponent<Renderer> ().material.color;
		Sort_platforms_to_go ();
		Score.SetWinScore ();
	}

	//Генерируем сцену. Создаем требуемое чило платформ, добавляем их в список Platforms
	void GenerateScene() {
		for (int i = 0; i < NumberOfPlatforms; i++)  {
			Platform new_platform; 
			if(Platforms_can_cross || Platforms.Count == 0)
				new_platform = GenerateRandomPlatform ();
			else 
				while (true) {
					bool flag = false;
					new_platform = GenerateRandomPlatform ();
					foreach (Platform elem in Platforms)
						if (Calculations.platforms_are_too_close (elem, new_platform)) {
							flag = true;
							break;
						}
					if(!flag)
						break;
					new_platform.DeletePlatform ();
				}
			Platforms.Add (new_platform);
		}
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
		float max_x = Ground.transform.localScale.x / 2;
		float min_x = -max_x;
		float max_z = Ground.transform.localScale.z / 2;
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
					Vector3 start, stop;
					Calculations.How_to_get_to_platform (Start_Platform, Checking, out start, out stop);
					float t = To + Mathf.Sqrt (2 * (Ho - (Checking.self.transform.position.y - Start_Platform.self.transform.position.y)) / g); // Время полета
					float can_go = (motion.moving_force / motion.mass) * Mathf.Pow (t, 2) / 2; // Сколько можем пролететь за t
					//Если успеваем долететь и начальная или конечная точка расположена на крае соответствующей платформы
					if ((stop - start).magnitude < can_go && (Start_Platform.point_is_on_side (start) || Checking.point_is_on_side (stop))) {
						//Между платформами ничего быть не должно
						if (start.y < stop.y && Calculations.ray_collides_smth (start + Vector3.up * 4, stop - Vector3.up, Start_Platform, Checking)) {
							continue;
						}
						if (start.y == stop.y  && Calculations.ray_collides_smth(start + Vector3.up*4, stop + Vector3.up*4, Start_Platform, Checking) )
							continue;
						if (start.y > stop.y) {
							bool flag = false; // будет true, если найдется пересечение
							for (int i = 0; i <= (start.y - stop.y) / 5; i++)
								if (Calculations.ray_collides_smth (start - i*Vector3.up * 5 - Vector3.up, new Vector3(stop.x, (start - i*Vector3.up * 5).y, stop.z) - Vector3.up, Start_Platform, Checking)) {
									flag = true;
									break;
								}
							if (flag)
								continue;
						}
						Start_Platform.Platforms_we_can_go_to.Add (Checking);
					}
				}
			}
		}

		Sort_platforms_to_go ();
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
		

	//В списке Platfroms для каждого элемента сортируем список Platforms_we_can_go_to
	void Sort_platforms_to_go()
	{
		foreach (Platform elem in Platforms)
			//Sort_list_by_length (elem.self.transform.position, elem.Platforms_we_can_go_to);
			sort_list_by_distanse(elem.Platforms_we_can_go_to, elem);
	}

	//Сортируем платформы в порядке возрастания дистанции между ними и исходной
	void sort_list_by_distanse(List<Platform> to_sort, Platform checkpoint) {
		for (int i = 0; i < to_sort.Count - 1; i++) {
			int min = i;
			for (int j = i+1; j < to_sort.Count; j++) {
				Vector3 start, stop;
				Calculations.How_to_get_to_platform (to_sort [j], checkpoint, out start, out stop);
				float dist1 = Calculations.xz_magnitude (start - stop);
				Calculations.How_to_get_to_platform (to_sort [min], checkpoint, out start, out stop);
				float dist2 = Calculations.xz_magnitude (start - stop);
				if (dist1 < dist2)
					min = j;
			}
			Platform x = to_sort [min];
			to_sort [min] = to_sort[i];
			to_sort [i] = x;
		}
	}

	//Сортирует платформы в порядке увеличения расстояния от центра до точки (mark) 
	void Sort_list_by_length(Vector3 mark, List<Platform> to_sort) {
		for (int i = 0; i < to_sort.Count - 1; i++) {
			int min = i;
			for (int j = i+1; j < to_sort.Count; j++) {
				if ((to_sort [j].self.transform.position - mark).magnitude < (to_sort [min].self.transform.position - mark).magnitude)
					min = j;
			}
			Platform x = to_sort [min];
			to_sort [min] = to_sort[i];
			to_sort [i] = x;
		}
	}

	//Поставить на все углы всех платформ по фантому
	void Place_Phantoms_on_each_corner() {
		foreach (Platform elem in Platforms) {

			Vector3 ps1 = elem.self.transform.localScale / 2; ps1.y = 0;
			Vector3 ps1n = ps1; ps1n.x = -ps1n.x;

			Object.Instantiate (PhantomPrefab, elem.Checkpoint_by_corner(ps1), Quaternion.identity);
			Object.Instantiate (PhantomPrefab, elem.Checkpoint_by_corner(-ps1), Quaternion.identity);
			Object.Instantiate (PhantomPrefab, elem.Checkpoint_by_corner(ps1n), Quaternion.identity);
			Object.Instantiate (PhantomPrefab, elem.Checkpoint_by_corner(- ps1n), Quaternion.identity);
		}
	}
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

	public void DeletePlatform() {
		foreach (GameObject elem in Pick_ups_on_this_platform)
			//elem.SetActive (false);
			Object.Destroy(elem);
		//self.SetActive (false);
		Object.Destroy(self);
	}

	public void How_much()
	{
		Debug.Log (self.transform.position);
		Debug.Log (Pick_ups_on_this_platform.Count);
	}
	//Генерируем собираемые объекты на платформе
	public void Generate_pickaple_objects(GameObject PickUpPrefab)
	{
		int number_of_pick_ups = Random.Range (0, (int)(self.transform.localScale.x * self.transform.localScale.y / 4 + 1));  
		Generate_pickaple_objects (PickUpPrefab, number_of_pick_ups);
	}
	//Позволяет сгенерировать определенное число собираемых объектов
	public void Generate_pickaple_objects(GameObject PickUpPrefab, int number_of_pick_ups)
	{
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

	//При проэктировании платформ на xz нам переодически нужно узнать, какой угол первой платформы лежит внутри другой (проэкции на xz)
	//Эта функция вернет вектор, который мы должны прибавить к центру платформы, чтобы в этот угол попасть
	public Vector3 Find_Which_corner_is_in_rect(Platform second) {
		return Find_Which_corner_is_in_rect (second.self.transform.position, second.self.transform.localScale);
	}

	//Проэкция какого угла платформы лежит внутри прямоугольника на xz (нестрогие равенства)
	public Vector3 Find_Which_corner_is_in_rect(Vector3 rect_pos, Vector3 rect_scale) {
		//координаты центров
		Vector3 pp1 = self.transform.position;
		Vector3 pp2 = rect_pos;
		//Масштаб1
		Vector3 ps1 = self.transform.localScale; ps1.y = 0;
		Vector3 ps2 = rect_scale; ps2.y = 0;
		//Для удобства
		Vector3 ps1n = ps1; ps1n.x = -ps1n.x;
		Vector3 ps2n = ps2; ps2n.x = -ps2n.x;

		if (Calculations.check_if_point_is_on_rect (pp2, ps2, pp1 + ps1 / 2)) {
			return ps1 / 2;
		} else if (Calculations.check_if_point_is_on_rect (pp2, ps2, pp1 - ps1 / 2)) {
			return -ps1 / 2;
		} else if (Calculations.check_if_point_is_on_rect (pp2, ps2, pp1 + ps1n / 2)) {
			return ps1n / 2;
		} else if (Calculations.check_if_point_is_on_rect (pp2, ps2, pp1 - ps1n / 2)) {
			return -ps1n / 2;
		}
		return new Vector3 (0, 0, 0);
	}

	// Скармливаем функции направление на нужный угол, она возвращает чекпоинт (точку, в коророй должен оказаться центр шара),
	// расположенный на 0.5 выше центра единичного квадрата, вписанного в угол (завязано на размерах шарика)
	public Vector3 Checkpoint_by_corner (Vector3 corner_direction) {
		Vector3 pp = self.transform.position;
		corner_direction.y = 0;
		Vector3 correction = -corner_direction;
		correction.x = Mathf.Sign (correction.x);
		correction.y = 0;
		correction.z = Mathf.Sign (correction.z);
		correction = correction.normalized / 2;
		correction.y = self.transform.localScale.y / 2 + 0.5f; 
		return pp + corner_direction + correction;
	}

	//Пороверить, находится ли точка в достаточной близости от сторон в проэекции на xz (достаточная близость - close)
	public bool point_is_on_side(Vector3 point) {
		float close = 1f; // Половина диаметра шарика
		Vector3 p = self.transform.position;
		Vector3 s = self.transform.localScale / 2;
		if (!Calculations.check_if_point_is_on_rect (p, s*2, point))
			return false;
		else {
			if (Mathf.Abs (p.x - s.x - point.x) <= close || Mathf.Abs (p.x + s.x - point.x) <= close || Mathf.Abs (p.z - s.z - point.z) <= close || Mathf.Abs (p.z + s.z - point.z) <= close)
				return true;
			return false;
		}
	}
}
//
//Простые вычисления, для которых не нужен полный список платформ

public class Calculations {
	private const float g = 9.81f;
	//Проверить, накладываются ли проекции точки и прямоугольника на xz
	public static bool check_if_point_is_on_rect(Vector3 position, Vector3 scale, Vector3 point){
		if (position.x - scale.x / 2 <= point.x && position.x + scale.x / 2 >= point.x && position.z - scale.z / 2 <= point.z && position.z + scale.z / 2 >= point.z) {
			return true;
		} else {
			return false;
		}
	}

	public static bool platforms_are_too_close(Platform p1, Platform p2) {
		if (p1.self.transform.position.y == p2.self.transform.position.y &&
		    (Mathf.Abs (p1.self.transform.position.x - p2.self.transform.position.x) <= p1.self.transform.localScale.x / 2 + p2.self.transform.localScale.x / 2 + 2f &&
		    Mathf.Abs (p1.self.transform.position.z - p2.self.transform.position.z) <= p1.self.transform.localScale.z / 2 + p2.self.transform.localScale.z / 2 + 2f))
			return true;
		return false;
	}

	//Считаем время, через которое игрок достигнет края платформы, двигаясь с текущей скоростью
	public static float time_befor_falling(Vector3 position, Vector3 velocity, GameObject platform) {
		float dist_x = Mathf.Abs ((platform.transform.position.x + Mathf.Sign(velocity.x) * platform.transform.localScale.x / 2) - position.x);
		float dist_z = Mathf.Abs ((platform.transform.position.z + Mathf.Sign(velocity.z) * platform.transform.localScale.x / 2) - position.z);

		if (Mathf.Abs (dist_x / velocity.x) < Mathf.Abs (dist_z / velocity.z))
			return Mathf.Abs (dist_x / velocity.x);
		else
			return Mathf.Abs (dist_z / velocity.z);
	}

	//Вернет ускорение, которое нужно приложить к телу, чтобы не упасть, т.е. при котором игрок, достигнув края платформы, 
	//будет иметь нулевую составляющую скорости, перпендикулярную краю
	public static Vector3 acceleration_to_prevent_falling(Vector3 position, Vector3 velocity, GameObject platform) {
		float dist_x = Mathf.Abs ((platform.transform.position.x + Mathf.Sign(velocity.x) * platform.transform.localScale.x / 2) - position.x);
		float dist_z = Mathf.Abs ((platform.transform.position.z + Mathf.Sign(velocity.z) * platform.transform.localScale.x / 2) - position.z);

		Vector3 direction = new Vector3 ();
		float distance = 0;
		if (dist_x / Mathf.Abs (velocity.x) < dist_z / Mathf.Abs (velocity.z)) {
			distance = dist_x;
			direction.x = Mathf.Sign (velocity.x);
		} else if (dist_x / Mathf.Abs (velocity.x) > dist_z / Mathf.Abs (velocity.z)) {
			distance = dist_z;
			direction.z = Mathf.Sign (velocity.z);
		} else {
			distance = dist_x;
			direction.x = Mathf.Sign (velocity.x);
			direction.z = Mathf.Sign (velocity.z);
			direction.Normalize ();
		}
		float acceleration = Mathf.Pow (Vector3.Dot (velocity, direction), 2) / distance;
		return -direction * acceleration;
	}

	//За какое время игрок упадет из start в destination
	//-1 - никогда не попадет
	public static float falling_time(Vector3 start, Vector3 destination, Vector3 velocity) {
		float h = destination.y - start.y;
		float v = velocity.y;
		float D = v * v - 2 * g * h;
		if (D < 0)
			return -1;
		return (v + Mathf.Sqrt (D)) / g;
	}

	//За какое время подлетим на заданную высоту
	// -1  - никогда
	public static float rising_time(Vector3 start, Vector3 destination, Vector3 velocity) {
		if(start.y > destination.y)
			return 0;
		float h = destination.y - start.y;
		float v = velocity.y;
		float D = v * v - 2 * g * h;
		if (D < 0)
			return -1;
		return (v - Mathf.Sqrt (D)) / g;
	}

	public static Vector3 find_direction_to_fall(Vector3 position) {
		if (!Calculations.point_above_the_ground (position + Vector3.forward))
			return Vector3.forward;
		if (!Calculations.point_above_the_ground (position + Vector3.back))
			return Vector3.back;
		if (!Calculations.point_above_the_ground (position + Vector3.left))
			return Vector3.left;
		if (!Calculations.point_above_the_ground (position + Vector3.right))
			return Vector3.right;
		return new Vector3 ();
	}

	//Проверить, есть ли непосредственно под точкой какая-нибудь платформа (платформа должна быть не ниже, чем -2 уровня точки)
	public static bool point_above_the_ground(Vector3 point) {
		if(Physics.Raycast(point, Vector3.down, 2))
		{
			return true;
		}
		return false;
	}

	//луч что то задевает, кроме exception
	public static bool ray_collides_smth(Vector3 start, Vector3 stop, Platform exception1, Platform exception2) {
		RaycastHit other;
		Ray ray = new Ray (start, stop - start);
		if (Physics.Raycast (ray, out other, (stop - start).magnitude)) {
			if (other.transform != exception1.self.transform && other.transform != exception2.self.transform)
			return true;
		}
		return false;
	}

	//Длина проэкции вектора на xz
	public static float xz_magnitude(Vector3 p) {
		p.y = 0;
		return p.magnitude;
	}


	//проверка на пересечение проэкций платформ на xz
	public static bool Check_if_platforms_cross(Platform p1, Platform p2){

		//координаты центров
		Vector3 pp1 = p1.self.transform.position; 
		Vector3 pp2 = p2.self.transform.position;
		//Масштаб
		Vector3 ps1 = p1.self.transform.localScale;
		Vector3 ps2 = p2.self.transform.localScale;


		if ((Mathf.Abs (pp1.x - pp2.x) <= (ps1.x + ps2.x) / 2 && Mathf.Abs (pp1.z - pp2.z) <= (ps1.z + ps2.z) / 2))
			return true;
		else
			return false;
	}

	//Работаем со случаем, когда p1 выше p2, их проекции пересекаются, но ни один угол p1 не накладывается на p2
	//Возвращаем чекпоинты  point1 - на верхней, point2 - на нижней
	public static void Chechpoint_for_overlay_platform (Platform p1, Platform p2, out Vector3 point1, out Vector3 point2) {
		//координаты центров
		Vector3 pp1 = p1.self.transform.position; 
		Vector3 pp2 = p2.self.transform.position;
		//Масштаб
		Vector3 ps1 = p1.self.transform.localScale;
		Vector3 ps2 = p2.self.transform.localScale;
		Vector3 ps2x_check = ps2; ps2x_check.z = ps1.z;
		Vector3 ps2z_check = ps2; ps2z_check.x = ps1.x;

		Vector3 ps1x = ps1/2; ps1x.y = 0; ps1x.z = 0; 
		Vector3 ps1z = ps1/2; ps1z.y = 0; ps1z.x = 0;

		Vector3 direction = new Vector3 ();

		if (check_if_point_is_on_rect (pp2, ps2x_check, pp1 + ps1x) && !check_if_point_is_on_rect (pp2, ps2x_check, pp1 - ps1x))
			direction = ps1x;
		else if (!check_if_point_is_on_rect (pp2, ps2x_check, pp1 + ps1x) && check_if_point_is_on_rect (pp2, ps2x_check, pp1 - ps1x))
			direction = -ps1x;
		else if (check_if_point_is_on_rect (pp2, ps2z_check, pp1 + ps1z) && !check_if_point_is_on_rect (pp2, ps2z_check, pp1 - ps1z))
			direction = ps1z;
		else if (!check_if_point_is_on_rect (pp2, ps2z_check, pp1 + ps1z) && check_if_point_is_on_rect (pp2, ps2z_check, pp1 - ps1z))
			direction = -ps1z;
		else if (check_if_point_is_on_rect (pp2, ps2x_check, pp1 + ps1x) && check_if_point_is_on_rect (pp2, ps2x_check, pp1 - ps1x) && ps1.x <= ps2.x)
			direction = ps1x;
		else if (check_if_point_is_on_rect (pp2, ps2z_check, pp1 + ps1z) && check_if_point_is_on_rect (pp2, ps2z_check, pp1 - ps1z) && ps1.z <= ps2.z) {
			direction = ps1z;
	//		Debug.Log ("true" + ps1.z.ToString() + " : " + ps2.z.ToString());
		}

		point1 = pp1 + direction - direction.normalized * 0.1f + new Vector3 (0, ps1.y / 2 + 0.5f, 0);
		point2 = point1 - new Vector3 (0, pp1.y - pp2.y, 0);
	}

	//Возвращает две точки - начало и конец. Передавать должны стартовую платформу, и одну из платформ из списка Plarforms_we_can_go_to
	//Само перемещение будет осуществляться в movement
	public static void How_to_get_to_platform(Platform Start, Platform Destination, out Vector3 start1, out Vector3 destination1) {
		Vector3 start = new Vector3(), destination = new Vector3();
		//Разберем случай пересекающихся платформ
		if (Calculations.Check_if_platforms_cross (Start, Destination)) {
			//Если на одной высоте:
			if (Start.self.transform.position.y == Destination.self.transform.position.y) {
				Find_nearest_points (Start, Destination, out start, out destination);
				if (Calculations.check_if_point_is_on_rect (Destination.self.transform.position, Destination.self.transform.localScale, start))
					destination = start;
				start1 = start; destination1 = destination;
				return;
			}
			//На разных высотах:
			else {
				//стартовая платформа выше
				if (Start.self.transform.position.y > Destination.self.transform.position.y) {
					Vector3 direction = Start.Find_Which_corner_is_in_rect (Destination);
					if (direction.magnitude != 0) {
						start = Start.Checkpoint_by_corner (direction);
						destination = start;
						destination.y -= Start.self.transform.position.y - Destination.self.transform.position.y;
						start1 = start;
						destination1 = destination;
						return;
					} else {
						Chechpoint_for_overlay_platform(Start, Destination, out start, out destination);
						start1 = start;
						destination1 = destination;
						return;
					}
				}

				//стартовая платформа ниже
				else if (Start.self.transform.position.y < Destination.self.transform.position.y) {
					Vector3 direction = Destination.Find_Which_corner_is_in_rect (Start);
					//Если хоть один угол платформы назначения лежит над нашей
					if (direction.magnitude != 0) {
						destination = Destination.Checkpoint_by_corner (direction);
						start = destination;
						start.y -= Destination.self.transform.position.y - Start.self.transform.position.y;
						start1 = start;
						destination1 = destination;
						return;
					} else { // Если ни один угол не лежит
						Chechpoint_for_overlay_platform(Destination, Start, out destination, out start);
						start1 = start;
						destination1 = destination;
						return;
					} 
				}

			}
		}
		//Проэкции платформ не пересекаются
		else {
			Calculations.Find_nearest_points (Start, Destination, out start, out destination);
			start1 = start; destination1 = destination;
			return;
		}
		start1 = start; destination1 = destination;
	}

	//Возвращает ближайшие точки непересекающихся платформ
	public static void Find_nearest_points (Platform Start_platform, Platform Destination_platform, out Vector3 Start1, out Vector3 Stop1) {
		Vector3 Start = new Vector3(), Stop = new Vector3();
		//координаты центров
		Vector3 pp1 = Start_platform.self.transform.position; 
		Vector3 pp2 = Destination_platform.self.transform.position;
		//Масштаб
		Vector3 ps1 = Start_platform.self.transform.localScale;
		Vector3 ps2 = Destination_platform.self.transform.localScale;


		Start.y = pp1.y + ps1.y / 2 + 0.5f;
		Stop.y = pp2.y + ps2.y / 2 + 0.5f;

		//Случай пересечения проэкций на ось х
		if (Mathf.Abs (pp1.x - pp2.x) < ps1.x / 2 + ps2.x / 2) {
			//Расстояние от центра до правой грани платформы
			Vector3 dir1 = ps1/2; dir1.y = 0; dir1.z = 0; 
			Vector3 dir2 = ps2/2; dir2.y = 0; dir2.z = 0;
			//координатой x вектора calc будет середина перекрытия проэкций платформ
			//Vector3 calc = - (pp1 + dir1 - (pp2 - dir2)) *(ps2.x/ps1.x)/(1 + ps2.x/ps1.x);
			Vector3 calc = - (pp1 + dir1 - (pp2 - dir2)) *(ps1.x/ps2.x)/(1 + ps1.x/ps2.x);
			Start.x = calc.x + pp1.x + dir1.x;
			Stop.x = Start.x;

			int invert = 1;
			if (pp2.z < pp1.z)
				invert = -1;
			Start.z = pp1.z + invert*(ps1.z/2 - 0.5f);
			Stop.z = pp2.z - invert*(ps2.z/2 - 0.5f);
			Start1 = Start;
			Stop1 = Stop;
			return;
		}
		//Случай пересечения проэкций на ось z
		if (Mathf.Abs (pp1.z - pp2.z) < ps1.z / 2 + ps2.z / 2) {
			Vector3 dir1 = ps1/2; dir1.y = 0; dir1.x = 0; 
			Vector3 dir2 = ps2/2; dir2.y = 0; dir2.x = 0;
			//координатой z вектора calc будет середина перекрытия проэкций платформ
			//Vector3 calc = - (pp1 + dir1 - (pp2 - dir2)) *(ps2.z/ps1.z)/(1 + ps2.z/ps1.z);
			Vector3 calc = - (pp1 + dir1 - (pp2 - dir2)) *(ps1.z/ps2.z)/(1 + ps1.z/ps2.z);
			Start.z =  calc.z + pp1.z + dir1.z;
			Stop.z = Start.z;

			int invert = 1;
			if (pp2.x < pp1.x)
				invert = -1;

			Start.x = pp1.x + invert*(ps1.x/2 - 0.5f);
			Stop.x = pp2.x - invert*(ps2.x/2 - 0.5f);
			Start1 = Start;
			Stop1 = Stop;
			return;
		}
		//Случай, когда проэкции вообще не пересекаются (ближайшие точки тогда - ближайшие углы)
		//Параметры прямоугольника,"натянутого" на центры платформ
		Vector3 fict_pos = (pp1 + pp2) / 2;
		Vector3 fict_scale = new Vector3 (Mathf.Abs (pp1.x - pp2.x), 0, Mathf.Abs(pp1.z - pp2.z));

		//Точка для 1ой платформы
		Vector3 direction = Start_platform.Find_Which_corner_is_in_rect(fict_pos, fict_scale);
		Start = Start_platform.Checkpoint_by_corner (direction);

		//Точка для второй платформы
		direction = Destination_platform.Find_Which_corner_is_in_rect(fict_pos, fict_scale);
		Stop = Destination_platform.Checkpoint_by_corner (direction);

		Start1 = Start;
		Stop1 = Stop;
	}
}