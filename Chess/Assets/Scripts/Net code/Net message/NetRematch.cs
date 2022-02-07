using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;

public class NetRematch : NetMessage
{
    public int team; //������� ������
    public byte wantRematch;//����� ������ ������ ������ ��� ��� (��� ���� bool ��� writer)

    public NetRematch()//�������� ���������
    {
        Code = OperationCode.REMATCH;
    }

    public NetRematch(DataStreamReader reader)//��������� ���������
    {
        Code = OperationCode.REMATCH;
        Deserialize(reader);//���������� ���������
    }

    public override void Serialize(ref DataStreamWriter writer)//����� ��� ��������
    {
        writer.WriteByte((byte)Code);//���������� ��������
        writer.WriteInt(team);//�������
        writer.WriteByte(wantRematch);//� ����� �� ������ ������
    }

    public override void Deserialize(DataStreamReader reader)//����� ��� ����������
    {
        //����� ������ ��� �� �� �����, ��� � � ��������, �� ��� ������ ������
        team = reader.ReadInt();
        wantRematch = reader.ReadByte();
    }

    public override void RecieveOnClient()//��������� �������� ���������
    {
        NetUtility.C_REMATCH?.Invoke(this);
    }

    public override void RecieveOnServer(NetworkConnection connection)//��������� ��������� ��������
    {
        NetUtility.S_REMATCH?.Invoke(this, connection);
    }
}
