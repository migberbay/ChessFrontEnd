using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessManager : MonoBehaviour
{
    // Black Pieces
    public Transform ph7, pg7, pf7, pe7, pd7, pc7, pb7, pa7, th8, cg8, af8, Kn, Qn, ac8, cb8, ta8;

    // White Pieces
    public Transform ph2, pg2, pf2, pe2, pd2, pc2, pb2, pa2, th1, cg1, af1, Kw, Qw, ac1, cb1, ta1;
   
    public Transform[][] _board =  new Transform[8][];

    void Start()
    {
        InitializeBoard();
        PrintBoardState();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializeBoard(){
        var first_row_white = new Transform[]{ta1,cb1,ac1,Qw,Kw,af1,cg1,th1};
        var pawn_row_white = new Transform[]{pa2,pb2,pc2,pd2,pe2,pf2,pg2,ph2};
        var no_mans_land = new Transform[8];
        var pawn_row_black = new Transform[]{pa7,pb7,pc7,pd7,pe7,pf7,pg7,ph7};
        var first_row_black = new Transform[]{ta8,cb8,ac8,Qn,Kn,af8,cg8,th8};

        _board[0] = first_row_black;
        _board[1] = pawn_row_black;
        _board[2] = no_mans_land;
        _board[3] = no_mans_land;
        _board[4] = no_mans_land;
        _board[5] = no_mans_land;
        _board[6] = pawn_row_white;
        _board[7] = first_row_white;
    }

    private void PrintBoardState(){
        string to_print = "==============================\n";
        foreach (var row in _board)
        {
            to_print +="||";
            foreach (var piece in row)
            {   
                try{
                    to_print += piece.gameObject.name +"|";
                }catch{
                    to_print += "----|";
                }
            }
            to_print += "|\n";
        }
        to_print += "==============================\n";
        Debug.Log(to_print);
    }
}
