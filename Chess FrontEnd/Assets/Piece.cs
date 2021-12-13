using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool ownedByPlayer;
    public string pieceType;
    
    private void Start() {
        if(gameObject.name.StartsWith("P"))
            pieceType = "pawn";
        
        if(gameObject.name.StartsWith("T"))
            pieceType = "rook";

        if(gameObject.name.StartsWith("C"))
            pieceType = "knight";

        if(gameObject.name.StartsWith("A"))
            pieceType = "bishop";

        if(gameObject.name.EndsWith("Q"))
            pieceType = "queen";
        
        if(gameObject.name.EndsWith("K"))
            pieceType = "king";
    }

    public void MoveToPosition(Vector2 destination){
        transform.position = new Vector3(destination.x, transform.position.y, destination.y);
    }
}
