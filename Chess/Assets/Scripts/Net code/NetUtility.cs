using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Networking.Transport;

public static class NetUtility
{
    public static void OnData(DataStreamReader stream, NetworkConnection connection, Server server = null)
    {
        NetMessage msg = null;//���������� ��������� ���
        var operationCode = (OperationCode)stream.ReadByte();//������ "��������" ���������
        switch (operationCode)//switch �������� ������ �� �������������� ���� ���� ����� ����� ������������� ������
        {
            case OperationCode.KEEP_ALIVE: msg = new NetKeepAlive(stream); break;
            case OperationCode.WELCOME: msg = new NetWelcome(stream); break;
            case OperationCode.START_GAME: msg = new NetStartGame(stream); break;
            case OperationCode.MAKE_MOVE: msg = new NetMakeMove(stream); break;
            case OperationCode.REMATCH: msg = new NetRematch(stream); break;
            default:
                Debug.LogError("message has no operation code");//���� � ��������� ��� �� ������ �� ����������������� �������� (�� ����, ������ �� ����� ����)
                break;
                //break ����� ������ ������ �����, ����� �������� ��������������� � ����� ����������� ������ ��������
        }

        if (server != null) //���� ���� ������, ��������� ������������ �� ����
            msg.RecieveOnServer(connection);//�������� ��������� �� �������
        else
            msg.RecieveOnClient();//������ �������� ���������
    }

    //��� ���������
    //������
    public static Action<NetMessage> C_KEEP_ALIVE;
    public static Action<NetMessage> C_WELCOME;
    public static Action<NetMessage> C_START_GAME;
    public static Action<NetMessage> C_MAKE_MOVE;
    public static Action<NetMessage> C_REMATCH;
    //������
    public static Action<NetMessage, NetworkConnection> S_KEEP_ALIVE;
    public static Action<NetMessage, NetworkConnection> S_WELCOME;
    public static Action<NetMessage, NetworkConnection> S_START_GAME;
    public static Action<NetMessage, NetworkConnection> S_MAKE_MOVE;
    public static Action<NetMessage, NetworkConnection> S_REMATCH;
    //��������� ��� ������� �������� NetworkConnection - ����� �����������
}
