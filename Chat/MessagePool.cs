using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Система объектного пулинга для переиспользования сообщений
/// </summary>
public class MessagePool
{
    private Queue<GameObject> availableMessages = new Queue<GameObject>();
    private List<GameObject> activeMessages = new List<GameObject>();
    
    private GameObject messagePrefab;
    private Transform messageContainer;
    private int initialPoolSize;
    private int maxPoolSize;
    
    public MessagePool(GameObject prefab, Transform container, int initialSize = 10, int maxSize = 50)
    {
        messagePrefab = prefab;
        messageContainer = container;
        initialPoolSize = initialSize;
        maxPoolSize = maxSize;
        
        // Создаём начальный пул сообщений
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewMessage();
        }
    }
    
    /// <summary>
    /// Получить сообщение из пула (активировать его)
    /// </summary>
    public GameObject GetMessage()
    {
        GameObject message;
        
        if (availableMessages.Count > 0)
        {
            message = availableMessages.Dequeue();
        }
        else
        {
            // Если нет свободных сообщений, создаём новое (если не превышен лимит)
            if (activeMessages.Count < maxPoolSize)
            {
                message = CreateNewMessage();
            }
            else
            {
                Debug.LogWarning("MessagePool: достигнут максимум объектов в пуле!");
                return null;
            }
        }
        
        message.SetActive(true);
        activeMessages.Add(message);
        
        // Очищаем сообщение перед использованием
        MessageUI messageUI = message.GetComponent<MessageUI>();
        if (messageUI != null)
        {
            messageUI.Clear();
        }
        
        return message;
    }
    
    /// <summary>
    /// Вернуть сообщение в пул (деактивировать его)
    /// </summary>
    public void ReturnMessage(GameObject message)
    {
        if (message != null && activeMessages.Contains(message))
        {
            message.SetActive(false);
            activeMessages.Remove(message);
            availableMessages.Enqueue(message);
        }
    }
    
    /// <summary>
    /// Вернуть все активные сообщения в пул
    /// </summary>
    public void ClearAllMessages()
    {
        List<GameObject> messagesToReturn = new List<GameObject>(activeMessages);
        foreach (GameObject message in messagesToReturn)
        {
            ReturnMessage(message);
        }
    }
    
    private GameObject CreateNewMessage()
    {
        GameObject newMessage = Object.Instantiate(messagePrefab, messageContainer);
        newMessage.SetActive(false);
        availableMessages.Enqueue(newMessage);
        return newMessage;
    }
}
