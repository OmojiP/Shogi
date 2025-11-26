using UnityEngine;

public class Piece : MonoBehaviour
{
    [SerializeField] private PieceType _pieceType;
    [SerializeField] private PlayerSide _playerSide;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public enum PieceType
{
    FU = 0,
    KYOSHA = 1,
    KEIMA = 2,
    GIN = 3,
    KIN = 4,
    KAKU = 5,
    HISHA = 6,
    OU = 7,
    GYOKU = 8,
}

public enum PlayerSide
{
    Bottom = 0,
    Top = 9,
}