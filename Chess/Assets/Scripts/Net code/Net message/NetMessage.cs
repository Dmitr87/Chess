using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public enum OperationCode //все возможные сообщения (из 3 типа сообщений - содержащих данные)
{
    KEEP_ALIVE = 1, //сервер отключается после того как не получает это сообщение определенное время
    WELCOME = 2,//сообщение, появляющееся когда новый игрок заходит на сервер
    START_GAME = 3,//сообщение, при получении которого начинается игра
    MAKE_MOVE = 4,//содержит информацию о совершенном ходе, чтобы можно было его совершить на устройстве другого пользователя
    REMATCH = 5//содержит информацию о том, хотят ли игроки сыграть заново (и начинает игру заново, если оба хотят)
}

public class NetMessage
{
    public OperationCode Code { set; get; } //делаем метод общедоступным, чтобы в других файлах можно было пользоваться его содержимым

    public virtual void Serialize(ref DataStreamWriter writer) //даем возможность пользоваться методом (и всеми остальными) и расширять его
    {
        writer.WriteByte((byte)Code);
    }

    public virtual void Deserialize(DataStreamReader reader)
    {

    }

    public virtual void RecieveOnClient()
    {

    }

    public virtual void RecieveOnServer(NetworkConnection connection)
    {

    }
}
