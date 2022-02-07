using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetWelcome : NetMessage
{
    public int AssignedTeam { set; get; }//переменная команды публичная, так как понадобится при совершении ходов и выборе позиции камеры

    public NetWelcome()//отправка сообщения
    {
        Code = OperationCode.WELCOME;
    }

    public NetWelcome(DataStreamReader reader)//получение сообщения
    {
        Code = OperationCode.WELCOME;
        Deserialize(reader);//распаковка
    }

    public override void Serialize(ref DataStreamWriter writer)//упаковка
    {
        writer.WriteByte((byte)Code);//записываем название сообщения
        writer.WriteInt(AssignedTeam);//записываем команду
    }

    public override void Deserialize(DataStreamReader reader)//распаковка
    {
        AssignedTeam = reader.ReadInt();//читаем команду
    }

    public override void RecieveOnClient()//получение сообщения клиентом
    {
        NetUtility.C_WELCOME?.Invoke(this);
    }

    public override void RecieveOnServer(NetworkConnection connection)//получение сообщения сервером
    {
        NetUtility.S_WELCOME?.Invoke(this, connection);
    }
}
