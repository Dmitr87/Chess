using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Networking.Transport;

public static class NetUtility
{
    public static void OnData(DataStreamReader stream, NetworkConnection connection, Server server = null)
    {
        NetMessage msg = null;//изначально сообщения нет
        var operationCode = (OperationCode)stream.ReadByte();//читаем "название" сообщения
        switch (operationCode)//switch выбирает строку из представленных ниже если певая часть соответствует данной
        {
            case OperationCode.KEEP_ALIVE: msg = new NetKeepAlive(stream); break;
            case OperationCode.WELCOME: msg = new NetWelcome(stream); break;
            case OperationCode.START_GAME: msg = new NetStartGame(stream); break;
            case OperationCode.MAKE_MOVE: msg = new NetMakeMove(stream); break;
            case OperationCode.REMATCH: msg = new NetRematch(stream); break;
            default:
                Debug.LogError("message has no operation code");//если в сообщении нет ни одного из вышеперечисленных названий (по сути, такого не может быть)
                break;
                //break после каждой строки нужен, чтобы проверка останавливалась и сразу принималось нужное значение
        }

        if (server != null) //если есть сервер, сообщение отправлялось на него
            msg.RecieveOnServer(connection);//получаем сообщение на сервере
        else
            msg.RecieveOnClient();//клиент получает сообщение
    }

    //все сообщения
    //клиент
    public static Action<NetMessage> C_KEEP_ALIVE;
    public static Action<NetMessage> C_WELCOME;
    public static Action<NetMessage> C_START_GAME;
    public static Action<NetMessage> C_MAKE_MOVE;
    public static Action<NetMessage> C_REMATCH;
    //сервер
    public static Action<NetMessage, NetworkConnection> S_KEEP_ALIVE;
    public static Action<NetMessage, NetworkConnection> S_WELCOME;
    public static Action<NetMessage, NetworkConnection> S_START_GAME;
    public static Action<NetMessage, NetworkConnection> S_MAKE_MOVE;
    public static Action<NetMessage, NetworkConnection> S_REMATCH;
    //сообщения для сервера содержит NetworkConnection - адрес отправителя
}
