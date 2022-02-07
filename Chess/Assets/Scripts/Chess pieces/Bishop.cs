using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int numberOfTilesX, int numberOfTilesY)
    {
        List<Vector2Int> r = new List<Vector2Int>(); // объявляю вектор с двумя значениями - номер клетки по оси x и y соответственно
        //далее все циклы аналогичны этому 
        //вправо вверх
        for (int x = currentX + 1, y = currentY + 1; x < numberOfTilesX && y < numberOfTilesY; x++, y++)
        {
            if (board[x, y] == null) //если нет фигур на данной клетке
                r.Add(new Vector2Int(x, y)); //добавляем возможный ход
            else
            {
                if (board[x, y].team != team) //если фигура на клетке вражеская
                    r.Add(new Vector2Int(x, y)); //добавляем ход

                break; //не проверняем следующие клетки т.к. нельзя ходить за фигуру, которую можно съесть
            }
        }
        //влево вверх
        for (int x = currentX - 1, y = currentY + 1; x >= 0 && y < numberOfTilesY; x--, y++)
        {
            if (board[x, y] == null)
                r.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    r.Add(new Vector2Int(x, y));

                break;
            }
        }
        //вправо вниз
        for (int x = currentX + 1, y = currentY - 1; x < numberOfTilesX && y >= 0; x++, y--)
        {
            if (board[x, y] == null)
                r.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    r.Add(new Vector2Int(x, y));

                break;
            }
        }
        //влево вниз
        for (int x = currentX - 1, y = currentY - 1; x >= 0 && y >= 0; x--, y--)
        {
            if (board[x, y] == null)
                r.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    r.Add(new Vector2Int(x, y));

                break;
            }
        }

        return r; //возвращаем все возможные ходы
    }
}
