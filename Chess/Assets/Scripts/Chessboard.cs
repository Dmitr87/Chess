using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using UnityEngine.UI;

public enum SpecialMove //��� ��������� ����������� ���� 
{
    None = 0,
    EnPassant = 1,
    Castling = 2,
    Promotion = 3
}

public class Chessboard : MonoBehaviour
{
    [SerializeField] private Material tileMaterial; //��������, �� �������� ������� ������ (����������)
    [SerializeField] private float tileSize = 1.0f; //������ ������
    [SerializeField] private float yOffset = 0.2f; //��������� ������ ���� �������� �����
    [SerializeField] private Vector3 boardCenter = Vector3.zero; //����� �����
    [SerializeField] private float deathSize = 0.7f; //������ ������ ����� ������
    [SerializeField] private float deathSpacing = 0.525f; //���������� ����� �������� ��������, ����� ��� ����� �� ����� �����
    [SerializeField] private float dragOffset = 1.0f; //
    [SerializeField] private GameObject victoryScreen; //�����, ������������ ����� ����
    [SerializeField] private Transform rematchIndicator; //�������, �������������� ����� ����, ��� ��������� ������� ���� ��� ����� ������ rematch
    [SerializeField] private Button rematchButton; //������ rematch

    [SerializeField] private GameObject[] prefabs; //������� (��������� �������)
    [SerializeField] private Material[] teamMaterials; //��������� ��� ����� 

    //������
    private List<Vector2Int> availableMoves = new List<Vector2Int>(); //������ ��������� �����
    private List<ChessPiece> deadWhites = new List<ChessPiece>(); //������� ������ �����
    private List<ChessPiece> deadBlacks = new List<ChessPiece>(); //������� ������ ������
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>(); //������ �����
    private SpecialMove specialMove; //����������� ��� (���� ��)
    private const int NUMBER_OF_TILES_X = 8; //���������� ������ �� ��� � (�����������)
    private const int NUMBER_OF_TILES_Y = 8; //���������� ������ �� ��� y (���������)
    private bool isWhiteTurn; //������ ��� ����� ��� ���
    private ChessPiece currentlyDragging; //������ ������� �� ����������� ������
    private ChessPiece[,] chessPieces; //��� ������
    private GameObject[,] tiles; //������ �����
    private Camera currentCamera; //������, ����� ������� �� ������� ������
    private Vector2Int currentHover; //������, �� ������� �� ������ ������ ������
    private Vector3 bounds; //������� �����
    //��� ���� �� ����
    private int playerCount = -1; //���������� ������� (���� ���������� � 0 ����� ������������ ������ �����)
    private int currentTeam = -1; //�������, ������� ������ ������ ���
    private bool localGame = true; //��������� ���� ��� ���
    private bool[] playerWantsRematch = new bool[2]; //������ �� 2 ���������, � ������ ���������� ����������, ����� �� ����� ��� ������� (������ �������) ������ ������ ��� ��� (�� - 1, ��� - 0)

    private void Start() // ��� ������� ����
    {
        isWhiteTurn = true; //���� ������ ���������� � ���� �����

        GenerateAllTiles(tileSize, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y); //������� �����

        SpawnAllPieces(); //���������� ��� ������
        PositionAllPieces(); //����������� ������ � ���������� �������

        RegisterEvents();
    }

    private void Update()
    {
        if (!currentCamera) //���� ������ �� ����������
        {
            currentCamera = Camera.main; //���������� ������
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); //��� ������� ���� ����� ������ � ����� �������
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            //��������� ������� ������, �� ������� �����
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //���� ������ �� ������ �� ������� ������
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition; //������������ �� ������, ������� ����
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                //���������� ������ ���� ���� ���������
            }
            //���� �������� � ������ �� ������
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                //���� ������� � ������ �����, �� ���� ����� ����� ���������, ����� ������ �������
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                //�������������� ���� ���������
            }

            //����� �������� ������ ����
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    //��� ��� ��� ���
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1)
                        || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && localGame) || (chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && localGame))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //������� ������ ��������� ����� � ����������� ������
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y);
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck();
                        HighlightTiles();
                    }
                }
            }

            //����� ��������� ������ ����
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY); //���������� ������� �������� ������� �������� ��� ������ ������� �����������

                if (ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y))) //���� ��� ������� �� ������� ��������
                {
                    MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    NetMakeMove makeMove = new NetMakeMove();
                    makeMove.startX = previousPosition.x;
                    makeMove.startY = previousPosition.y;
                    makeMove.endX = hitPosition.x;
                    makeMove.endY = hitPosition.y;
                    makeMove.team = (currentTeam == 0) ? 1 : 0;
                    Client.Instance.SendToServer(makeMove); //������ ��� � ���������� ��� �� ������
                }
                else //�����
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y)); //���������� ������ �����
                    currentlyDragging = null; //�� ����������� ������� ������
                    RemoveHighlightTiles(); //�� ������������ ������� ������
                }
            }
        }
        else //���������� ���������� �����
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        //���� ������ ������ � ����
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    //��������� �����
    private void GenerateAllTiles(float tileSize, int numberOfTilesX, int numberOfTilesY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((numberOfTilesX / 2) * tileSize, 0, (numberOfTilesX / 2) * tileSize) + boardCenter; //���������� ������� �����
        //������� ������
        tiles = new GameObject[numberOfTilesX, numberOfTilesY];
        for (int x = 0; x < numberOfTilesX; x++)
            for (int y = 0; y < numberOfTilesY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }
    //��������� ����� ������
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y)); //������� ������
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh(); //������� �����
        tileObject.AddComponent<MeshFilter>().mesh = mesh; //��������� ����� �� ������
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial; //��������� �� ������ ��������

        Vector3[] vertices = new Vector3[4]; //������ ���������������, ������� 4 �������� � ������� (������ ������� - �������)
        //��������� ������ �������
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        //��������� ������������
        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals(); //������� ������� � ������� ������������� � ������, ��������� �����

        tileObject.layer = LayerMask.NameToLayer("Tile"); //����������� ������ ���� ������
        tileObject.AddComponent<BoxCollider>(); //������� ���������, ����� ������ �� �������� ���� �� ����� 

        return tileObject; //���������� ������
    }

    //����� �����
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y]; //������� ����� ��� ������

        int whiteTeam = 0, blackTeam = 1; //������ ������� ��������
        //���� ���������� ������ ������ ���� ������ � �������� �� �� �����
        //�����
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < NUMBER_OF_TILES_X; i++)
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);

        //������
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < NUMBER_OF_TILES_X; i++)
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
    }
    //��������� ���� ������
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>(); //������� ������ � ������� �������� � ������ ChessPiece

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team]; //��������� ������ �������� ��������������� �������

        return cp; //���������� ������
    }

    //������������ �����
    private void PositionAllPieces()
    {
        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true); //��� ������ ������, ���� � ��� ��������� ������, ���������� �� ��������� �� ������
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x; //�������� ������� ��������� �� ��� x
        chessPieces[x, y].currentY = y; //�������� ������� ��������� �� ��� y
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force); //�������� ������ ���������� ������

    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2); //���� �������� ������, ����������� �� ����� �������� ���������
    }

    //����������� ����
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant) //������ �� �������
        {
            var newMove = moveList[moveList.Count - 1]; //���������� ���
            var targetPawnPosition = moveList[moveList.Count - 2]; //����- ���������� ���
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y]; //����� �����, ���� ������� ����� ����- ���������� �����
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y]; //�����, ���� ������� ����� ���������� �����

            if (myPawn.currentX == enemyPawn.currentX) //���� ����� ��������� �� ����� �������� �� ��� x (�� ����� �����������)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1) //���� ��������� ����� ������ ��� ����� ����� �����
                {
                    if (enemyPawn.team == 0) //���� ��������� ����� ���� �����
                    {
                        deadWhites.Add(enemyPawn); //��������� �� � ������ ��������� �����
                        enemyPawn.SetScale(Vector3.one * deathSize); //������ ������
                        enemyPawn.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadWhites.Count); //���������� � ������ �����
                    }
                    else //���������� ��� ������ �������
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null; //������ � ������, ��� ������ ��������� �����, ��� ������
                }
            }
        }

        if (specialMove == SpecialMove.Castling) //���������
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; //���������� ���

            if (lastMove[1].x == 2) //���� ��������� ���� ����� 
            {
                if (lastMove[1].y == 0) //���� ������������ �����
                {
                    chessPieces[3, 0] = chessPieces[0, 0];
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                    //������ ��������� �����
                }
                else if (lastMove[1].y == 7) //���������� ��� ������
                {
                    chessPieces[3, 7] = chessPieces[0, 7];
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }

            else if (lastMove[1].x == 6) // ��� ��������� ������ ���������� 
            {
                if (lastMove[1].y == 0)
                {
                    chessPieces[5, 0] = chessPieces[7, 0];
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7)
                {
                    chessPieces[5, 7] = chessPieces[7, 7];
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }

        if (specialMove == SpecialMove.Promotion) //����������� ����� 
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; //���������� ���
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y]; //������ �����

            if (targetPawn.team == 0 && lastMove[1].y == 7) //���� ����� ����� � ����� �� �����
            {
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                //������ ����� �� ����� � ��� �� �������, ��� ��� ���� ������
            }
            if (targetPawn.team == 1 && lastMove[1].y == 0) //���������� ��� ������ �������
            {
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            }
        }
    }

    private void PreventCheck() //�������������� ����, ������� �������� ������
    {
        ChessPiece targetKing = null; //���������� ������ ���

        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (chessPieces[x, y] != null) //���� ������ �� ������ ����
                    if (chessPieces[x, y].type == ChessPieceType.King) //���� ��� ������ ������
                        if (chessPieces[x, y].team == currentlyDragging.team) //���� ������ ��� �������, ������� ������ ������ ���
                            targetKing = chessPieces[x, y]; //������ �������

        SimulateMoveForPiece(currentlyDragging, ref availableMoves, targetKing); //���������� ��������� ����
    }

    private void SimulateMoveForPiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        //��������� ��������� ����� ����� ��� ������������
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //��������� �������� �� �����-�� ��� � ����
        for (int i = 0; i < moves.Count; i++)
        {
            int simulationX = moves[i].x;
            int simulationY = moves[i].y;
            //����� ������
            Vector2Int kingPositionRN = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // ���� �� ������� �������
            if (cp.type == ChessPieceType.King)
                kingPositionRN = new Vector2Int(simulationX, simulationY);

            ChessPiece[,] simulation = new ChessPiece[NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y];
            List<ChessPiece> simulationAttackingPieces = new List<ChessPiece>();
            for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            {
                for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = chessPieces[x, y]; //���� ������ ��������� �� �����, ��������� ��
                        if (simulation[x, y].team != cp.team)
                            simulationAttackingPieces.Add(simulation[x, y]); //���� ������ ������ �������, ��������� ����
                    }
                }
            }
            //������������� ����
            simulation[actualX, actualY] = null;
            cp.currentX = simulationX;
            cp.currentY = simulationY;
            simulation[simulationX, simulationY] = cp;
            //���� �� ������� ������
            var deadPiece = simulationAttackingPieces.Find(c => c.currentX == simulationX && c.currentY == simulationY);
            if (deadPiece != null)
                simulationAttackingPieces.Remove(deadPiece);
            //���������� ��� ����� 
            List<Vector2Int> simulationMoves = new List<Vector2Int>();
            for (int a = 0; a < simulationAttackingPieces.Count; a++)
            {
                var pieceMoves = simulationAttackingPieces[a].GetAvailableMoves(ref simulation, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                    simulationMoves.Add(pieceMoves[b]);
            }
            //����� �� ����� ������
            if (ContainsValidMove(ref simulationMoves, kingPositionRN))
            {
                //������� ���� ���
                movesToRemove.Add(moves[i]);
            }

            //���������� �������� ���������
            cp.currentX = actualX;
            cp.currentY = actualY;
        }
        //������� ����
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }

    private bool IsNowCheckmate() //�������� �� ���
    {
        var lastMove = moveList[moveList.Count - 1]; //����� ��������� ���
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0; //���������� �������, ������� ������ �� �����, � ���������� ���������������

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;

        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam) //���� �������, ������� ����� ���������, ������������� ����� ������
                    {
                        defendingPieces.Add(chessPieces[x, y]); //��������� ������
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y]; //�����������, ���� ������ - ������
                    }
                    else
                        attackingPieces.Add(chessPieces[x, y]); //����� ��������� � ������ ������
                }

        //���� �� ������
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y); //�������� ��� ����, ������� ����� ������� ��������� �������
            for (int b = 0; b < pieceMoves.Count; b++)
                currentAvailableMoves.Add(pieceMoves[b]);
        }

        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            //����� �� ������� ���, ����� ��������� ������
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y);
                SimulateMoveForPiece(defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return false;
            }
            return true;
        }
        return false;
    }

    //��������

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y); //���������� ������, ���� ���, ���������� ����� ������, ����� �� ���

        return -Vector2Int.one; //������� �� ����������� (��� ������������)
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true; //���� ����� ������� � ��� ������

        return false;
    }

    //������������ ���� �� ������� ����� �������
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }

    //������� ���������
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }

    private void MoveTo(int startX, int startY, int x, int y)
    {
        ChessPiece cp = chessPieces[startX, startY]; //������, ������� �� �����
        Vector2Int previousPosition = new Vector2Int(startX, startY); //��������� ������� - ������� ������
        //���� �� �� ����� ������� ������ ������
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team) //���� ������� ����� ���������, �� ������� ������
                return;

            //���� ������ ���������
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King) // ���� ������ ������
                    Checkmate(1);
                //��������� ������ � ��������� (���� �����)
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadWhites.Count);
            }
            else //���������� ��� ������ �������
            {
                if (ocp.type == ChessPieceType.King)
                    Checkmate(0);

                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp; //�� ����� ����� ����� ���� ������
        chessPieces[previousPosition.x, previousPosition.y] = null; //�� ����� ������ ������ ��� ������

        PositionSinglePiece(x, y); //���������� ������ 

        isWhiteTurn = !isWhiteTurn; //������ ���
        if (localGame)
            currentTeam = (currentTeam == 0) ? 1 : 0; //� ��������� ���� ������ ������� ������
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) }); //��������� ��������� ���

        ProcessSpecialMove();

        if (currentlyDragging)
            currentlyDragging = null; //���������� ����������� ������ 
        RemoveHighlightTiles(); //���������� ��������� �����

        if (IsNowCheckmate())
            Checkmate(cp.team); //���� ���, ������� �����

        return;
    }

    private void RegisterEvents() //��������� ��������� (�������)
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.IsLocalGame += OnSetLocalGame;

    }

    private void UnregisterEvents() //������� �������
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        GameUI.Instance.IsLocalGame -= OnSetLocalGame;
    }

    private void OnWelcomeServer(NetMessage message, NetworkConnection connection) //��� ����������� � ������� ��� �������
    {
        //���������� ������� ������������� �������
        NetWelcome netWelcome = message as NetWelcome;

        if (localGame)
            netWelcome.AssignedTeam = Random.Range(0, 2); //��� ��������� ���� ������� ���������� ��������
        else
            netWelcome.AssignedTeam = ++playerCount; //��� ���� �� ���� ���, ��� ������� ������ ������ �� �����, ��� ������������ - �� ������

        Server.Instance.SendToClient(connection, netWelcome); //���������� �� ������� ������������ ���������

        if (playerCount == 1) //���� ������� ����� 2 (�.� ���������� ���� -1)
        {
            Server.Instance.Broadcast(new NetStartGame()); //�� ������� ����������� ������ ���� �������������
        }
    }

    private void OnWelcomeClient(NetMessage message) //��� ����������� � ������� ��� ������������
    {
        NetWelcome netWelcome = message as NetWelcome;

        currentTeam = netWelcome.AssignedTeam; //����������� ������� ������

        if (localGame)
            Server.Instance.Broadcast(new NetStartGame()); //���� ���� ���������, ��������� ������ 1 �����, � �� ����� ����� ��������
    }

    private void OnStartGameClient(NetMessage message) //��� ������ ���� ��� ������
    {
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? cameraAngle.whiteTeam : cameraAngle.blackTeam); //������ ��������� ������ � ����������� �� �������
    }

    private void OnSetLocalGame(bool value) //��� �������� ��������� ���� �������� ���������� ������� � �������
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = value;
    }

    private void OnMakeMoveServer(NetMessage message, NetworkConnection connection) //��� ���������� ���� ��� �������
    {
        NetMakeMove makeMove = message as NetMakeMove;
        Server.Instance.Broadcast(makeMove); //����������� ���
    }

    private void OnMakeMoveClient(NetMessage message) //��� ���������� ���� ��� ������
    {
        NetMakeMove makeMove = message as NetMakeMove;

        if (makeMove.team == currentTeam) //���� ������� ����
        {
            ChessPiece target = chessPieces[makeMove.startX, makeMove.startY]; //������ ������� �����
            availableMoves = target.GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y); //���� ��� ��������� ����
            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves); //���� ��� ��������� ����������� ����

            MoveTo(makeMove.startX, makeMove.startY, makeMove.endX, makeMove.endY); //����������� ������
        }
    }

    private void OnRematchServer(NetMessage message, NetworkConnection connection) //��� ������� ��� �������
    {
        Server.Instance.Broadcast(message); //����������� ���������
    }

    private void OnRematchClient(NetMessage message) //��� ������� ��� ������
    {
        NetRematch rematch = message as NetRematch;

        playerWantsRematch[rematch.team] = rematch.wantRematch == 1; //����� ������� ����� �� ������ ����� ������
        if (!localGame)
        {
            if (rematch.team != currentTeam)
            {
                rematchIndicator.transform.GetChild((rematch.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true); //���������� ������� ������ ��������� � �������
                if (rematch.wantRematch != 1)
                    rematchButton.interactable = false; //������ ������ �� ������, ���� ������ ����� ������� ����
            }
        }

        if (playerWantsRematch[0] && playerWantsRematch[1])
            GameReset(); //���� ��� ������ ����� ������ ���, ���� ���������������
    }

    //���
    private void Checkmate(int team)
    {
        DisplayVictory(team); //���� ����� ������ ���, ������������ ����� ������
    }

    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true); //���������� ����� ������
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true); //���������� ��������� � ���, ��� ������� (������� ��������� ���) ��������
    }

    public void OnRematchButton()
    {
        if (localGame) //���� ���� ���������
        {
            NetRematch whiteRematch = new NetRematch();
            whiteRematch.team = 0;
            whiteRematch.wantRematch = 1; //����� ����� ����������
            Client.Instance.SendToServer(whiteRematch); //���������� �� ������ ���������� � ���, ��� ����� ����������
            //���������� ��� ������ �������
            NetRematch blackRematch = new NetRematch();
            blackRematch.team = 1;
            blackRematch.wantRematch = 1;
            Client.Instance.SendToServer(blackRematch);
        }
        else //����������, ������ ���� ����� ����� ����������
        {
            NetRematch rematch = new NetRematch();
            rematch.team = currentTeam;
            rematch.wantRematch = 1;
            Client.Instance.SendToServer(rematch);
        }
    }

    public void OnMenuButton() //��� ������� �� ������ ����
    {
        NetRematch rematch = new NetRematch();
        rematch.team = currentTeam;
        rematch.wantRematch = 0; //������ �� ����� ����������, ��� ��� ����������
        Client.Instance.SendToServer(rematch); //���������� �� ������ ����������
        //����������� ����, �������� �� � �������� ���������� ������� � �������
        GameReset();
        GameUI.Instance.OnLeaveTheGameButton();

        Invoke("ShutdownRelay", 1.0f);

        playerCount = -1;
        currentTeam = -1;
    }

    public void GameReset() //��������� ����
    {
        rematchButton.interactable = true; //����� �������� �� ������ ����������

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false); //������� ��������� 
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false); //���
        //���������
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false); //������� ��������� � ������
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false); //������� ����� � ������

        //������� �����
        currentlyDragging = null; //�� ����������� ������
        availableMoves.Clear(); //��� ��������� �����
        moveList.Clear(); //��� ���� �������
        playerWantsRematch[0] = playerWantsRematch[1] = false; //������ �� ����� ���������� (�� �������� ��������)
        //������� �� �����
        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
        {
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject); //������� ��� ������

                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject); //������� ��� ������� ������ ��� �����
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject); //��� ������

        deadBlacks.Clear();
        deadWhites.Clear();
        //������ ������� ������ � ������ ��
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }

    private void ShutdownRelay() //��������� � ������ � �������
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

}
