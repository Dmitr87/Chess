using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
} //объявляю все возможные шахматные фигуры

public class ChessPiece : MonoBehaviour
{
    public int team; //цвет фигуры
    public int currentX; //текущее положение по x
    public int currentY; //текущее положение по y

    public ChessPieceType type; //фигура

    private Vector3 desiredPosition; // позиция фигуры
    private Vector3 desiredScale = Vector3.one; //размер которому будет соответствовать фигура

    private void Start()
    {
        transform.rotation = Quaternion.Euler((team == 0) ? Vector3.zero : new Vector3(0, 180, 0));//поворачиваю фигуры (белые остаются на месте, черные поворачиваются на 180 градусов)
        //quaternion.euler принимает 3 значения (z, x, y) и поворачивает по соответствующей оси на заданное количество градусов
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10); //объект (фигура) перемещается от первого значения ко второму с увеличивающейся скоростью
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10); //объект меняет размер (также скорость изменения увеличивается)
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int numberOfTilesX, int numberOfTilesY) //метод для поиска всех возможных ходов 
    {
        //используется и расширяется в других файлах с фигурами
        List<Vector2Int> r = new List<Vector2Int>();

        return r;
    }

    public virtual SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)//ищем все возможные виртуальные ходы (например, рокировка)
    {
        //также расширяется в файлах для пешки и короля
        return SpecialMove.None;
    }

    public virtual void SetPosition(Vector3 position, bool force = false) //перемещаем фигуру
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)//изменяем размер фигуры
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredPosition;
    }
    //если force = false, то фигуры перемещаются и меняют размер плавно, иначе телепортируются (как при реванше)
}
