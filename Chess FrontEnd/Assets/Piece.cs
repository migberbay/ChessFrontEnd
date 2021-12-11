using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool ownedByPlayer;
    
    public void MoveToPosition(Vector2 destination){
        Debug.Log("piece recieved: " + destination.ToString());
        transform.position = new Vector3(destination.x, transform.position.y, destination.y);
    }
}
