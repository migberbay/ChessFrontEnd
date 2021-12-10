using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;

public class ChessManager : MonoBehaviour
{
    // Black Pieces
    public Transform pa7, pb7, pc7, pd7, pe7, pf7, pg7, ph7, ta8, cb8, ac8, Kn, Qn, af8, cg8, th8;

    // White Pieces
    public Transform pa2, pb2, pc2, pd2, pe2, pf2, pg2, ph2, ta1, cb1, ac1, Kw, Qw, af1, cg1, th1;

    public Transform selectedPiece;

    public Transform[,] _board =  new Transform[8,8];

    public string[,] squareNames = new string[8,8]{
    { "a1","a2","a3","a4","a5","a6","a7","a8"},
    { "b1","b2","b3","b4","b5","b6","b7","b8"},
    { "c1","c2","c3","c4","c5","c6","c7","c8"},
    { "d1","d2","d3","d4","d5","d6","d7","d8"},
    { "e1","e2","e3","e4","e5","e6","e7","e8"},
    { "f1","f2","f3","f4","f5","f6","f7","f8"},
    { "g1","g2","g3","g4","g5","g6","g7","g8"},
    { "h1","h2","h3","h4","h5","h6","h7","h8"}};

    public GameObject[,] _highlightSquares = new GameObject[8,8];

    public bool playingWhite = false, playingBlack = false, playerTurn = false;
    public GameObject MCWhite, MCBlack, highlightSquarePrefab, highlightSquareCont;
    private Camera active;
    private List<string> stringmoves;

    private string url = "http://127.0.0.1:8000", gameKey = "";

    // si no tienes movimientos y estas en jaque es jaque mate
    // si no tienes movimientos y no estas en jaque es tablas

    void Start()
    {
        InitializeBoard();
        InitializeSystem();
        try{
            InitializeGameOnServer();
        }catch{
            Debug.Log("Server is not online!");
        }
        GenerateHighlightSquareArray();
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop(){
        if(playerTurn){
            bool moveSelected = false;
            while(!moveSelected){
                PlayerSelectionFrameRules();
                yield return null;
            }
        } // if is not player turn.
    }


    private void PlayerSelectionFrameRules(){
        if(Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            Ray ray = active.ScreenPointToRay(Input.mousePosition);
            
            if(selectedPiece == null){ // Piece Selection
                int layer_mask = LayerMask.GetMask("Pieces");
                Physics.Raycast(ray,out hit,30f,layer_mask);
                if(hit.point != new Vector3(0,0,0)){
                    Debug.Log("we hit a piece");
                    selectedPiece = hit.transform;
                    selectedPiece.GetComponent<Outline>().OutlineColor = Color.red;
                    var pieceSquare = squareNames[Mathf.RoundToInt(selectedPiece.transform.position.x), Mathf.RoundToInt(selectedPiece.transform.position.z)];
                    List<string> applicable_moves = new List<string>(stringmoves.FindAll(x => x.Substring(0,2) == pieceSquare));
                    
                    int i = 0, j = 0;
                    foreach (var a in squareNames){
                        foreach (var move in applicable_moves){
                            if(move.Substring(2,2) == a){
                                Debug.Log(move.Substring(2,2) +" = " +a +"?");
                                _highlightSquares[i,j].SetActive(true);
                            }
                        }
                        
                        j++;
                        if(j == 8){
                            i++;
                            j = 0;
                        }
                    }

                }else{
                    Debug.Log("we hit nothing");
                }
            }else{ // Move Piece
                int layer_mask = LayerMask.GetMask("Board");
            }
        }

        if(Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)){
            if(selectedPiece != null){
                selectedPiece.gameObject.GetComponent<Outline>().OutlineColor = Color.green;
                ResetHighLightSquares();
            }
            selectedPiece = null;
        }
    }

    private void ResetHighLightSquares(){
        foreach (var s in _highlightSquares)
        {
            s.SetActive(false);
        }
    }

    private void InitializeSystem(){
        if(UnityEngine.Random.Range(0,2)==0){
            active = MCWhite.GetComponent<Camera>();
            playingWhite = true;    
            MCWhite.SetActive(true);
            foreach (var piece in GetAllPiecesAsArray("w"))
            {
                var outline = piece.gameObject.AddComponent<Outline>();
                var p = piece.gameObject.AddComponent<Piece>();
                outline.OutlineColor = Color.green;
                outline.OutlineWidth = 7.5f;
                p.ownedByPlayer = true;
            }
            foreach (var piece in GetAllPiecesAsArray("b"))
            {
                var p = piece.gameObject.AddComponent<Piece>();
                p.ownedByPlayer = false;
            }
            playerTurn = true;
        }else{
            active = MCBlack.GetComponent<Camera>();
            playingBlack = true;
            MCBlack.SetActive(true);
            foreach (var piece in GetAllPiecesAsArray("b"))
            {
                var outline = piece.gameObject.AddComponent<Outline>();
                var p = piece.gameObject.AddComponent<Piece>();
                outline.OutlineColor = Color.green;
                outline.OutlineWidth = 7.5f;
                p.ownedByPlayer = true;
            }
            foreach (var piece in GetAllPiecesAsArray("w"))
            {
                var p = piece.gameObject.AddComponent<Piece>();
                p.ownedByPlayer = false;
            }
            playerTurn = false;
        }
    }

    private void InitializeGameOnServer(){
        var r = WebRequest.Create(url +"/games");
        r.Method = "POST";
        var response = r.GetResponse();
        var response_stream = response.GetResponseStream();
        var reader = new StreamReader(response_stream);
        string data = reader.ReadToEnd();
        JObject root = JObject.Parse(data);
        gameKey = root.SelectToken("key").ToString();
        Debug.Log("game key:" + gameKey);
        var turn_info = root.SelectToken("turn_info");        
        stringmoves = turn_info.SelectToken("moves").Values<string>().ToList();
    }

    private void InitializeBoard(){
        var first_row_white = new Transform[]{ta1,cb1,ac1,Qw,Kw,af1,cg1,th1};
        var pawn_row_white = new Transform[]{pa2,pb2,pc2,pd2,pe2,pf2,pg2,ph2};
        var pawn_row_black = new Transform[]{pa7,pb7,pc7,pd7,pe7,pf7,pg7,ph7};
        var first_row_black = new Transform[]{ta8,cb8,ac8,Qn,Kn,af8,cg8,th8};

        _board = new Transform[8,8]{
        { ta1, pa2,null,null,null,null,pa7,ta8},
        { cb1, pb2,null,null,null,null,pb7,cb8},
        { ac1, pc2,null,null,null,null,pc7,ac8},
        { Qw, pd2,null,null,null,null,pd7,Qn},
        { Kw, pe2,null,null,null,null,pe7,Kn},
        { af1, pf2,null,null,null,null,pf7,af8},
        { cg1, pg2,null,null,null,null,pg7,cg8},
        { th1, ph2,null,null,null,null,ph7,th8}};
    }

    private void GenerateHighlightSquareArray(){
        for (int i = 0; i < 8; i++){
            for (int j = 0; j < 8; j++){
                var squareInstance = Instantiate(highlightSquarePrefab, new Vector3(i,0.232f,j), Quaternion.identity);
                squareInstance.transform.parent = highlightSquareCont.transform;
                squareInstance.transform.Rotate(new Vector3(90,0,0), Space.Self);

                _highlightSquares[i,j] = squareInstance;
                squareInstance.SetActive(false);
            }
        }
    }

    private Transform[] GetAllPiecesAsArray(string color){
        if(color == "w"){
            return new Transform[]{ph2, pg2, pf2, pe2, pd2, pc2, pb2, pa2, th1, cg1, af1, Kw, Qw, ac1, cb1, ta1};
        }else{
            return new Transform[]{ph7, pg7, pf7, pe7, pd7, pc7, pb7, pa7, th8, cg8, af8, Kn, Qn, ac8, cb8, ta8};
        }
    }
}
