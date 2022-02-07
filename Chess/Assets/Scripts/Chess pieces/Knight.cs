using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int numberOfTilesX, int numberOfTilesY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        //все ходы аналогичны первому
        //вправо вверх
        int x = currentX + 1;
        int y = currentY + 2;
        if (x < numberOfTilesX && y < numberOfTilesY) //не выходит ли ход за пределы доски
            if (board[x, y] == null || board[x, y].team != team) //если клетка свободна или там находится вражеская фигура
                r.Add(new Vector2Int(x, y)); // добавляем ход
        //не проверяем наличие фигур между начальной и конечной точкой, так как конь может ходить через другие фигуры
        x = currentX + 2;
        y = currentY + 1;
        if (x < numberOfTilesX && y < numberOfTilesY)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        //влево вверх
        x = currentX - 1;
        y = currentY + 2;
        if (x >= 0 && y < numberOfTilesY)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        x = currentX - 2;
        y = currentY + 1;
        if (x >= 0 && y < numberOfTilesY)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        //вниз вправо
        x = currentX + 1;
        y = currentY - 2;
        if (x < numberOfTilesX && y >= 0)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        x = currentX + 2;
        y = currentY - 1;
        if (x < numberOfTilesX && y >= 0)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        //вниз влево
        x = currentX - 1;
        y = currentY - 2;
        if (x >= 0 && y >= 0)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        x = currentX - 2;
        y = currentY - 1;
        if (x >= 0 && y >= 0)
            if (board[x, y] == null || board[x, y].team != team)
                r.Add(new Vector2Int(x, y));

        return r;
    }
}
