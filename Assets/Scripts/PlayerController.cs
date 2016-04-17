using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour {

    public float speed;
    public Text countText;
    public Text winText;
    public float jump_high;

    private Rigidbody rb;
    private int count;

    void Start ()
    {
        rb = GetComponent<Rigidbody>();
        count = 0;
        SetCountText ();
        winText.text = "";
    }

    void FixedUpdate ()
    {
        Vector3 pos = rb.position;
        float moveHorizontal = Input.GetAxis ("Horizontal");
        float moveVertical = Input.GetAxis ("Vertical");
        float jump = Input.GetAxis("Jump");
        
        Vector3 movement = new Vector3 (moveHorizontal, jump*jump_high, moveVertical);

        rb.AddForce (movement * speed);
        SetPosition(pos);
    }

    void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.CompareTag ( "Pick Up"))
        {
            other.gameObject.SetActive (false);
            count = count + 1;
            SetCountText ();
        }
    }

    void SetCountText ()
    {
        countText.text = "Count: " + count.ToString (); 
        if (count >= 9)
        {
            winText.text = "You Win!";
        }
    }
    void SetPosition(Vector3 pos)
    {
        winText.text = "x = " + pos.x.ToString() + "\ny = " + pos.y.ToString() + "\nz = " + pos.z.ToString();
    }
}