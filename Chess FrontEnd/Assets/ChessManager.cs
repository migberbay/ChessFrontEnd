using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

public class ChessManager : MonoBehaviour
{
    #region declarations

    [Header("Pieces")]
    [Space(1)]
    // Black Pieces
    public Transform pa7;
    public Transform pb7, pc7, pd7, pe7, pf7, pg7, ph7, ta8, cb8, ac8, Kn, Qn, af8, cg8, th8;

    // White Pieces
    public Transform pa2, pb2, pc2, pd2, pe2, pf2, pg2, ph2, ta1, cb1, ac1, Kw, Qw, af1, cg1, th1;

    [Header("Icons")]
    [Space(1)]
    public Sprite bbf;
    public Sprite bbl,bbr,bnf,bnl,bnr,bqf,bql,bqr,brf,brl,brr,wbf,wbl,wbr,wnf,wnl,wnr,wqf,wql,wqr,wrf,wrl,wrr,castle_bl,castle_br,castle_wl,castle_wr;

    [Header("Logic")]
    [Space(1)]

    public Transform whiteCementery;
    public Transform blackCementery;

    public GameObject MCWhite, MCBlack, highlightSquarePrefab, highlightSquareCont, CanvasOptionPrefab, ButtonOptionPrefab, ResetCanvas;

    public Mesh queenMesh, bishopMesh, knightMesh, rookMesh, pawnMesh;


    Transform[,] _board =  new Transform[8,8];
    int deadPiecesWhite = 0, deadPiecesBlack = 0;
    bool moveSelected, playingWhite = false, playingBlack = false, playerTurn = false, currentTurnCheck = false, isGameLoopRunning = false;
    float timeGameLoopInactive = 0f;

    Transform selectedPiece;
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
    GameObject canvasInstance;
    SoundManager soundM;


    #endregion

    public string playerColor(){
        return playingBlack ? "b":"w";
    }
    
    private void Awake() {
        InitializeBoard();
        InitializeSystem();
    }

    void Start(){
        soundM = GameObject.FindObjectOfType<SoundManager>();
        UnlayerEnemyPieces();
        try{
            InitializeGameOnServer();
        }catch{
            Debug.Log("Server is not online!");
            return;
        }
        GenerateHighlightSquareArray();
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop(){
        GetTurnInfoFromServer();
        var gameOver = CheckGameDoneConditions();
        if(gameOver){
            yield break;
        }

        if(playerTurn){
            moveSelected = false;
            while(!moveSelected){
                isGameLoopRunning = true;
                moveSelected = PlayerSelectionFrameRules();
                yield return null;
            }
            playerTurn = false;
        }else{ // if is not player turn.
            // DoAIMove(true); // performs a random move.
            DoAIMove(false); // gets a move from the server and executes it.
            playerTurn = true;
        }
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Game loop ended in: " + Time.time.ToString());
        StartCoroutine(GameLoop());
    }

    private void Update() {
        isGameLoopRunning = false;
    }

    private void FixedUpdate() {
        if(isGameLoopRunning){
            timeGameLoopInactive = 0f;
            return;
        }

        timeGameLoopInactive += Time.fixedDeltaTime;
        if(timeGameLoopInactive >= 3f){
            timeGameLoopInactive = 0f;
            Debug.Log("GameLoop is not running...");
        }
    }

    private bool CheckGameDoneConditions(){
        if(stringmoves.Count == 0){
            // si no tienes movimientos y estas en jaque es jaque mate
            // si no tienes movimientos y no estas en jaque es tablas
            if(currentTurnCheck){
                Debug.Log("Game Over, CheckMate!");
                StartCoroutine(ExplodeAllPieces());
            }else{
                Debug.Log("Game Over, Draw :(");
                StartCoroutine(ExplodeAllPieces());
            }
            return true;
        }
        return false;
    }

    void PresentResetMenu(){
        ResetCanvas.SetActive(true);
    }

    public void ResetGame(){
        ResetCanvas.SetActive(false);
        InitializeBoard();

        for (int i = _board.GetLowerBound(0); i <= _board.GetUpperBound(0); i++){
            for (int j = _board.GetLowerBound(1); j <= _board.GetUpperBound(1); j++){
                var o  = _board[i,j];
                if(o != null){
                    var p = o.gameObject;
                    var rb = p.GetComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.transform.position = new Vector3(i, 0.25f, j);
                    if(j == 0 || j == 1)// White
                        rb.transform.rotation = Quaternion.Euler(-90,0,0);
                    else
                        rb.transform.rotation = Quaternion.Euler(-90,0,180);

                    var piece = p.GetComponent<Piece>();
                    piece.ownedByPlayer = false;
                    if(piece.pieceType == "pawn"){
                        p.GetComponent<MeshFilter>().mesh = pawnMesh;
                    }
                }
            }
        }

        foreach (var item in _highlightSquares){
            Destroy(item);
        }

        MCBlack.SetActive(false);
        MCWhite.SetActive(false);
        moveSelected = false;
        playingBlack = false;
        playingWhite = false;
        playerTurn = false;
        currentTurnCheck = false;

        deadPiecesBlack = 0;
        deadPiecesWhite = 0;

        this.StopAllCoroutines();

        Awake();
        Start();

        //reset the cameras on the menus that need em for canvas events.

        GameObject.FindObjectOfType<MoveToMusicMenu>().Start();
        GameObject.FindObjectOfType<RegulateMasterVolume>().Start();
    }

    IEnumerator ExplodeAllPieces(){
        foreach (var piece in GetAllPiecesAsArray("w"))
        {
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        foreach (var piece in GetAllPiecesAsArray("b"))
        {
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        var numexplosions = 20;
        var explosionforce = 7.5f;
        var explosionRadius = 3f;

        float xmin = -1, xmax = 8 , zmax = 8, zmin = -1;
        for (int i = 0; i < numexplosions; i++)
        {   
            var explosionOrigin = new Vector3(Random.Range(xmin,xmax) ,1, Random.Range(zmin,zmax));
            foreach (var piece in Physics.OverlapSphere(explosionOrigin, explosionRadius))
            {
                if(piece.GetComponent<Piece>() != null)
                    piece.GetComponent<Rigidbody>().AddExplosionForce(explosionforce, explosionOrigin, explosionRadius, 0, ForceMode.Impulse);
            }
            yield return new WaitForSeconds(0.1f);
        }

        PresentResetMenu();
    }

    public void QuitGame(){
        Application.Quit();
        Debug.Log("this quits the game on built application");
    }

    private void DoAIMove(bool random){
        Vector2 aux = new Vector2();
        // pick random from stringmoves and send it back
        var chosenMove = "";

        if(random)
            chosenMove = stringmoves[Random.Range(0,stringmoves.Count)];
        else
            chosenMove = GetMoveSuggestionFromServer();

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
        soundM.PlayChessMoveSound();

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

    private Sprite GetMoveImagePromotions(string toPromote, string dir, string color){
        if(toPromote == "Q"){
            switch (dir)
            {
                case "<":
                    if(color == "w")
                        return wql;
                    else
                        return bql;

                case ">":
                    if(color == "w")
                        return wqr;
                    else
                        return bqr;

                case "^":
                    if(color == "w")
                        return wqf;
                    else
                        return bqf;
            }
        }

        if(toPromote == "R"){
            switch (dir)
            {
                case "<":
                    if(color == "w")
                        return wrl;
                    else
                        return brl;

                case ">":
                    if(color == "w")
                        return wrr;
                    else
                        return brr;

                case "^":
                    if(color == "w")
                        return wrf;
                    else
                        return brf;
            }
            
        }

        if(toPromote == "B"){
            switch (dir)
            {
                case "<":
                    if(color == "w")
                        return wbl;
                    else
                        return bbl;

                case ">":
                    if(color == "w")
                        return wbr;
                    else
                        return bbr;

                case "^":
                    if(color == "w")
                        return wbf;
                    else
                        return bbf;
            }
            
        }

        if(toPromote == "N"){
            switch (dir)
            {
                case "<":
                    if(color == "w")
                        return wnl;
                    else
                        return bnl;

                case ">":
                    if(color == "w")
                        return wnr;
                    else
                        return bnr;

                case "^":
                    if(color == "w")
                        return wnf;
                    else
                        return bnf;
            }
        }

        return null;
    }

    private Sprite GetMoveImageCastle(string dir){
        //castle_bl,castle_br,castle_wl,castle_wr;
        
        if(playingWhite){
            if(dir == ">")
                return castle_wr;
            else
                return castle_wl;
        }else{
            if(dir == ">")
                return castle_br;
            else
                return castle_bl;
        }
    }

    private void AddOptionPanelToSelectedPiece(List<string> options){
        canvasInstance = Instantiate(CanvasOptionPrefab);
        canvasInstance.transform.position = new Vector3(selectedPiece.transform.position.x, 2.5f, selectedPiece.transform.position.z);
        canvasInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(options.Count*1.5f, 0.8f);
        canvasInstance.GetComponent<BoxCollider>().size = new Vector3(options.Count*1.5f, 0.75f, 0.1f);
        var look = canvasInstance.GetComponent<LookAtCamera>();
        look.cameraToLook = playingWhite ? MCWhite : MCBlack;

        if(playingBlack)
            look.InitPanel("b");

        if(playingWhite)
            look.InitPanel("w");

        var layoutParent = canvasInstance.transform.Find("Panel");
        foreach (var o in options){
            var buttonInstance = Instantiate(ButtonOptionPrefab, layoutParent);
            Button b = buttonInstance.GetComponent<Button>();
            var button_image = buttonInstance.GetComponentInChildren<Image>();

            var pieceType = selectedPiece.GetComponent<Piece>().pieceType;
            if(!currentTurnCheck){
                if(o == "O-O-O"){
                    if(pieceType == "rook"){
                        if(playingWhite){
                            button_image.sprite = GetMoveImageCastle(">");
                            b.onClick.AddListener(delegate{Castle(3,-2, selectedPiece, Kw);});
                        }
                        if(playingBlack){
                            button_image.sprite = GetMoveImageCastle("<");
                            b.onClick.AddListener(delegate{Castle(3,-2, selectedPiece, Kn);});
                        }
                    }else{
                        if(playingWhite){
                            button_image.sprite = GetMoveImageCastle("<");//"<-C";
                            b.onClick.AddListener(delegate{Castle(3,-2, ta1, selectedPiece);});
                        }
                        if(playingBlack){
                            button_image.sprite = GetMoveImageCastle(">");//"C->";
                            b.onClick.AddListener(delegate{Castle(3,-2, ta8, selectedPiece);});
                        }
                    }  
                }

                if(o == "O-O"){
                    if(pieceType == "rook"){
                        if(playingWhite){
                            button_image.sprite = GetMoveImageCastle("<");//"<-C";
                            b.onClick.AddListener(delegate{Castle(-2,2, selectedPiece, Kw);});
                        }
                        if(playingBlack){
                            button_image.sprite = GetMoveImageCastle(">");//"C->";
                            b.onClick.AddListener(delegate{Castle(-2,2, selectedPiece, Kn);});
                        }
                    }else{
                        if(playingWhite){
                            button_image.sprite = GetMoveImageCastle(">");//"C->";
                            b.onClick.AddListener(delegate{Castle(-2,2, th1, selectedPiece);});
                        }
                        if(playingBlack){
                            button_image.sprite = GetMoveImageCastle("<");//"<-C";
                            b.onClick.AddListener(delegate{Castle(-2,2, th8, selectedPiece);});
                        }
                    }
                }
            }

            

            if(pieceType == "pawn"){
                var originFile = o.Substring(0,1);
                var destinationFile = o.Substring(2,1);
                var pieceToPromote = o.Substring(5,1); 

                // Convert the string into a byte.
                byte asciiByteOrigin = Encoding.ASCII.GetBytes(originFile)[0];
                byte asciiByteDestination = Encoding.ASCII.GetBytes(destinationFile)[0];

                bool lessThanCurrent = asciiByteOrigin < asciiByteDestination;
                bool greaterThanCurrent = asciiByteOrigin > asciiByteDestination;

                Debug.Log("origin: " + asciiByteOrigin + ", Destination: " + asciiByteDestination);
                
                if(lessThanCurrent){
                    if(playingBlack){
                        button_image.sprite = GetMoveImagePromotions(pieceToPromote,"<","b");
                        b.onClick.AddListener(delegate{PromotePawn(pieceToPromote, o, new Vector2(1,-1));});
                    }
                    if(playingWhite){
                        button_image.sprite = GetMoveImagePromotions(pieceToPromote,">","w");
                        b.onClick.AddListener(delegate{PromotePawn(pieceToPromote, o, new Vector2(1,1));});
                    }

                }else if(greaterThanCurrent){
                    if(playingBlack){
                        button_image.sprite = GetMoveImagePromotions(pieceToPromote,">","b");
                        b.onClick.AddListener(delegate{PromotePawn(pieceToPromote, o, new Vector2(-1,-1));});
                    }
                    if(playingWhite){
                        button_image.sprite = GetMoveImagePromotions(pieceToPromote,"<","w");
                        b.onClick.AddListener(delegate{PromotePawn(pieceToPromote, o, new Vector2(-1,1));});
                    }
                }else{
                    if(playingBlack){
                        button_image.sprite = GetMoveImagePromotions(pieceToPromote,"^","b");
                        b.onClick.AddListener(delegate{PromotePawn(pieceToPromote, o, new Vector2(0,-1));});
                    }else{
                        button_image.sprite = GetMoveImagePromotions(pieceToPromote,"^","w");
                        b.onClick.AddListener(delegate{PromotePawn(pieceToPromote, o, new Vector2(0,1));});
                    }
                }
            }
        }
    }

    private void Castle(int rookX, int kingX, Transform rook, Transform king){
        //Update board
        _board[(int)king.transform.position.x, (int)king.transform.position.z] = null;
        _board[(int)rook.transform.position.x, (int)rook.transform.position.z] = null;

        _board[(int)king.transform.position.x + kingX, (int)king.transform.position.z] = king;
        _board[(int)rook.transform.position.x + rookX, (int)rook.transform.position.z] = rook;

        //Update pieces.
        king.transform.position += new Vector3(kingX,0,0); 
        rook.transform.position += new Vector3(rookX,0,0); 
        
        var moveSyntax = "";
        if(Mathf.Abs(rookX) == 3)
            moveSyntax = "O-O-O";
        else
            moveSyntax = "O-O";

        SendMoveToServer(new Vector2(), new Vector2(), moveSyntax, true);
        UnselectPiece();
        
        playerTurn = false;
        moveSelected = true;
    }

    private void PromotePawn(string toPromote, string serverSyntax , Vector2 positionChange){
        Vector2 selectedPos = new Vector2 (Mathf.RoundToInt(selectedPiece.transform.position.x), Mathf.RoundToInt(selectedPiece.transform.position.z));
        Vector2 destinationPos = selectedPos + positionChange;

        //Update board
        _board[(int)selectedPos.x, (int)selectedPos.y] = null;
        Transform piece_in_destination = _board[Mathf.RoundToInt(destinationPos.x), Mathf.RoundToInt(destinationPos.y)];
        _board[Mathf.RoundToInt(destinationPos.x), Mathf.RoundToInt(destinationPos.y)] = selectedPiece.transform;

        if(piece_in_destination != null){
            MovePieceToCementery(piece_in_destination);
        }

        //Update server and client visuals
        SendMoveToServer(new Vector2(), new Vector2(), serverSyntax, true);
        selectedPiece.gameObject.GetComponent<Piece>().MoveToPosition(destinationPos);
        switch (toPromote)
        {
            case "Q":
                selectedPiece.GetComponent<MeshFilter>().mesh = queenMesh;
                break;

            case "B":
                selectedPiece.GetComponent<MeshFilter>().mesh = bishopMesh;
                break;
            
            case "R":
                selectedPiece.GetComponent<MeshFilter>().mesh = rookMesh;
                break;

            case "N":
                selectedPiece.GetComponent<MeshFilter>().mesh = knightMesh;
                break;

            default:
                Debug.Log("Something is fishy here, the promotion you asked for was: " + toPromote);
                break;
        }

        UnselectPiece();
        
        playerTurn = false;
        moveSelected = true;
    }

    private bool PlayerSelectionFrameRules(){
        if(Input.GetMouseButtonDown(0)){
            RaycastHit hit;
            Ray ray = active.ScreenPointToRay(Input.mousePosition);
            
            if(selectedPiece == null){ // Piece Selection
                int layer_mask = LayerMask.GetMask("Pieces");
                Physics.Raycast(ray,out hit,100f,layer_mask);
                Debug.Log("Cheking for pieces");

                if(hit.point != new Vector3(0,0,0)){ // we hit a piece.
                    if(hit.transform.GetComponent<Piece>().ownedByPlayer){ // the piece is owned by the player.
                        selectedPiece = hit.transform;

                        selectedPiece.GetComponent<Outline>().OutlineColor = Color.red;
                        var pieceSquare = squareNames[Mathf.RoundToInt(selectedPiece.transform.position.x), Mathf.RoundToInt(selectedPiece.transform.position.z)];
                        List<string> applicable_moves = new List<string>(stringmoves.FindAll(x => x.Substring(0,2) == pieceSquare));
                        
                        var pieceScript = selectedPiece.GetComponent<Piece>();
                        var pieceType = pieceScript.pieceType;
                        pieceScript.isSelected = true;

                        // Checking castles
                        var castleMoves = stringmoves.FindAll(x => x.Substring(0,1) == "O");

                        if(castleMoves.Count != 0){
                            if(pieceType == "rook"){
                                if(selectedPiece.transform.position.x == 0 && castleMoves.Contains("O-O-O")) //A-file rook
                                    AddOptionPanelToSelectedPiece(new List<string>{"O-O-O"});

                                if(selectedPiece.transform.position.x == 7 && castleMoves.Contains("O-O")) // H-file rook.
                                    AddOptionPanelToSelectedPiece(new List<string>{"O-O"});
                            }

                            if(pieceType == "king"){
                                AddOptionPanelToSelectedPiece(castleMoves);
                            }
                        }

                        bool pawnIsSelectedAndCanPromote = false;

                        // Checking Pawn Promotion
                        if(pieceType == "pawn"){
                            var pawnPromotions = applicable_moves.FindAll(x => x.Contains("="));
                            if(pawnPromotions.Count != 0){
                                pawnIsSelectedAndCanPromote = true;
                                List<string> options = new List<string>();
                                foreach (var promotion in pawnPromotions){ // There can be up to 12 pawn promotions
                                    options.Add(promotion);
                                }
                                AddOptionPanelToSelectedPiece(options);
                            }
                        }


                        // Highlight squares
                        if(!pawnIsSelectedAndCanPromote){
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
                }
            }else{ // Move Piece
                int layer_mask = LayerMask.GetMask(new string[]{"Board","UI"});
                Physics.Raycast(ray, out hit, 100f, layer_mask);
                Debug.Log("Cheking for board");

                try{
                    if(hit.transform.gameObject.layer == 5)// hits a UI layer object
                        return false; // we leave the logic to the UI object.
                }catch{
                    // we hit neither the board or the UI object
                    // so we cancel the operation completely.
                    UnselectPiece();
                    return false;
                }

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
                        soundM.PlayChessMoveSound();
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
            foreach (var p in GetAllPiecesAsArray("w")){
                p.gameObject.layer = 0;
            }
        }

        if(playingWhite){
            foreach (var p in GetAllPiecesAsArray("b")){
                p.gameObject.layer = 0;
            }
        }
    }

    private void UnselectPiece(){
        if(selectedPiece != null){
            selectedPiece.gameObject.GetComponent<Outline>().OutlineColor = Color.green;
            selectedPiece.GetComponent<Piece>().Unselect();
            ResetHighLightSquares();
            validMoves.Clear();
            Destroy(canvasInstance);
        }
        selectedPiece = null;
}

    private void ResetHighLightSquares(){
        foreach (var s in _highlightSquares)
        {
            s.SetActive(false);
        }
    }

    private void SetOutlineAndPiece(Transform piece, bool pieceActive){
        Outline outline = piece.GetComponent<Outline>();

        if(outline == null){
            Debug.Log("adding outline component to piece");
            outline = piece.gameObject.AddComponent<Outline>();
        }else{
            outline = piece.gameObject.GetComponent<Outline>();
        }

        outline.enabled = false;
        outline.OutlineColor = Color.green;
        outline.OutlineMode = Outline.Mode.OutlineVisible;
        outline.OutlineWidth = 5f;
        
        Piece pieceScript = piece.GetComponent<Piece>();
        if(pieceScript == null){
            pieceScript = piece.gameObject.AddComponent<Piece>();
        }else{
            pieceScript = piece.gameObject.GetComponent<Piece>();
        }
        pieceScript.enabled = pieceActive;
        pieceScript.ownedByPlayer = pieceActive;
    }

    private void InitializeSystem(){
        int side = UnityEngine.Random.Range(0,2);
        // side = 1;

        if(side==0){
            active = MCWhite.GetComponent<Camera>();
            playingWhite = true;    
            MCWhite.SetActive(true);

            foreach (var piece in GetAllPiecesAsArray("w")){ // PLAYER PIECES.
                SetOutlineAndPiece(piece, true);
            }
            foreach (var piece in GetAllPiecesAsArray("b")){ // RIVAL PIECES.
                SetOutlineAndPiece(piece, false);
            }
            playerTurn = true;
        }else{
            active = MCBlack.GetComponent<Camera>();
            playingBlack = true;
            MCBlack.SetActive(true);
            
            foreach (var piece in GetAllPiecesAsArray("b")){ // PLAYER PIECES.
                SetOutlineAndPiece(piece, true);
            }
            foreach (var piece in GetAllPiecesAsArray("w")){ // RIVAL PIECES.
                SetOutlineAndPiece(piece, false);
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
        // Debug.Log("turn info: " + turn_info.ToString());
        stringmoves = turn_info.SelectToken("moves").Values<string>().ToList();
        currentTurnCheck = turn_info.SelectToken("in_check").Value<bool>();
    }

    private string GetMoveSuggestionFromServer(){
        var r = WebRequest.Create(url +"/games/"+gameKey+"/move_suggestion");
        r.Method = "GET";
        var response = r.GetResponse();
        var response_stream = response.GetResponseStream();
        var reader = new StreamReader(response_stream);
        string data = reader.ReadToEnd();
        JObject root = JObject.Parse(data);
        var move = root.SelectToken("move").ToString();
        return move;
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