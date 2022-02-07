using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int numberOfTilesX, int numberOfTilesY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1; //пешка ходит вперед или назад (относительно оси доски) в зависимости от команды

        //вперед на клетку
        if (board[currentX, currentY + direction] == null) //если клетка впереди свободна
            r.Add(new Vector2Int(currentX, currentY + direction));

        //две клетки вперед 
        if (board[currentX, currentY + direction] == null)
        {
            //белые
            if (team == 0 && currentY == 1 && board[currentX, currentY + direction * 2] == null) //если две клтеки впереди свободны и пешка еще не ходила
                r.Add(new Vector2Int(currentX, currentY + direction * 2));
            //черные
            if (team == 1 && currentY == 6 && board[currentX, currentY + direction * 2] == null)
                r.Add(new Vector2Int(currentX, currentY + direction * 2));
        }

        //едим фигуру
        if (currentX != numberOfTilesX - 1) //если пешка стоит не у края доски
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team) //если справа от пешки (относительно оси x) нет фигуры или ее цвет другой
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
        if (currentX != 0)
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team) //если слева нет фигуры или ее цвет другой
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
        return r;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        int direction = (team == 0) ? 1 : -1; //направление ходов пешки

        if ((team == 0 && currentY == 6) || (team == 1 && currentY == 1)) //если пешка следующим шагом встанет на край доски
            return SpecialMove.Promotion; //превращение пешки

        //взятие на проходе
        if (moveList.Count > 0)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1]; //берем последний ход
            if (board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn) //если он был сделан пешкой
            {
                if (Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2) //если она сходила на две пешки впере
                {
                    if (board[lastMove[1].x, lastMove[1].y].team != team) //если пешка находится в другой команде
                    {
                        if (lastMove[1].y == currentY) //если вражеская пешка стоит рядом с нашей пешкой
                        {
                            if (lastMove[1].x == currentX - 1) //если пешка справа от нашей
                            {
                                availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));//есть возможность съесть пешку
                                return SpecialMove.EnPassant;//можно совершить взятие на проходе
                            }
                            if (lastMove[1].x == currentX + 1) //если пешка слева
                            {
                                availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                                return SpecialMove.EnPassant;
                            }
                        }
                    }
                }
            }

        }

        return SpecialMove.None;
    }
}
