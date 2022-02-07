using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int numberOfTilesX, int numberOfTilesY) //используем метод, написанный в скрипте chessPiece
    {
        List<Vector2Int> r = new List<Vector2Int>();
        //все вариации написаны аналагично первой
        //вправо + диагонали
        if (currentX + 1 < numberOfTilesX) //если справа есть место 
        {
            //добавляем ход вправо если клетка свободна или занята вражеской фигурой
            if (board[currentX + 1, currentY] == null)
                r.Add(new Vector2Int(currentX + 1, currentY));
            else if (board[currentX + 1, currentY].team != team)
                r.Add(new Vector2Int(currentX + 1, currentY));
            //добавляем ход вправо вверх
            if (currentY + 1 < numberOfTilesY)
                if (board[currentX + 1, currentY + 1] == null)
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
                else if (board[currentX + 1, currentY + 1].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY + 1));
            //ход вправо вниз
            if (currentY - 1 >= 0)
                if (board[currentX + 1, currentY - 1] == null)
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
                else if (board[currentX + 1, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX + 1, currentY - 1));
        }


        //влево + диагонали
        if (currentX - 1 >= 0)
        {

            if (board[currentX - 1, currentY] == null)
                r.Add(new Vector2Int(currentX - 1, currentY));
            else if (board[currentX - 1, currentY].team != team)
                r.Add(new Vector2Int(currentX - 1, currentY));

            if (currentY + 1 < numberOfTilesY)
                if (board[currentX - 1, currentY + 1] == null)
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));
                else if (board[currentX - 1, currentY + 1].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY + 1));

            if (currentY - 1 >= 0)
                if (board[currentX - 1, currentY - 1] == null)
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
                else if (board[currentX - 1, currentY - 1].team != team)
                    r.Add(new Vector2Int(currentX - 1, currentY - 1));
        }

        //вверх
        if (currentY + 1 < numberOfTilesY)
        {
            if (board[currentX, currentY + 1] == null)
                r.Add(new Vector2Int(currentX, currentY + 1));
            else if (board[currentX, currentY + 1].team != team)
                r.Add(new Vector2Int(currentX, currentY + 1));
        }

        //вниз
        if (currentY - 1 >= 0)
        {
            if (board[currentX, currentY - 1] == null)
                r.Add(new Vector2Int(currentX, currentY - 1));
            else if (board[currentX, currentY - 1].team != team)
                r.Add(new Vector2Int(currentX, currentY - 1));
        }

        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)//смотрим, возможна ли рокировка
    {
        SpecialMove r = SpecialMove.None; //присваиваем none чтобы вернуть, если рокировка недоступна

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7)); //смотрим, ходил ли король
        var leftRook = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7)); //смотрим, ходила ли левая ладья
        var rightRook = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7)); //правая ладья
        //при проверке ищем все ходы, совершенные от положения фигуры по x (для обеих команд одинаковое) и по y (для белых - 0, для черных - 7)
        if (kingMove == null) //если король не ходил, т.к. если ходила одна из ладей, но король не ходил, то возможна рокировка в другую сторону
        {
            if (team == 0) //для белых
            {
                if (leftRook == null) //если левая ладья не ходила
                    if (board[0, 0].type == ChessPieceType.Rook) //если в клетке действительно находится ладья (чтобы нельзя было рокироваться с фигурой, съевшей ладью)
                        if (board[0, 0].team == 0) //проверка, действительно ли в клетке ладья белых (не съела ли нашу ладью вражеская)
                            if (board[3, 0] == null)
                                if (board[2, 0] == null)
                                    if (board[1, 0] == null) //пустые ли клетки между королем и ладьей
                                    {
                                        availableMoves.Add(new Vector2Int(2, 0)); //добавляем возможный ход
                                        r = SpecialMove.Castling; //добавляем специальный ход
                                    }
                //далее все аналогично
                if (rightRook == null)
                    if (board[7, 0].type == ChessPieceType.Rook)
                        if (board[7, 0].team == 0)
                            if (board[6, 0] == null)
                                if (board[5, 0] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 0));
                                    r = SpecialMove.Castling;
                                }
            }
            else
            {
                if (leftRook == null)
                    if (board[0, 7].type == ChessPieceType.Rook)
                        if (board[0, 7].team == 1)
                            if (board[3, 7] == null)
                                if (board[2, 7] == null)
                                    if (board[1, 7] == null)
                                    {
                                        availableMoves.Add(new Vector2Int(2, 7));
                                        r = SpecialMove.Castling;
                                    }

                if (rightRook == null)
                    if (board[7, 7].type == ChessPieceType.Rook)
                        if (board[7, 7].team == 1)
                            if (board[6, 7] == null)
                                if (board[5, 7] == null)
                                {
                                    availableMoves.Add(new Vector2Int(6, 7));
                                    r = SpecialMove.Castling;
                                }
            }

        }
        //не делаем проверку на цвет и тип короля, т.к. нашего короля не могут съесть (иначе закончится игра)
        return r; // возвращаем возможные ходы
    }
}
