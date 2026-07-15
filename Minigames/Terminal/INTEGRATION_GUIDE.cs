// 🖥️ ПОЛНЫЙ ПРИМЕР ИНТЕГРАЦИИ ТЕРМИНАЛА
//
// Это руководство показывает, как полностью интегрировать терминал в вашу VN

using UnityEngine;
using System.Collections;

/*
 * ШАГИ ИНТЕГРАЦИИ:
 * 
 * 1. SETUP В UNITY
 * ================
 * - На сцене создать Canvas для терминала
 * - Создать Panel с компонентом TerminalUI
 * - Добавить в сцену GameObject с TerminalMinigame
 * - Привязать UI элементы в инспекторе
 * 
 * 2. JSON КОНФИГ
 * ==============
 * - Открыть Resources/Terminals/terminals.json
 * - Добавить новые терминальные последовательности
 * - Каждый терминал имеет unique ID (например "archive_decrypt")
 * 
 * 3. ВЫЗОВ ИЗ ДИАЛОГА
 * ===================
 * - В ScenarioManager вызывается: scenarioManager.OpenTerminal("archive_decrypt")
 * - Терминал открывается, диалог паузится
 * - После завершения диалог продолжается с того же места
 * 
 * 4. НАГРАДЫ
 * ===========
 * - Image: разблокирует предмет в галерее
 * - Message: показывает текст в консоли
 * - Music: разблокирует музыку
 * - Unlock: разблокирует общий объект
 */
/*
// ПРИМЕР 1: Вызов терминала из кнопки (для тестирования)
public class TerminalTestExample : MonoBehaviour
{
    public void TestTerminal()
    {
        ScenarioManager scenarioManager = FindObjectOfType<ScenarioManager>();
        if (scenarioManager != null)
        {
            // Открыть терминал архива
            scenarioManager.OpenTerminal("archive_decrypt");
        }
    }
}
*/
// ПРИМЕР 2: Добавление терминала в чат сообщение
// Отредактировать chats.json следующим образом:
/*
{
  "id": "my_chat",
  "name": "Someone",
  "messages": [
    {
      "senderId": "npc",
      "text": "Привет! Я нашла зашифрованный архив..."
    },
    {
      "senderId": "system",
      "text": "TERMINAL_START:archive_decrypt"
    },
    {
      "senderId": "npc",
      "text": "Спасибо! Я получила доступ к файлам."
    }
  ]
}
*/

// ПРИМЕР 3: JSON структура нового терминала
/*
{
  "id": "my_custom_terminal",
  "title": "Custom Terminal",
  "description": "My terminal for minigame",
  "commands": [
    {
      "prompt": "access database",
      "response": "Accessing...\n[CONNECTED]",
      "delay": 1.0
    },
    {
      "prompt": "search user_data",
      "response": "Searching...\n[FOUND] 5 records",
      "delay": 1.5
    },
    {
      "prompt": "export results",
      "response": "Exporting...\n[COMPLETE]",
      "delay": 1.0
    }
  ],
  "reward": {
    "type": "Image",
    "id": "new_secret_photo"
  },
  "onCompleteMessage": "[SUCCESS] Data exported"
}
*/
/*
// ПРИМЕР 4: Проверка результата терминала
public class TerminalResultExample : MonoBehaviour
{
    public void OnTerminalCompleted()
    {
        GameData gameData = GameManager.Instance.GameData;
        
        // Проверить, был ли разблокирован предмет
        if (gameData.unlockedGalleryItems.Contains("secret_photo_01"))
        {
            Debug.Log("Фото было разблокировано!");
        }
    }
}
*/
// ============================================
// СТАНДАРТНЫЕ КОМАНДЫ ДЛЯ JSON
// ============================================

/*
АРХИВЫ:
-------
connect archive_node
list encrypted_files
decrypt -fast
extract logs

ЧАТЫ:
-----
scan recovery_partition
filter message_type
restore -all

МУЗЫКА:
-------
access audio_vault
authorize -master
load soundtrack

БАЗЫ ДАННЫХ:
------------
access database
search user_data
export results

СИСТЕМНЫЕ:
----------
reboot system
load backup
initialize_protocol
verify_credentials
*/
