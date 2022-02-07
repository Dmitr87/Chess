using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetKeepAlive : NetMessage
{
    public NetKeepAlive()//�������� ���������
    {
        Code = OperationCode.KEEP_ALIVE;
    }

    public NetKeepAlive(DataStreamReader reader)//��������� ���������
    {
        Code = OperationCode.KEEP_ALIVE;
        Deserialize(reader);//"�������������" ���������
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);//��� ��� ��������� ������ ������ ���� (���������� �� �����) �� ����� �������� ���� ������ ��������
    }

    public override void Deserialize(DataStreamReader reader)
    {
        //������ ���� �� ��� �����������, ����� �������� ��� ���������, ������� ������ ��������� ������ � ��������� ������
    }

    public override void RecieveOnClient()
    {
        NetUtility.C_KEEP_ALIVE?.Invoke(this);//������ �������� ���������
    }

    public override void RecieveOnServer(NetworkConnection connection)
    {
        NetUtility.S_KEEP_ALIVE?.Invoke(this, connection);//������ �������� ���������
    }
}
