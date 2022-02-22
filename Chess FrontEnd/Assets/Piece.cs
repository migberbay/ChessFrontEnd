using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool ownedByPlayer, isSelected = false, pieceIsMoving = false, is_dead = false;
    public string pieceType;
    public Outline outline;
    
    public void Start() {
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

        try{
            outline = GetComponent<Outline>();
            outline.enabled = false;
        }catch{

        }
    }

    public void MoveToPosition(Vector2 destination){
        var origin = transform.position;
        var dest = new Vector3(destination.x, transform.position.y, destination.y);
        StartCoroutine(MovePiece(origin, dest));
    }
    
    IEnumerator MovePiece(Vector3 origin, Vector3 destination){
        float maxTime = 0.75f, currentTime = 0;
        pieceIsMoving = true;
        while(maxTime > currentTime){
            var pos = Vector3.Lerp(origin, destination, currentTime/maxTime);
            transform.position = pos;
            currentTime += Time.deltaTime;
            // Debug.Log("movement hapening: "+ currentTime.ToString());
            yield return null;
        }
        transform.position = destination;
        pieceIsMoving = false;
    }

    private void OnMouseOver() {
        if(outline != null && ownedByPlayer)
            outline.enabled = true;
    }

    private void OnMouseExit() {
        if(outline != null && !isSelected)
            outline.enabled = false;
    }

    public void Unselect(){
        outline.enabled = false;
        isSelected = false;
    }
}
