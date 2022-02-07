using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetRematch : NetMessage
{
    public int team; //команда игрока
    public byte wantRematch;//хочет клиент играть заново или нет (нет типа bool для writer)

    public NetRematch()//отправка сообщения
    {
        Code = OperationCode.REMATCH;
    }

    public NetRematch(DataStreamReader reader)//получение сообщения
    {
        Code = OperationCode.REMATCH;
        Deserialize(reader);//распаковка сообщения
    }

    public override void Serialize(ref DataStreamWriter writer)//метод для упаковки
    {
        writer.WriteByte((byte)Code);//записываем название
        writer.WriteInt(team);//команду
        writer.WriteByte(wantRematch);//и хочет ли играть заново
    }

    public override void Deserialize(DataStreamReader reader)//метод для распаковки
    {
        //опять делаем все то же самое, как и в упаковке, но без первой строки
        team = reader.ReadInt();
        wantRematch = reader.ReadByte();
    }

    public override void RecieveOnClient()//получение клиентом сообщения
    {
        NetUtility.C_REMATCH?.Invoke(this);
    }

    public override void RecieveOnServer(NetworkConnection connection)//получение сообщения сервером
    {
        NetUtility.S_REMATCH?.Invoke(this, connection);
    }
}
