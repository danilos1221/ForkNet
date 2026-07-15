using System;
using UnityEngine;
using System.Collections.Generic;

public static class ChatParser
{
    public static Chat Parse(string text)
    {
        Chat chat = new Chat();

        string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        int messageIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            // Комментарии
            if (line.StartsWith("//"))
                continue;

            // -------------------------
            // Заголовок чата
            // -------------------------

            if (line.StartsWith("#chat"))
            {
                chat.id = line.Substring(5).Trim();
                continue;
            }

            if (line.StartsWith("@name"))
            {
                chat.name = line.Substring(5).Trim();
                continue;
            }

            if (line.StartsWith("@avatar"))
            {
                chat.avatarPath = line.Substring(7).Trim();
                continue;
            }

            if (line.StartsWith("@type"))
            {
                string type = line.Substring(5).Trim();

                if (Enum.TryParse(type, true, out ChatType chatType))
                    chat.chatType = chatType;

                continue;
            }

            // Проверяем наличие выбора
            if (line == "@choice" || line == "@choise")
            {
                ChatMessage choiceMessage = new ChatMessage
                {
                    id = $"msg_{messageIndex++}",
                    type = "choice",
                    choices = new List<ChatChoice>()
                };

                while (++i < lines.Length)
                {
                    string option = lines[i].Trim();

                    if (string.IsNullOrEmpty(option))
                        continue;

                    if (option.StartsWith(":"))
                    {
                        i--;
                        break;
                    }

                    if (option.StartsWith("@"))
                    {
                        i--;
                        break;
                    }

                    if (!option.StartsWith(">"))
                    {
                        Debug.LogError("Вариант выбора должен начинаться с '>'");
                        continue;
                    }

                    string choisetext = option.Substring(1).Trim();
    
                    if (++i >= lines.Length)
                        break;

                    string gotoLine = lines[i].Trim();

                    if (!gotoLine.StartsWith("->"))
                    {
                        Debug.LogError("После варианта должен идти ->");
                        break;
                    }

                    choiceMessage.choices.Add(new ChatChoice
                    {
                        text = choisetext,
                        @goto = gotoLine.Substring(2).Trim()
                    });
                }

                chat.messages.Add(choiceMessage);
                continue;
            }

            if (line.StartsWith(":"))
            {
                string labelId = line.Substring(1).Trim();

                if (string.IsNullOrEmpty(labelId))
                {
                    Debug.LogWarning("Пустой label в сценарии чата.");
                    continue;
                }

                if (string.Equals(labelId, "end", StringComparison.OrdinalIgnoreCase))
                {
                    chat.messages.Add(new ChatMessage
                    {
                        id = $"msg_{messageIndex++}",
                        type = "block_end"
                    });
                    continue;
                }

                chat.messages.Add(new ChatMessage
                {
                    id = labelId,
                    type = "label"
                });
                continue;
            }

            if (line.StartsWith("->"))
            {
                string gotoTarget = line.Substring(2).Trim();

                if (string.IsNullOrEmpty(gotoTarget))
                {
                    Debug.LogError("Переход '->' должен содержать целевой label.");
                    continue;
                }

                chat.messages.Add(new ChatMessage
                {
                    id = $"msg_{messageIndex++}",
                    @goto = gotoTarget
                });
                continue;
            }

            // -------------------------
            // Сообщение
            // -------------------------

            int colon = line.IndexOf(':');

            if (colon == -1)
                continue;

            string speaker = line.Substring(0, colon).Trim();
            string messageText = line.Substring(colon + 1).Trim();
            bool isImageMessage = messageText.StartsWith("@") && messageText.Length > 1;

            ChatMessage msg = new ChatMessage
            {
                id = $"msg_{messageIndex++}",
                text = isImageMessage ? string.Empty : messageText,
                imageId = isImageMessage ? messageText.Substring(1).Trim() : string.Empty,
                timestamp = ""
            };

            if (isImageMessage && string.IsNullOrEmpty(msg.imageId))
            {
                Debug.LogWarning($"Пустой imageId в строке: '{line}'");
                msg.text = messageText;
                msg.imageId = string.Empty;
            }

            switch (speaker)
            {
                case "Настя":
                    msg.senderId = "nastya";
                    msg.senderName = "Настя";
                    break;

                case "Герой":
                    msg.senderId = "player";
                    msg.senderName = "Ты";
                    break;

                default:
                    msg.senderId = speaker.ToLower();
                    msg.senderName = speaker;
                    break;
            }


            chat.messages.Add(msg);
        }

        //Debug.Log($"Parsed chat: {chat.id} with {chat.messages.Count} messages.");
        return chat;
    }
}