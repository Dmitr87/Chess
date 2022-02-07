using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public enum OperationCode //��� ��������� ��������� (�� 3 ���� ��������� - ���������� ������)
{
    KEEP_ALIVE = 1, //������ ����������� ����� ���� ��� �� �������� ��� ��������� ������������ �����
    WELCOME = 2,//���������, ������������ ����� ����� ����� ������� �� ������
    START_GAME = 3,//���������, ��� ��������� �������� ���������� ����
    MAKE_MOVE = 4,//�������� ���������� � ����������� ����, ����� ����� ���� ��� ��������� �� ���������� ������� ������������
    REMATCH = 5//�������� ���������� � ���, ����� �� ������ ������� ������ (� �������� ���� ������, ���� ��� �����)
}

public class NetMessage
{
    public OperationCode Code { set; get; } //������ ����� �������������, ����� � ������ ������ ����� ���� ������������ ��� ����������

    public virtual void Serialize(ref DataStreamWriter writer) //���� ����������� ������������ ������� (� ����� ����������) � ��������� ���
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
