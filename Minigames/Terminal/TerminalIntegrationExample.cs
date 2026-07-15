// ПРИМЕР ИСПОЛЬЗОВАНИЯ ТЕРМИНАЛА В ДИАЛОГЕ
// 
// 1. В JSON чата добавить сообщение с командой открытия терминала:
//
// {
//   "senderId": "npc",
//   "text": "Я нашла архив, но он зашифрован...",
//   "terminalId": "archive_decrypt"
// }
//
// 2. Затем добавить обработчик в ScenarioManager.PlayChatSequence():
//
//   var message = currentChat.messages[i];
//   
//   // Проверяем, есть ли терминал в этом сообщении
//   if (!string.IsNullOrEmpty(message.terminalId))
//   {
//       OpenTerminal(message.terminalId);
//       yield return new WaitForSeconds(1f);
//   }
//
// 3. В ChatData.cs добавить поле terminalId в ChatMessage:
//
// [System.Serializable]
// public class ChatMessage
// {
//     public string senderId;
//     public string text;
//     public string senderName;
//     public string imageId;
//     public string terminalId;  // <- НОВОЕ ПОЛЕ
// }

using System.Collections.Generic;

[System.Serializable]
public class TerminalIntegrationExample
{
    // Готовые примеры терминалов для копирования в JSON
    public static string GetExampleJSON()
    {
        return @"{
  ""terminals"": [
    {
      ""id"": ""archive_decrypt"",
      ""title"": ""Archive Decryption"",
      ""description"": ""Access the encrypted archive"",
      ""commands"": [
        {
          ""prompt"": ""connect archive_node"",
          ""response"": ""Connecting to archive_node...\n[SUCCESS] Connected to remote node"",
          ""delay"": 1.5
        },
        {
          ""prompt"": ""list encrypted_files"",
          ""response"": ""Fetching directory...\n[3 files found]\n- chat_logs.enc\n- photos.enc\n- music.enc"",
          ""delay"": 1.0
        },
        {
          ""prompt"": ""decrypt -fast"",
          ""response"": ""Initiating decryption sequence...\n[████████░░] 80%\n[SUCCESS] Decryption complete"",
          ""delay"": 2.0
        },
        {
          ""prompt"": ""extract photos.enc"",
          ""response"": ""Extracting encrypted data...\n[SUCCESS] Archive extracted to memory"",
          ""delay"": 1.5
        }
      ],
      ""reward"": {
        ""type"": ""Image"",
        ""id"": ""secret_photo_01""
      },
      ""onCompleteMessage"": ""[SYSTEM] Archive access granted""
    }
  ]
}";
    }
}
