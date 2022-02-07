using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System;

public class Server : MonoBehaviour
{
    public static Server Instance { set; get; } //возможность пользоваться методами класса

    private void Awake()
    {
        Instance = this;
    }

    public NetworkDriver driver; //драйвер (чтобы можно было пользоваться сетью)
    private NativeList<NetworkConnection> connections; //список пользователей

    private bool isActive = false; //изначально сервер неактивен
    private const float keepAliveTickRate = 20.0f; //время, которое отводится на получение сообщения от пользователя о том что он тут
    private float lastKeepAlive; //время получения последнего сообщения от пользователя

    public Action connectionDropped;//существует подключение или нет

    //методы
    public void Init(ushort port) //ushort  - число от 0 до 65 535 (положительное), так как номер порта не может быть отрицательным
    {
        driver = NetworkDriver.Create();//создаем драйвер
        NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;//подключение к любому устройству по ipv4
        endpoint.Port = port;//порт

        if (driver.Bind(endpoint) != 0)//если драйвер уже привязан к порту
            return;
        else//иначе
            driver.Listen();//рассматриваем входящие подключения на драйвере

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);//список подключенных пользователей
        isActive = true;//сервер активен
    }

    public void Shutdown()
    {
        if (isActive)//если сервер работает
        {
            driver.Dispose();//убираем драйвер
            connections.Dispose();//удаляем подключенные клиенты
            isActive = false;//сервер неактивен
        }
    }

    public void Update()
    {
        if (!isActive)//если сервер неактивен, то ничего не может измениться
            return;

        KeepAlive();//сервер не отключается

        driver.ScheduleUpdate().Complete();//обновляем драйвера
        CleanupConnections();//убираем подключения
        AcceptNewConnections();//добавляем новые подключения
        UpdateMessagePump();
    }

    private void CleanupConnections()
    {
        for (int i = 0; i < connections.Length; i++) //проверяем каждое подключение
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);//удаляет элемент и ставит на его место следующий, чтобы в списке не было "дыр"
                --i;//убавляем i, чтобы проверять элемент под тем же номером (т.к. там сейчас новый)
            }
        }
    }

    private void KeepAlive()
    {
        if (Time.time - lastKeepAlive > keepAliveTickRate) //если после последнего отправления сообщения прошло больше заданного значения (20 секунд у нас)
        {
            lastKeepAlive = Time.time;//последнее отправление сообщения было сейчас (мы отправляем сообщение)
            Broadcast(new NetKeepAlive());//транслируем сообщение (отправляем всем подключениям)
        }
    }

    private void AcceptNewConnections()
    {
        NetworkConnection c;
        while ((c = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(c);
        }
    }

    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        for (int i = 0; i < connections.Length; i++)//проверяем каждое подключение
        {
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)//пока есть непустые сообщения
            {
                if (cmd == NetworkEvent.Type.Data)//если сообщение содержит данные
                {
                    NetUtility.OnData(stream, connections[i], this);//читаем сообщение
                }
                else if (cmd == NetworkEvent.Type.Disconnect)//если кто-то отключился
                {
                    connections[i] = default(NetworkConnection);//сбрасываем подключение
                    connectionDropped?.Invoke();
                    Shutdown();//отключаем сервер
                }
            }
        }
    }

    //особые методы для сервера
    //отправляем определенное сообщение определенному пользователю
    public void SendToClient(NetworkConnection connection, NetMessage msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);//начинаем отправку сообщения
        msg.Serialize(ref writer);//"упаковываем" в сообщение всю информацию, используя writer
        driver.EndSend(writer);//заканчиваем
    }
    //отправляем определенное сообщение всем на сервере (обоим игрокам)
    public void Broadcast(NetMessage msg)
    {
        for (int i = 0; i < connections.Length; i++)//для каждого подключения
            if (connections[i].IsCreated)//пользователь подключен
            {
                SendToClient(connections[i], msg);//отправляем сообщение пользователю
            }
    }


}
