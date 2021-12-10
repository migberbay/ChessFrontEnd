using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool ownedByPlayer;
    
    public void MoveToPosition(Vector3 destination){
        transform.position = new Vector3(destination.x, transform.position.y,destination.z);
    }
}
