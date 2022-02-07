using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetStartGame : NetMessage
{
    public NetStartGame() //�������� ���������
    {
        Code = OperationCode.START_GAME;
    }

    public NetStartGame(DataStreamReader reader)//���������
    {
        Code = OperationCode.START_GAME;
        Deserialize(reader);//����������
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);//�������� �������� ���������
    }

    public override void Deserialize(DataStreamReader reader)
    {
        //��������� �� �������� ������ ����� ��������, ��� ��� ��� �������� ���� ���������� �� ����� ������ �������
    }

    public override void RecieveOnClient()//��������� �������� ���������
    {
        NetUtility.C_START_GAME?.Invoke(this);
    }

    public override void RecieveOnServer(NetworkConnection connection)//��������� ��������� ��������
    {
        NetUtility.S_START_GAME?.Invoke(this, connection);
    }
}
