using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public enum cameraAngle //номера позиций камеры
{
    menu = 0,//камера, не смотрящая на доску (видно на всех экранах меню)
    whiteTeam = 1,//камера для белой команды
    blackTeam = 2 //камера для черной команды
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; } //возможно ссылаться на методы этого класса с помощью GameUI.Instance.smth

    public Server server;//сервер
    public Client client;//клиент

    [SerializeField] public Animator menuAnimator; //аниматор, нужен для плавных переходов между экранами меню
    [SerializeField] private TMP_InputField addressInput; //поле для ввода ip адреса
    [SerializeField] private GameObject[] cameraAngles; //объект для изменения положения камеры

    public Action<bool> IsLocalGame; //является ли игра локальной

    private void Awake() //вызывается до старта всех скриптов
    {
        Instance = this;
        RegisterEvents();
    }
    //кнопки
    public void OnLocalGameButton() //что происходит при нажатии на кнопку локальной игры
    {
        menuAnimator.SetTrigger("InGameMenu"); //выбираем меню игры
        IsLocalGame?.Invoke(true); //переменная теперь true
        server.Init(49002); //подключаем сервер к порту 49002 (взял номер с самого низа списка свободных портов википедии) 
        client.Init("127.0.0.1", 49002); //подключаем пользователя к порту (универсальный ip)
    }

    public void OnOnlineGameButton() //при нажатии на кнопку игры по сети
    {
        menuAnimator.SetTrigger("OnlineMenu"); //выбираем экран для игры по сети
    }

    public void OnHostButton() //при нажатии на кнопку создания сервера
    {
        IsLocalGame?.Invoke(false); //игра не локальная
        server.Init(49002); //подключаем сервер к порту
        client.Init("127.0.0.1", 49002); //подключаем клиента к порту
        menuAnimator.SetTrigger("HostMenu"); //включаем анимацию перехода на экран ожидания игрока
    }

    public void OnConnectButton() //при нажатии на кнопку подключения к серверу
    {
        IsLocalGame?.Invoke(false); //игра не локальная
        client.Init(addressInput.text, 49002); //подключаемся к введенному ip и порту 49002
    }

    public void OnBackButton() //при нажатии на кнопку выхода из меню игры по сети
    {
        menuAnimator.SetTrigger("StartMenu"); //возвращаемся в начальное меню
    }

    public void OnHostBackButton() //при нажатии кнопки выхода на экрана ожидания противника
    {
        server.Shutdown(); //отключаем сервер
        client.Shutdown(); //отключаем клиента
        menuAnimator.SetTrigger("OnlineMenu"); //возвращаемся в меню игры по сети
    }

    public void OnLeaveTheGameButton() //при нажатии на кнопку отключения от игры (после конца игры)
    {
        ChangeCamera(cameraAngle.menu); //возвращаем камеру в положение, в котором она находилась в меню (смотрит вдаль)
        menuAnimator.SetTrigger("StartMenu"); //возвращаемся в начальное меню
    }
    //камеры
    public void ChangeCamera(cameraAngle index) //смена позиции камеры
    {
        for (int i = 0; i < cameraAngles.Length; i++) //для каждого значения позиции камеры
            cameraAngles[i].SetActive(false); //отключаем его

        cameraAngles[(int)index].SetActive(true); //включаем нужную позицию камеры
    }
    //прочее
    private void RegisterEvents() //начинает игру
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void UnregisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }

    private void OnStartGameClient(NetMessage message) //включает экран для игры
    {
        menuAnimator.SetTrigger("InGameMenu");
    }
}
