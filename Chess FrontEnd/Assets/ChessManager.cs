using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using TMPro;

public class ChessManager : MonoBehaviour
{
    #region declarations
    // Black Pieces
    public Transform pa7, pb7, pc7, pd7, pe7, pf7, pg7, ph7, ta8, cb8, ac8, Kn, Qn, af8, cg8, th8;

    // White Pieces
    public Transform pa2, pb2, pc2, pd2, pe2, pf2, pg2, ph2, ta1, cb1, ac1, Kw, Qw, af1, cg1, th1;

    public Transform whiteCementery, blackCementery;

    public GameObject MCWhite, MCBlack, highlightSquarePrefab, highlightSquareCont, CanvasOptionPrefab, ButtonOptionPrefab;


    int deadPiecesWhite = 0, deadPiecesBlack = 0;
    bool playingWhite = false, playingBlack = false, playerTurn = false, currentTurnCheck = false;
    Transform selectedPiece;
    Transform[,] _board =  new Transform[8,8];
    List<Vector2> validMoves = new List<Vector2>();
    string[,] squareNames = new string[8,8]{
    { "a1","a2","a3","a4","a5","a6","a7","a8"},
    { "b1","b2","b3","b4","b5","b6","b7","b8"},
    { "c1","c2","c3","c4","c5","c6","c7","c8"},
    { "d1","d2","d3","d4","d5","d6","d7","d8"},
    { "e1","e2","e3","e4","e5","e6","e7","e8"},
    { "f1","f2","f3","f4","f5","f6","f7","f8"},
    { "g1","g2","g3","g4","g5","g6","g7","g8"},
    { "h1","h2","h3","h4","h5","h6","h7","h8"}};
    GameObject[,] _highlightSquares = new GameObject[8,8];
    Camera active;
    List<string> stringmoves;
    string url = "http://127.0.0.1:8000", gameKey = "";

    #endregion

    void Start()
    {
        InitializeBoard();
        InitializeSystem();
        UnlayerEnemyPieces();
        try{
            InitializeGameOnServer();
        }catch{
            Debug.Log("Server is not online!");
        }
        GenerateHighlightSquareArray();
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop(){
        GetTurnInfoFromServer();
        var gameOver = CheckGameDoneConditions();
        if(gameOver){
            yield return null;
        }
        
        Debug.Log("continues loop");

        if(playerTurn){
            bool moveSelected = false;
            while(!moveSelected){
                moveSelected = PlayerSelectionFrameRules();
                yield return null;
            }
            playerTurn = false;
        }else{ // if is not player turn.
            DoRandomMove();
            playerTurn = true;
        }

        StartCoroutine(GameLoop());
    }

    private bool CheckGameDoneConditions(){
        if(stringmoves.Count == 0){
            // si no tienes movimientos y estas en jaque es jaque mate
            // si no tienes movimientos y no estas en jaque es tablas
            if(currentTurnCheck){
                Debug.Log("Game Over, CheckMate!");
            }else{
                Debug.Log("Game Over, Draw :(");
            }
            return true;
        }
        return false;
    }


    private void DoRandomMove(){
        Vector2 aux = new Vector2();
        // pick random from stringmoves and send it back
        var chosenMove = stringmoves[Random.Range(0,stringmoves.Count)];
        Vector2 origin = new Vector2(), destination = new Vector2();

        for (int i = squareNames.GetLowerBound(0); i <= squareNames.GetUpperBound(0); i++){
            for (int j = squareNames.GetLowerBound(1); j <= squareNames.GetUpperBound(1); j++){
                if(squareNames[i,j] == chosenMove.Substring(0,2)){
                    origin.x = i;
                    origin.y = j;
                }
                
                if(squareNames[i,j] == chosenMove.Substring(2,2)){
                    destination.x = i;
                    destination.y = j;
                }

                if(origin != new Vector2() && destination != new Vector2()){break;}
            }
        }
        // Move in client
        Transform enemyPiece = _board[(int)origin.x, (int)origin.y];
        enemyPiece.gameObject.GetComponent<Piece>().MoveToPosition(destination);

        _board[(int)origin.x, (int)origin.y] = null;
        Transform piece_in_destination = _board[(int)destination.x, (int)destination.y];
        _board[(int)destination.x, (int)destination.y] = enemyPiece.transform;

        if(piece_in_destination != null){ // this is an enemy move so we send the piece to enemy cementery
            MovePieceToCementery(piece_in_destination);
        }


        //Update server and client visuals
        SendMoveToServer(aux, aux, chosenMove, true);
    }

    private void MovePieceToCementery(Transform piece){
        Transform cementery;
        int deadPieces = 0;
        if((playingBlack && playerTurn) || (playingWhite && !playerTurn)){
            deadPieces = deadPiecesWhite;
            deadPiecesWhite ++;
            cementery = whiteCementery;
        }else{
            deadPieces = deadPiecesBlack;
            deadPiecesBlack ++;
            cementery = blackCementery;
        }

        piece.transform.position = new Vector3(cementery.position.x + (int)deadPieces/5, piece.transform.position.y, cementery.position.z + deadPieces%5);
        try{
            piece.gameObject.GetComponent<Piece>().enabled = false;
            piece.gameObject.GetComponent<Outline>().enabled = false;
        }catch{}

    }

    private void AddOptionPanelToPiece(List<string> options, Transform piece){
        var canvasInstance = Instantiate(CanvasOptionPrefab);
        canvasInstance.transform.position = new Vector3(piece.transform.position.x, 2.5f, piece.transform.position.z);
        canvasInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(options.Count, 0.75f);
        var look = canvasInstance.GetComponent<LookAtCamera>();
        look.cameraToLook = playingWhite ? MCWhite : MCBlack;

        if(playingBlack)
            look.InitPanel("b");

        if(playingWhite)
            look.InitPanel("w");

        var layoutParent = canvasInstance.transform.Find("Panel");
        foreach (var o in options){
            var buttonInstance = Instantiate(ButtonOptionPrefab, layoutParent);
            var button_text = buttonInstance.GetComponentInChildren<TMP_Text>();
            if(o == "O-O-O"){
                if(piece.GetComponent<Piece>().pieceType == "rook"){
                    if(playingWhite)
                        button_text.text = "C->";
                    if(playingBlack)
                        button_text.text = "<-C";
                }else{
                    if(playingWhite)
                        button_text.text = "<-C";
                    if(playingBlack)
                        button_text.text = "C->";
                }
                   
            }
            if(o == "O-O"){
                if(piece.GetComponent<Piece>().pieceType == "rook"){
                    if(playingWhite)
                        button_text.text = "<-C";
                    if(playingBlack)
                        button_text.text = "C->";
                }else{
                    if(playingWhite)
                        button_text.text = "C->";
                    if(playingBlack)
                        button_text.text = "<-C";
                }
            }
        }
    }

    private void Castle(string castleOptions){

    }

    private void PromotePawn(Transform pawn, string toPromote){

    }

    private bool PlayerSelectionFrameRules(){
        if(Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            Ray ray = active.ScreenPointToRay(Input.mousePosition);
            
            if(selectedPiece == null){ // Piece Selection
                int layer_mask = LayerMask.GetMask("Pieces");
                Physics.Raycast(ray,out hit,100f,layer_mask);
                if(hit.point != new Vector3(0,0,0)){ // we hit a piece.
                    if(hit.transform.GetComponent<Piece>().ownedByPlayer){
                        selectedPiece = hit.transform;
                        selectedPiece.GetComponent<Outline>().OutlineColor = Color.red;
                        var pieceSquare = squareNames[Mathf.RoundToInt(selectedPiece.transform.position.x), Mathf.RoundToInt(selectedPiece.transform.position.z)];
                        List<string> applicable_moves = new List<string>(stringmoves.FindAll(x => x.Substring(0,2) == pieceSquare));
                        
                        var pieceType = selectedPiece.GetComponent<Piece>().pieceType;

                        // Checking castles

                        var castleMoves = stringmoves.FindAll(x => x.Substring(0,1) == "O");

                        if(castleMoves.Count != 0){
                            if(pieceType == "rook"){
                                // RaycastHit[] hits = new RaycastHit[2];
                                // Physics.Raycast(selectedPiece.transform.position + new Vector3(-0.75f, 0.5f, 0), Vector3.left, out hits[0], 10);
                                // Physics.Raycast(selectedPiece.transform.position + new Vector3(0.75f, 0.5f, 0), Vector3.right, out hits[1], 10);
                                
                                // var aux1 = hits[0].transform.GetComponent<Piece>();
                                // if(aux1 != null) aux1.pieceType == "king

                                if(selectedPiece.transform.position.x == 0 && castleMoves.Contains("O-O-O")) //A-file rook
                                    AddOptionPanelToPiece(new List<string>{"O-O-O"}, selectedPiece);

                                if(selectedPiece.transform.position.x == 7 && castleMoves.Contains("O-O")) // H-file rook.
                                    AddOptionPanelToPiece(new List<string>{"O-O"}, selectedPiece);
                                
                            }

                            if(pieceType == "king"){
                                AddOptionPanelToPiece(castleMoves, selectedPiece);
                            }
                        }

                        // Checking PawnPromotion
                        if(pieceType == "pawn"){
                            var pawnPromotions = applicable_moves.FindAll(x => x.Contains("="));
                            if(pawnPromotions.Count != 0){
                                List<string> options = new List<string>();
                                foreach (var promotion in pawnPromotions){ // There can be up to 12 pawn promotions
                                    options.Add(promotion);
                                }
                                AddOptionPanelToPiece(options, selectedPiece);
                            }
                        }

                        
                        // Highlight squares
                        for (int i = squareNames.GetLowerBound(0); i <= squareNames.GetUpperBound(0); i++){
                            for (int j = squareNames.GetLowerBound(1); j <= squareNames.GetUpperBound(1); j++){
                                foreach (var move in applicable_moves){
                                    if(move.Substring(2,2) == squareNames[i,j]){
                                        _highlightSquares[i,j].SetActive(true);
                                        validMoves.Add(new Vector2(i,j));
                                    }
                                }
                            }
                        }


                    }
                }
            }else{ // Move Piece
                int layer_mask = LayerMask.GetMask("Board");
                Physics.Raycast(ray, out hit, 100f, layer_mask);

                var pointHit = new Vector2(Mathf.RoundToInt(hit.point.x), Mathf.RoundToInt(hit.point.z));
                
                foreach (var v in validMoves){
                    if(v.Equals(pointHit)){
                        //TODO: en passant

                        //Move is valid, perform it and pass the turn to the next player.
                        //Update board
                        _board[Mathf.RoundToInt(selectedPiece.transform.position.x), Mathf.RoundToInt(selectedPiece.transform.position.z)] = null;
                        Transform piece_in_destination = _board[Mathf.RoundToInt(pointHit.x), Mathf.RoundToInt(pointHit.y)];
                        _board[Mathf.RoundToInt(pointHit.x), Mathf.RoundToInt(pointHit.y)] = selectedPiece.transform;

                        if(piece_in_destination != null){ // this is our move so we send the piece to our cementery.
                            MovePieceToCementery(piece_in_destination);
                        }

                        //Update server and client visuals
                        SendMoveToServer(new Vector2(Mathf.RoundToInt(selectedPiece.transform.position.x), Mathf.RoundToInt(selectedPiece.transform.position.z)), pointHit, "", false);
                        selectedPiece.gameObject.GetComponent<Piece>().MoveToPosition(pointHit);
                        UnselectPiece();
                        
                        playerTurn = false;
                        return true;
                    }
                }
                // point wasn't valid, unselect pieces.
                UnselectPiece();
            } 
        }

        if(Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)){
            UnselectPiece();
        }

        return false;
    }

    private void UnlayerEnemyPieces(){
        if(playingBlack){
            foreach (var p in GetAllPiecesAsArray("w"))
            {
                p.gameObject.layer = 0;
            }
        }

        if(playingWhite){
            foreach (var p in GetAllPiecesAsArray("b"))
            {
                p.gameObject.layer = 0;
            }
        }
    }

    private void UnselectPiece(){
        if(selectedPiece != null){
            selectedPiece.gameObject.GetComponent<Outline>().OutlineColor = Color.green;
            ResetHighLightSquares();
            validMoves.Clear();
        }
        selectedPiece = null;
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
    }

    private void GetTurnInfoFromServer(){
        var r = WebRequest.Create(url +"/games/"+gameKey);
        r.Method = "GET";
        var response = r.GetResponse();
        var response_stream = response.GetResponseStream();
        var reader = new StreamReader(response_stream);
        string data = reader.ReadToEnd();
        JObject root = JObject.Parse(data);
        var turn_info = root.SelectToken("turn_info");
        Debug.Log("turn info: " + turn_info.ToString());
        stringmoves = turn_info.SelectToken("moves").Values<string>().ToList();
        currentTurnCheck = turn_info.SelectToken("in_check").Value<bool>();
    }

    private void SendMoveToServer(Vector2 move_origin, Vector2 move_desination, string moveString, bool usingString){
        var r = WebRequest.Create(url +"/games/"+ gameKey +"/move");
        r.Method = "POST";
        r.ContentType = "application/json";

        var post_data = "";
        if(usingString){
            post_data = "{\"move\":\""+moveString+"\"}";
        }else{
            post_data = "{\"move\":\""+squareNames[(int)move_origin.x, (int)move_origin.y]
            +""+squareNames[(int)move_desination.x, (int)move_desination.y]+"\"}";
        }

        var data = Encoding.ASCII.GetBytes(post_data);

        using (var stream = r.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        var response = (HttpWebResponse)r.GetResponse();
        var response_stream = response.GetResponseStream();
        var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
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
