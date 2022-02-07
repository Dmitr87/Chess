using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetStartGame : NetMessage
{
    public NetStartGame() //отправка сообщения
    {
        Code = OperationCode.START_GAME;
    }

    public NetStartGame(DataStreamReader reader)//получение
    {
        Code = OperationCode.START_GAME;
        Deserialize(reader);//распаковка
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);//упаковка названия сообщения
    }

    public override void Deserialize(DataStreamReader reader)
    {
        //сообщение не содержит ничего кроме названия, так как оно начинает игру независимо от любых других условий
    }

    public override void RecieveOnClient()//получение клиентом сообщения
    {
        NetUtility.C_START_GAME?.Invoke(this);
    }

    public override void RecieveOnServer(NetworkConnection connection)//получение сообщения сервером
    {
        NetUtility.S_START_GAME?.Invoke(this, connection);
    }
}
