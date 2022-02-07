using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetMakeMove : NetMessage
{
    public int startX;//��������� ��������� �� ��� �
    public int startY;//��������� ��������� �� ��� y
    public int endX;//�������� ��������� �� ��� �
    public int endY;//�������� ��������� �� ��� y
    public int team;//�������, ����������� ���

    public NetMakeMove()//�������� ���������
    {
        Code = OperationCode.MAKE_MOVE;
    }

    public NetMakeMove(DataStreamReader reader)//��������� ���������
    {
        Code = OperationCode.MAKE_MOVE;
        Deserialize(reader);//���������� ���������
    }

    public override void Serialize(ref DataStreamWriter writer)//��������
    {
        writer.WriteByte((byte)Code);//���������� �������� ���������, ��������� ������� ����������� ��������������� ����������
        writer.WriteInt(startX);
        writer.WriteInt(startY);
        writer.WriteInt(endX);
        writer.WriteInt(endY);
        writer.WriteInt(team);
    }

    public override void Deserialize(DataStreamReader reader)//����������
    {
        //���������� ���� ������, ������ ���� ���������� (�������� ���������) �� ��� ���������,�������� �� �������� � ��� �� �������, ��� � ������
        startX = reader.ReadInt();
        startY = reader.ReadInt();
        endX = reader.ReadInt();
        endY = reader.ReadInt();
        team = reader.ReadInt();
    }

    public override void RecieveOnClient()
    {
        NetUtility.C_MAKE_MOVE?.Invoke(this);//������ �������� ���������
    }

    public override void RecieveOnServer(NetworkConnection connection)
    {
        NetUtility.S_MAKE_MOVE?.Invoke(this, connection);//������ �������� ���������
    }
}
