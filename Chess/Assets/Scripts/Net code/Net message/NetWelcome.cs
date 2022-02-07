using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetWelcome : NetMessage
{
    public int AssignedTeam { set; get; }//���������� ������� ���������, ��� ��� ����������� ��� ���������� ����� � ������ ������� ������

    public NetWelcome()//�������� ���������
    {
        Code = OperationCode.WELCOME;
    }

    public NetWelcome(DataStreamReader reader)//��������� ���������
    {
        Code = OperationCode.WELCOME;
        Deserialize(reader);//����������
    }

    public override void Serialize(ref DataStreamWriter writer)//��������
    {
        writer.WriteByte((byte)Code);//���������� �������� ���������
        writer.WriteInt(AssignedTeam);//���������� �������
    }

    public override void Deserialize(DataStreamReader reader)//����������
    {
        AssignedTeam = reader.ReadInt();//������ �������
    }

    public override void RecieveOnClient()//��������� ��������� ��������
    {
        NetUtility.C_WELCOME?.Invoke(this);
    }

    public override void RecieveOnServer(NetworkConnection connection)//��������� ��������� ��������
    {
        NetUtility.S_WELCOME?.Invoke(this, connection);
    }
}
