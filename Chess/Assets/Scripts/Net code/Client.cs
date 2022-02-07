using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;

public class Client : MonoBehaviour
{
    public static Client Instance { set; get; } //добавляем возможность пользоваться методами класса

    private void Awake()
    {
        Instance = this;
    }

    public NetworkDriver driver;//интернет-драйвер, без него невозможно подключиться по сети к другому игроку
    private NetworkConnection connection;//интернет-подключение

    private bool isActive = false;//индикатор соединения (false когда мы не подключены к другим устройствам и к нам никто не подключен)

    public Action connectionDropped;//динамическая переменная,нужна для того чтобы следить за состоянием подключения

    //методы
    public void Init(string ip, ushort port)
    {
        driver = NetworkDriver.Create();//создаем драйвер для создания подключения
        NetworkEndPoint endpoint = NetworkEndPoint.Parse(ip, port);//создаем конечную точку (про это расскажу подробнее)

        connection = driver.Connect(endpoint);//подключаемся с помощью ранее созданного драйвера к конечной точке

        isActive = true;//теперь соединение установлено

        RegisterToEvent();//отправляем сообщение о том что мы подключены
    }

    public void Shutdown()
    {
        if (isActive) //если подключение есть
        {
            UnregisterToEvent(); //больше не отправляем сообщения 
            driver.Dispose(); //удаляем драйвер
            isActive = false; //меняем индикатор
            connection = default(NetworkConnection);//сбрасываем подключение
        }
    }

    public void Update()
    {
        if (!isActive) //если подключения нет, ничего не происходит
            return;

        driver.ScheduleUpdate().Complete();//иначе обновляем драйвер
        CheckAlive();//проверяем, работает ли подключение 
        UpdateMessagePump();
    }

    private void CheckAlive()
    {
        if (!connection.IsCreated && isActive)//если индикатор подключения "включен", но на самом деле подключения нет
        {
            connectionDropped?.Invoke();//проверяем, сбрасывалось ли подключение
            Shutdown();//сбрасываем подключение и индикатор
        }
    }

    private void UpdateMessagePump()
    {
        DataStreamReader stream;//для распаковки данных в сообщении
        NetworkEvent.Type cmd;//тип сообщения (3 - подключение, отключение и какие-то данные)
        while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)//пока есть какие-то сообщения
        {
            if (cmd == NetworkEvent.Type.Connect)//если пользователь подключился
                SendToServer(new NetWelcome());//отправлем на сервер сообщение
            else if (cmd == NetworkEvent.Type.Data)//если пользователь отправил данные
                NetUtility.OnData(stream, default(NetworkConnection));//читаем данные
            else if (cmd == NetworkEvent.Type.Disconnect)//если пользователь отключился
            {
                connection = default(NetworkConnection);//возвращаем подключение к стандартному значению
                connectionDropped?.Invoke();
                Shutdown();//отключаемся и сбрасываем подключение
            }
        }
    }

    public void SendToServer(NetMessage msg)
    {
        DataStreamWriter writer;//DataStreamWriter нужен для "упаковки" данных перед отправкой
        driver.BeginSend(connection, out writer); //начинаем отправку сообщения на заданный адрес
        msg.Serialize(ref writer);//"упаковываем" наше сообщение, отсылаясь на writer
        driver.EndSend(writer);//заканчиваем отправку
    }

    //события 
    private void RegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE += OnKeepAlive;//отправляем сообщения о том что мы на сервере
    }

    private void UnregisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE -= OnKeepAlive;//не отправляем сообщения о том что мы на сервере
    }

    private void OnKeepAlive(NetMessage nm)
    {
        SendToServer(nm);//отправляем на сервер сообщение, чтобы он мог проверить, подключен пользователь или нет
    }
}
