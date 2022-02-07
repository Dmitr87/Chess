using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetKeepAlive : NetMessage
{
    public NetKeepAlive()//отправка сообщения
    {
        Code = OperationCode.KEEP_ALIVE;
    }

    public NetKeepAlive(DataStreamReader reader)//получение сообщения
    {
        Code = OperationCode.KEEP_ALIVE;
        Deserialize(reader);//"распаковываем" сообщения
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);//так как сообщение просто должно быть (содержимое не важно) то можно записать туда только название
    }

    public override void Deserialize(DataStreamReader reader)
    {
        //первый байт мы уже распаковали, когда получали тип сообщения, поэтому сейчас сообщение пустое и извлекать нечего
    }

    public override void RecieveOnClient()
    {
        NetUtility.C_KEEP_ALIVE?.Invoke(this);//клиент получает сообщение
    }

    public override void RecieveOnServer(NetworkConnection connection)
    {
        NetUtility.S_KEEP_ALIVE?.Invoke(this, connection);//сервер получает сообщение
    }
}
