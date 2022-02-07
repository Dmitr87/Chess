using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public enum cameraAngle //������ ������� ������
{
    menu = 0,//������, �� ��������� �� ����� (����� �� ���� ������� ����)
    whiteTeam = 1,//������ ��� ����� �������
    blackTeam = 2 //������ ��� ������ �������
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; } //�������� ��������� �� ������ ����� ������ � ������� GameUI.Instance.smth

    public Server server;//������
    public Client client;//������

    [SerializeField] public Animator menuAnimator; //��������, ����� ��� ������� ��������� ����� �������� ����
    [SerializeField] private TMP_InputField addressInput; //���� ��� ����� ip ������
    [SerializeField] private GameObject[] cameraAngles; //������ ��� ��������� ��������� ������

    public Action<bool> IsLocalGame; //�������� �� ���� ���������

    private void Awake() //���������� �� ������ ���� ��������
    {
        Instance = this;
        RegisterEvents();
    }
    //������
    public void OnLocalGameButton() //��� ���������� ��� ������� �� ������ ��������� ����
    {
        menuAnimator.SetTrigger("InGameMenu"); //�������� ���� ����
        IsLocalGame?.Invoke(true); //���������� ������ true
        server.Init(49002); //���������� ������ � ����� 49002 (���� ����� � ������ ���� ������ ��������� ������ ���������) 
        client.Init("127.0.0.1", 49002); //���������� ������������ � ����� (������������� ip)
    }

    public void OnOnlineGameButton() //��� ������� �� ������ ���� �� ����
    {
        menuAnimator.SetTrigger("OnlineMenu"); //�������� ����� ��� ���� �� ����
    }

    public void OnHostButton() //��� ������� �� ������ �������� �������
    {
        IsLocalGame?.Invoke(false); //���� �� ���������
        server.Init(49002); //���������� ������ � �����
        client.Init("127.0.0.1", 49002); //���������� ������� � �����
        menuAnimator.SetTrigger("HostMenu"); //�������� �������� �������� �� ����� �������� ������
    }

    public void OnConnectButton() //��� ������� �� ������ ����������� � �������
    {
        IsLocalGame?.Invoke(false); //���� �� ���������
        client.Init(addressInput.text, 49002); //������������ � ���������� ip � ����� 49002
    }

    public void OnBackButton() //��� ������� �� ������ ������ �� ���� ���� �� ����
    {
        menuAnimator.SetTrigger("StartMenu"); //������������ � ��������� ����
    }

    public void OnHostBackButton() //��� ������� ������ ������ �� ������ �������� ����������
    {
        server.Shutdown(); //��������� ������
        client.Shutdown(); //��������� �������
        menuAnimator.SetTrigger("OnlineMenu"); //������������ � ���� ���� �� ����
    }

    public void OnLeaveTheGameButton() //��� ������� �� ������ ���������� �� ���� (����� ����� ����)
    {
        ChangeCamera(cameraAngle.menu); //���������� ������ � ���������, � ������� ��� ���������� � ���� (������� �����)
        menuAnimator.SetTrigger("StartMenu"); //������������ � ��������� ����
    }
    //������
    public void ChangeCamera(cameraAngle index) //����� ������� ������
    {
        for (int i = 0; i < cameraAngles.Length; i++) //��� ������� �������� ������� ������
            cameraAngles[i].SetActive(false); //��������� ���

        cameraAngles[(int)index].SetActive(true); //�������� ������ ������� ������
    }
    //������
    private void RegisterEvents() //�������� ����
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void UnregisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }

    private void OnStartGameClient(NetMessage message) //�������� ����� ��� ����
    {
        menuAnimator.SetTrigger("InGameMenu");
    }
}
