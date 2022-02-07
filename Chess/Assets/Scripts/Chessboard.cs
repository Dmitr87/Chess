using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using UnityEngine.UI;

public enum SpecialMove //все возможные специальные ходы 
{
    None = 0,
    EnPassant = 1,
    Castling = 2,
    Promotion = 3
}

public class Chessboard : MonoBehaviour
{
    [SerializeField] private Material tileMaterial; //материал, из которого состоят плитки (прозрачный)
    [SerializeField] private float tileSize = 1.0f; //размер плиток
    [SerializeField] private float yOffset = 0.2f; //насколько плитки выше текстуры доски
    [SerializeField] private Vector3 boardCenter = Vector3.zero; //центр доски
    [SerializeField] private float deathSize = 0.7f; //размер фигуры после смерти
    [SerializeField] private float deathSpacing = 0.525f; //расстояние между мертвыми фигурами, когда они стоят по бокам доски
    [SerializeField] private float dragOffset = 1.0f; //
    [SerializeField] private GameObject victoryScreen; //экран, появляющийся после мата
    [SerializeField] private Transform rematchIndicator; //надпись, отображающаяся после того, как противник покинул игру или нажал кнопку rematch
    [SerializeField] private Button rematchButton; //кнопка rematch

    [SerializeField] private GameObject[] prefabs; //префабы (заготовки моделей)
    [SerializeField] private Material[] teamMaterials; //материалы для фигур 

    //логика
    private List<Vector2Int> availableMoves = new List<Vector2Int>(); //список возможных ходов
    private List<ChessPiece> deadWhites = new List<ChessPiece>(); //мертвые фигуры белых
    private List<ChessPiece> deadBlacks = new List<ChessPiece>(); //мертвые фигуры черных
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>(); //список ходов
    private SpecialMove specialMove; //специальный ход (один из)
    private const int NUMBER_OF_TILES_X = 8; //количество клеток по оси х (горизонталь)
    private const int NUMBER_OF_TILES_Y = 8; //количество клеток по оси y (вертикаль)
    private bool isWhiteTurn; //сейчас ход белых или нет
    private ChessPiece currentlyDragging; //фигура которую мы передвигаем сейчас
    private ChessPiece[,] chessPieces; //все фигуры
    private GameObject[,] tiles; //клетки доски
    private Camera currentCamera; //камера, через которую мы смотрим сейчас
    private Vector2Int currentHover; //клетка, на которую мы сейчас навели курсор
    private Vector3 bounds; //границы доски
    //для игры по сети
    private int playerCount = -1; //количество игроков (счет начинается с 0 когда подключается первый игрок)
    private int currentTeam = -1; //команда, которая сейчас делает ход
    private bool localGame = true; //локальная игра или нет
    private bool[] playerWantsRematch = new bool[2]; //список из 2 элементов, в каждом содержится информация, хочет ли игрок под номером (индекс массива) играть заново или нет (да - 1, нет - 0)

    private void Start() // при запуске кода
    {
        isWhiteTurn = true; //игра всегда начинается с хода белых

        GenerateAllTiles(tileSize, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y); //создаем доску

        SpawnAllPieces(); //появляются все фигуры
        PositionAllPieces(); //расставляем фигуры в правильном порядке

        RegisterEvents();
    }

    private void Update()
    {
        if (!currentCamera) //если камера не выставлена
        {
            currentCamera = Camera.main; //выставляем каиеру
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); //луч который идет через камеру в место курсора
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            //получение индекса плитки, на которую нажал
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //если навожу на плитку не сдругой плитки
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition; //подсвечиваем ту плитку, которую бьем
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                //присваиваю нашему слою слой наведения
            }
            //если перевожу с плитки на плитку
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                //если сходить в клетку можно, то слой будет слоем наведения, иначе просто плиткой
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                //переприсваиваю слой наведения
            }

            //когда нажимаем кнопку мыши
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    //наш ход или нет
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1)
                        || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && localGame) || (chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && localGame))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        //получаю список возможных ходов и подсвечиваю клетки
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y);
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck();
                        HighlightTiles();
                    }
                }
            }

            //когда отпускаем кнопку мыши
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY); //предыдущая позиция является текущей позицией для фигуры которую передвинули

                if (ContainsValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y))) //если ход который мы сделали возможен
                {
                    MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    NetMakeMove makeMove = new NetMakeMove();
                    makeMove.startX = previousPosition.x;
                    makeMove.startY = previousPosition.y;
                    makeMove.endX = hitPosition.x;
                    makeMove.endY = hitPosition.y;
                    makeMove.team = (currentTeam == 0) ? 1 : 0;
                    Client.Instance.SendToServer(makeMove); //делаем ход и отправляем его на сервер
                }
                else //иначе
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y)); //возвращаем фигуру назад
                    currentlyDragging = null; //не передвигаем никакие фигуры
                    RemoveHighlightTiles(); //не подсвечиваем никакие клетки
                }
            }
        }
        else //аналогично предыдущей части
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

        //если держим фигуру в руке
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    //отрисовка доски
    private void GenerateAllTiles(float tileSize, int numberOfTilesX, int numberOfTilesY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((numberOfTilesX / 2) * tileSize, 0, (numberOfTilesX / 2) * tileSize) + boardCenter; //определяем границы доски
        //создаем плитки
        tiles = new GameObject[numberOfTilesX, numberOfTilesY];
        for (int x = 0; x < numberOfTilesX; x++)
            for (int y = 0; y < numberOfTilesY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }
    //отрисовка одной клетки
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y)); //создаем плитку
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh(); //создаем сетку
        tileObject.AddComponent<MeshFilter>().mesh = mesh; //добавляем сетку на плитку
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial; //добавляем на плитку материал

        Vector3[] vertices = new Vector3[4]; //плитка четырехугольная, поэтому 4 элемента в массиве (каждый элемент - вершина)
        //добавляем каждую вершину
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        //добавляем треугольники
        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals(); //считаем нормали с помощью треугольников и вершин, созданных ранее

        tileObject.layer = LayerMask.NameToLayer("Tile"); //присваиваем плитке слой плитки
        tileObject.AddComponent<BoxCollider>(); //создаем коллайдер, чтобы клетки не налезали друг на друга 

        return tileObject; //возвращаем плитку
    }

    //спавн фигур
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y]; //создаем место под фигуры

        int whiteTeam = 0, blackTeam = 1; //задаем индексы командам
        //ниже присваивам каждой клетке свою фигуру и помещаем ее на доску
        //белые
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

        //черные
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
    //добавляем одну фигуру
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>(); //создаем фигуру с помощью префабов и метода ChessPiece

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team]; //добавляем фигуре материал соответствующей команды

        return cp; //возвращаем фигуру
    }

    //расположение фигур
    private void PositionAllPieces()
    {
        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true); //для каждой клетки, если в ней находится фигура, выставляем ее правильно на клетку
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x; //получаем текущее положение по оси x
        chessPieces[x, y].currentY = y; //получаем текущее положение по оси y
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force); //помещаем фигуру посередине клетки

    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2); //ищем середину клетки, основываясь на ранее заданных значениях
    }

    //специальные ходы
    private void ProcessSpecialMove()
    {
        if (specialMove == SpecialMove.EnPassant) //взятие на проходе
        {
            var newMove = moveList[moveList.Count - 1]; //предыдущий ход
            var targetPawnPosition = moveList[moveList.Count - 2]; //пред- предыдущий ход
            ChessPiece enemyPawn = chessPieces[targetPawnPosition[1].x, targetPawnPosition[1].y]; //берем место, куда сходила пешка пред- предыдущим ходом
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y]; //место, куда сходила пешка предыдущим ходом

            if (myPawn.currentX == enemyPawn.currentX) //если пешки находятся на одном значении по оси x (на одной горизонтали)
            {
                if (myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1) //если вражеская пешка справа или слева нашей пешки
                {
                    if (enemyPawn.team == 0) //если съеденная пешка былп белой
                    {
                        deadWhites.Add(enemyPawn); //добавляем ее в список съеденных фигур
                        enemyPawn.SetScale(Vector3.one * deathSize); //меняем размер
                        enemyPawn.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadWhites.Count); //перемещаем в нужное место
                    }
                    else //аналогично для другой команды
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize) - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null; //теперь в клетке, где стояла съеденная пешка, нет фигуры
                }
            }
        }

        if (specialMove == SpecialMove.Castling) //рокировка
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; //предыдущий ход

            if (lastMove[1].x == 2) //если рокировка была влево 
            {
                if (lastMove[1].y == 0) //если рокировались белые
                {
                    chessPieces[3, 0] = chessPieces[0, 0];
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                    //меняем положение ладьи
                }
                else if (lastMove[1].y == 7) //аналогично для черных
                {
                    chessPieces[3, 7] = chessPieces[0, 7];
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }

            else if (lastMove[1].x == 6) // для рокировки вправо аналогично 
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

        if (specialMove == SpecialMove.Promotion) //превращение пешки 
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; //предыдущий ход
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y]; //нужная пешка

            if (targetPawn.team == 0 && lastMove[1].y == 7) //если пешка белая и дошла до верха
            {
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                //меняем пешку на ферзя в той же позиции, где она была раньше
            }
            if (targetPawn.team == 1 && lastMove[1].y == 0) //аналогично для черной команды
            {
                ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                PositionSinglePiece(lastMove[1].x, lastMove[1].y);
            }
        }
    }

    private void PreventCheck() //предотвращение хода, который подствит короля
    {
        ChessPiece targetKing = null; //изначально короля нет

        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (chessPieces[x, y] != null) //если фигура на клетке есть
                    if (chessPieces[x, y].type == ChessPieceType.King) //если эта фигура король
                        if (chessPieces[x, y].team == currentlyDragging.team) //если король той команды, которая сейчас делает ход
                            targetKing = chessPieces[x, y]; //король искомый

        SimulateMoveForPiece(currentlyDragging, ref availableMoves, targetKing); //симулируем возможные ходы
    }

    private void SimulateMoveForPiece(ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)
    {
        //сохраняем положение чтобы потом его восстановить
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        //проверяем приводит ли какой-то ход к шаху
        for (int i = 0; i < moves.Count; i++)
        {
            int simulationX = moves[i].x;
            int simulationY = moves[i].y;
            //общий случай
            Vector2Int kingPositionRN = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // если мы сходили королем
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
                        simulation[x, y] = chessPieces[x, y]; //если фигура находится на доске, добавляем ее
                        if (simulation[x, y].team != cp.team)
                            simulationAttackingPieces.Add(simulation[x, y]); //если фигура другой команды, добавляем сюда
                    }
                }
            }
            //симулирование хода
            simulation[actualX, actualY] = null;
            cp.currentX = simulationX;
            cp.currentY = simulationY;
            simulation[simulationX, simulationY] = cp;
            //была ли съедена фигура
            var deadPiece = simulationAttackingPieces.Find(c => c.currentX == simulationX && c.currentY == simulationY);
            if (deadPiece != null)
                simulationAttackingPieces.Remove(deadPiece);
            //симулируем все атаки 
            List<Vector2Int> simulationMoves = new List<Vector2Int>();
            for (int a = 0; a < simulationAttackingPieces.Count; a++)
            {
                var pieceMoves = simulationAttackingPieces[a].GetAvailableMoves(ref simulation, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                    simulationMoves.Add(pieceMoves[b]);
            }
            //могут ли убить короля
            if (ContainsValidMove(ref simulationMoves, kingPositionRN))
            {
                //убираем этот ход
                movesToRemove.Add(moves[i]);
            }

            //возвращаем исходное положение
            cp.currentX = actualX;
            cp.currentY = actualY;
        }
        //убираем ходы
        for (int i = 0; i < movesToRemove.Count; i++)
        {
            moves.Remove(movesToRemove[i]);
        }
    }

    private bool IsNowCheckmate() //проверка на мат
    {
        var lastMove = moveList[moveList.Count - 1]; //берем последний ход
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0) ? 1 : 0; //определяем команду, которая ходила до этого, и возвращаем противоположную

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;

        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (chessPieces[x, y] != null)
                {
                    if (chessPieces[x, y].team == targetTeam) //если команда, которая может проиграть, соответствует цвету фигуры
                    {
                        defendingPieces.Add(chessPieces[x, y]); //добавляем фигуру
                        if (chessPieces[x, y].type == ChessPieceType.King)
                            targetKing = chessPieces[x, y]; //присваиваем, если фигура - король
                    }
                    else
                        attackingPieces.Add(chessPieces[x, y]); //иначе добавляем в другой список
                }

        //бьют ли короля
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for (int i = 0; i < attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y); //получаем все ходы, которые может сделать атакующая команда
            for (int b = 0; b < pieceMoves.Count; b++)
                currentAvailableMoves.Add(pieceMoves[b]);
        }

        if (ContainsValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY)))
        {
            //можно ли сделать ход, чтобы заслонить короля
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

    //операции

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y); //возвращаем плитку, если луч, проходящий через камеру, дошел до нее

        return -Vector2Int.one; //никогда не выполняется (для подстраховки)
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true; //если можно сходить в эту клетку

        return false;
    }

    //подсвечиваем поля на которые можно сходить
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }

    //убираем подсветку
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }

    private void MoveTo(int startX, int startY, int x, int y)
    {
        ChessPiece cp = chessPieces[startX, startY]; //фигура, которую мы берем
        Vector2Int previousPosition = new Vector2Int(startX, startY); //начальная позиция - позиция фигуры
        //ести ли на месте нажатия другая фигура
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (cp.team == ocp.team) //если команды фигур совпадают, то сходить нельзя
                return;

            //если фигура вражеская
            if (ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King) // если фигура король
                    Checkmate(1);
                //добавляем фигуру к съеденным (было ранее)
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize) - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadWhites.Count);
            }
            else //аналогично для другой команды
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

        chessPieces[x, y] = cp; //на новом месте стоит наша фигура
        chessPieces[previousPosition.x, previousPosition.y] = null; //на месте фигуры теперь нет ничего

        PositionSinglePiece(x, y); //перемещаем фигуру 

        isWhiteTurn = !isWhiteTurn; //меняем ход
        if (localGame)
            currentTeam = (currentTeam == 0) ? 1 : 0; //в локальной игре меняем команду игрока
        moveList.Add(new Vector2Int[] { previousPosition, new Vector2Int(x, y) }); //добавляем возможный ход

        ProcessSpecialMove();

        if (currentlyDragging)
            currentlyDragging = null; //сбрасываем перемещение фигуры 
        RemoveHighlightTiles(); //сбрасываем подсветку полей

        if (IsNowCheckmate())
            Checkmate(cp.team); //если мат, выводим экран

        return;
    }

    private void RegisterEvents() //добавляем сообщения (события)
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

    private void UnregisterEvents() //убираем события
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

    private void OnWelcomeServer(NetMessage message, NetworkConnection connection) //при подключении к серверу для сервера
    {
        //присвоение команды подключенному клиенту
        NetWelcome netWelcome = message as NetWelcome;

        if (localGame)
            netWelcome.AssignedTeam = Random.Range(0, 2); //при локальной игре команда выбирается случайно
        else
            netWelcome.AssignedTeam = ++playerCount; //при игре по сети тот, кто создает сервер играет за белых, кто подключается - за черных

        Server.Instance.SendToClient(connection, netWelcome); //отправляем от сервера пользователю сообщение

        if (playerCount == 1) //если игроков стало 2 (т.к изначально было -1)
        {
            Server.Instance.Broadcast(new NetStartGame()); //от сервера транслируем начало игры пользователям
        }
    }

    private void OnWelcomeClient(NetMessage message) //при подключении к серверу для пользователя
    {
        NetWelcome netWelcome = message as NetWelcome;

        currentTeam = netWelcome.AssignedTeam; //присваиваем команду игроку

        if (localGame)
            Server.Instance.Broadcast(new NetStartGame()); //если игра локальная, требуется только 1 игрок, и мы можем сразу начинать
    }

    private void OnStartGameClient(NetMessage message) //при начале игры для игрока
    {
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? cameraAngle.whiteTeam : cameraAngle.blackTeam); //меняем положение камеры в зависимости от команды
    }

    private void OnSetLocalGame(bool value) //при создании локальной игры обнуляем количество игроков и команду
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = value;
    }

    private void OnMakeMoveServer(NetMessage message, NetworkConnection connection) //при совершении хода для сервера
    {
        NetMakeMove makeMove = message as NetMakeMove;
        Server.Instance.Broadcast(makeMove); //транслируем ход
    }

    private void OnMakeMoveClient(NetMessage message) //при совершении хода для игрока
    {
        NetMakeMove makeMove = message as NetMakeMove;

        if (makeMove.team == currentTeam) //если команда наша
        {
            ChessPiece target = chessPieces[makeMove.startX, makeMove.startY]; //фигура которая ходит
            availableMoves = target.GetAvailableMoves(ref chessPieces, NUMBER_OF_TILES_X, NUMBER_OF_TILES_Y); //ищем все возможные ходы
            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves); //ищем все возможные специальные ходы

            MoveTo(makeMove.startX, makeMove.startY, makeMove.endX, makeMove.endY); //передвигаем фигуру
        }
    }

    private void OnRematchServer(NetMessage message, NetworkConnection connection) //при реванше для сервера
    {
        Server.Instance.Broadcast(message); //транслируем сообщение
    }

    private void OnRematchClient(NetMessage message) //при реванше для игрока
    {
        NetRematch rematch = message as NetRematch;

        playerWantsRematch[rematch.team] = rematch.wantRematch == 1; //игрок который нажал на кнопку хочет реванш
        if (!localGame)
        {
            if (rematch.team != currentTeam)
            {
                rematchIndicator.transform.GetChild((rematch.wantRematch == 1) ? 0 : 1).gameObject.SetActive(true); //показываем другому игроку сообщение о реванше
                if (rematch.wantRematch != 1)
                    rematchButton.interactable = false; //нельзя нажать на кнопку, если другой игрок покинул игру
            }
        }

        if (playerWantsRematch[0] && playerWantsRematch[1])
            GameReset(); //если оба игрока ходят играть еще, игра перезапускается
    }

    //мат
    private void Checkmate(int team)
    {
        DisplayVictory(team); //если игрок сделал мат, показывается экран победы
    }

    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true); //показываем экран победы
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true); //показываем сообщение о том, что команда (которая поставила мат) выиграла
    }

    public void OnRematchButton()
    {
        if (localGame) //если игра локальная
        {
            NetRematch whiteRematch = new NetRematch();
            whiteRematch.team = 0;
            whiteRematch.wantRematch = 1; //белые хотят переиграть
            Client.Instance.SendToServer(whiteRematch); //отправляем на сервер информацию о том, что хотим переиграть
            //аналогично для другой команды
            NetRematch blackRematch = new NetRematch();
            blackRematch.team = 1;
            blackRematch.wantRematch = 1;
            Client.Instance.SendToServer(blackRematch);
        }
        else //аналогично, только один игрок хочет переиграть
        {
            NetRematch rematch = new NetRematch();
            rematch.team = currentTeam;
            rematch.wantRematch = 1;
            Client.Instance.SendToServer(rematch);
        }
    }

    public void OnMenuButton() //при нажатии на кнопку меню
    {
        NetRematch rematch = new NetRematch();
        rematch.team = currentTeam;
        rematch.wantRematch = 0; //игроть не хочет переиграть, так как отключился
        Client.Instance.SendToServer(rematch); //отправляем на сервер информацию
        //заканчиваем игру, покидаем ее и обнуляем количество игроков и команды
        GameReset();
        GameUI.Instance.OnLeaveTheGameButton();

        Invoke("ShutdownRelay", 1.0f);

        playerCount = -1;
        currentTeam = -1;
    }

    public void GameReset() //обнуление игры
    {
        rematchButton.interactable = true; //можно нажимать на кнопку переигрови

        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false); //убираем сообщения 
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false); //оба
        //интерфейс
        victoryScreen.transform.GetChild(0).gameObject.SetActive(false); //убираем сообщения о победе
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false); //убираем экран о победе

        //очистка доски
        currentlyDragging = null; //не передвигаем фигуры
        availableMoves.Clear(); //нет доступных ходов
        moveList.Clear(); //все ходы удаляем
        playerWantsRematch[0] = playerWantsRematch[1] = false; //игроки не хотят переиграть (мы обнулили значения)
        //очистка от фигур
        for (int x = 0; x < NUMBER_OF_TILES_X; x++)
        {
            for (int y = 0; y < NUMBER_OF_TILES_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject); //удаляем все фигуры

                chessPieces[x, y] = null;
            }
        }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject); //удаляем все мертвые фигуры для белых
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject); //для черных

        deadBlacks.Clear();
        deadWhites.Clear();
        //заново создаем фигуры и ставим их
        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }

    private void ShutdownRelay() //выключаем и сервер и клиента
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }

}
