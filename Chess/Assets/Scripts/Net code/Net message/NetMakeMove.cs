using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetMakeMove : NetMessage
{
    public int startX;//начальное положение по оси х
    public int startY;//начальное положение по оси y
    public int endX;//конечное положение по оси х
    public int endY;//конечное положение по оси y
    public int team;//команда, совершившая ход

    public NetMakeMove()//отправка сообщения
    {
        Code = OperationCode.MAKE_MOVE;
    }

    public NetMakeMove(DataStreamReader reader)//получение сообщения
    {
        Code = OperationCode.MAKE_MOVE;
        Deserialize(reader);//распаковка сообщения
    }

    public override void Serialize(ref DataStreamWriter writer)//упаковка
    {
        writer.WriteByte((byte)Code);//записываем название сообщения, следующие команды упаковывают соответствующие переменные
        writer.WriteInt(startX);
        writer.WriteInt(startY);
        writer.WriteInt(endX);
        writer.WriteInt(endY);
        writer.WriteInt(team);
    }

    public override void Deserialize(DataStreamReader reader)//распаковка
    {
        //аналогично всем файлам, первый байт информации (название сообщения) мы уже прочитали,начинаем со всторого в том же порядке, что и читаем
        startX = reader.ReadInt();
        startY = reader.ReadInt();
        endX = reader.ReadInt();
        endY = reader.ReadInt();
        team = reader.ReadInt();
    }

    public override void RecieveOnClient()
    {
        NetUtility.C_MAKE_MOVE?.Invoke(this);//клиент получает сообщение
    }

    public override void RecieveOnServer(NetworkConnection connection)
    {
        NetUtility.S_MAKE_MOVE?.Invoke(this, connection);//сервер получает сообщение
    }
}
