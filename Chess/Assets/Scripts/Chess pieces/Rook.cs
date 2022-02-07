using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int numberOfTilesX, int numberOfTilesY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        //вниз
        for (int i = currentY - 1; i >= 0; i--)//все клетки под ладьей
        {
            if (board[currentX, i] == null)//если клетка пустая
                r.Add(new Vector2Int(currentX, i));
            else//если клетка занята
            {
                if (board[currentX, i].team != team)//если в клетке стоит вражеская фигура
                    r.Add(new Vector2Int(currentX, i));

                break;//ладья не может ходить через фигуры
            }
        }

        //вверх
        for (int i = currentY + 1; i < numberOfTilesY; i++) //все клетки над ладьей
        {
            if (board[currentX, i] == null)
                r.Add(new Vector2Int(currentX, i));

            if (board[currentX, i] != null)
            {
                if (board[currentX, i].team != team)
                    r.Add(new Vector2Int(currentX, i));

                break;
            }
        }

        //влево
        for (int i = currentX - 1; i >= 0; i--) //слева от ладьи
        {
            if (board[i, currentY] == null)
                r.Add(new Vector2Int(i, currentY));

            if (board[i, currentY] != null)
            {
                if (board[i, currentY].team != team)
                    r.Add(new Vector2Int(i, currentY));

                break;
            }
        }

        //вправо
        for (int i = currentX + 1; i < numberOfTilesX; i++) //справа
        {
            if (board[i, currentY] == null)
                r.Add(new Vector2Int(i, currentY));

            if (board[i, currentY] != null)
            {
                if (board[i, currentY].team != team)
                    r.Add(new Vector2Int(i, currentY));

                break;
            }
        }

        return r;
    }
}
