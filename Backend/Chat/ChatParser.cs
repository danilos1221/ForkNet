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

            if (line.StartsWith("@thread"))
            {
                chat.threadId = line.Substring(7).Trim();
                continue;
            }

            if (line.StartsWith("@day"))
            {
                string value = line.Substring(4).Trim();
                if (!int.TryParse(value, out chat.dayNumber))
                {
                    chat.dayNumber = 1;
                    Debug.LogWarning($"Некорректный @day '{value}'. Использую dayNumber=1");
                }
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

                    string choiceTextRaw = option.Substring(1).Trim();
                    ParseChoiceScoreTag(choiceTextRaw, out string choisetext, out string scoreKey, out int scoreDelta);
    
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
                        @goto = gotoLine.Substring(2).Trim(),
                        scoreKey = scoreKey,
                        scoreDelta = scoreDelta
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

            if (line.StartsWith("@ifscore", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseIfScoreLine(line, out ChatMessage conditionalMessage))
                {
                    Debug.LogError($"Не удалось распарсить @ifscore: '{line}'");
                    continue;
                }

                conditionalMessage.id = $"msg_{messageIndex++}";
                conditionalMessage.type = "if_score";
                chat.messages.Add(conditionalMessage);
                continue;
            }

            if (line.StartsWith("@setscore", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseSetScoreLine(line, out ChatMessage setScoreMessage))
                {
                    Debug.LogError($"Не удалось распарсить @setscore: '{line}'");
                    continue;
                }

                setScoreMessage.id = $"msg_{messageIndex++}";
                setScoreMessage.type = "set_score";
                chat.messages.Add(setScoreMessage);
                continue;
            }

            if (line.StartsWith("@addscore", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseAddScoreLine(line, out ChatMessage addScoreMessage))
                {
                    Debug.LogError($"Не удалось распарсить @addscore: '{line}'");
                    continue;
                }

                addScoreMessage.id = $"msg_{messageIndex++}";
                addScoreMessage.type = "add_score";
                chat.messages.Add(addScoreMessage);
                continue;
            }

            if (line.StartsWith("@resetscore", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseResetScoreLine(line, out ChatMessage resetScoreMessage))
                {
                    Debug.LogError($"Не удалось распарсить @resetscore: '{line}'");
                    continue;
                }

                resetScoreMessage.id = $"msg_{messageIndex++}";
                resetScoreMessage.type = "set_score";
                chat.messages.Add(resetScoreMessage);
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
                case "Nastya":
                    msg.senderId = "nastya";
                    msg.senderName = "Nastya";
                    break;

                case "Protagonist":
                    msg.senderId = "player";
                    msg.senderName = "You";
                    break;

                default:
                    msg.senderId = speaker.ToLower();
                    msg.senderName = speaker;
                    break;
            }


            chat.messages.Add(msg);
        }

        if (string.IsNullOrWhiteSpace(chat.threadId))
            chat.threadId = chat.id;

        if (chat.dayNumber <= 0)
            chat.dayNumber = 1;

        //Debug.Log($"Parsed chat: {chat.id} with {chat.messages.Count} messages.");
        return chat;
    }

    private static void ParseChoiceScoreTag(string rawChoiceText, out string visibleText, out string scoreKey, out int scoreDelta)
    {
        visibleText = rawChoiceText;
        scoreKey = string.Empty;
        scoreDelta = 0;

        if (string.IsNullOrWhiteSpace(rawChoiceText))
            return;

        int tagStart = rawChoiceText.LastIndexOf("[score ", StringComparison.OrdinalIgnoreCase);
        if (tagStart < 0 || !rawChoiceText.EndsWith("]"))
            return;

        string tagBody = rawChoiceText.Substring(tagStart + 1, rawChoiceText.Length - tagStart - 2).Trim();
        string[] parts = tagBody.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3 || !string.Equals(parts[0], "score", StringComparison.OrdinalIgnoreCase))
            return;

        if (!int.TryParse(parts[2], out int parsedDelta))
            return;

        visibleText = rawChoiceText.Substring(0, tagStart).TrimEnd();
        scoreKey = parts[1].Trim();
        scoreDelta = parsedDelta;
    }

    private static bool TryParseIfScoreLine(string line, out ChatMessage message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        string rest = line.Substring("@ifscore".Length).Trim();
        int arrowIndex = rest.IndexOf("->", StringComparison.Ordinal);
        if (arrowIndex < 0)
            return false;

        string conditionPart = rest.Substring(0, arrowIndex).Trim();
        string branchesPart = rest.Substring(arrowIndex + 2).Trim();

        string[] conditionTokens = conditionPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (conditionTokens.Length != 3)
            return false;

        string key = conditionTokens[0].Trim();
        string op = conditionTokens[1].Trim();
        if (!IsSupportedScoreOperator(op))
            return false;

        if (!int.TryParse(conditionTokens[2], out int threshold))
            return false;

        string[] branches = branchesPart.Split('|');
        string trueTarget = branches.Length > 0 ? branches[0].Trim() : string.Empty;
        string falseTarget = branches.Length > 1 ? branches[1].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(trueTarget))
            return false;

        message = new ChatMessage
        {
            scoreKey = key,
            scoreOperator = op,
            scoreThreshold = threshold,
            gotoIfTrue = trueTarget,
            gotoIfFalse = falseTarget
        };

        return true;
    }

    private static bool IsSupportedScoreOperator(string op)
    {
        return string.Equals(op, ">", StringComparison.Ordinal) ||
               string.Equals(op, ">=", StringComparison.Ordinal) ||
               string.Equals(op, "<", StringComparison.Ordinal) ||
               string.Equals(op, "<=", StringComparison.Ordinal) ||
               string.Equals(op, "==", StringComparison.Ordinal) ||
               string.Equals(op, "!=", StringComparison.Ordinal);
    }

    private static bool TryParseSetScoreLine(string line, out ChatMessage message)
    {
        message = null;

        string rest = line.Substring("@setscore".Length).Trim();
        string[] tokens = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2)
            return false;

        if (!int.TryParse(tokens[1], out int value))
            return false;

        message = new ChatMessage
        {
            scoreKey = tokens[0].Trim(),
            scoreValue = value
        };
        return true;
    }

    private static bool TryParseAddScoreLine(string line, out ChatMessage message)
    {
        message = null;

        string rest = line.Substring("@addscore".Length).Trim();
        string[] tokens = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2)
            return false;

        if (!int.TryParse(tokens[1], out int delta))
            return false;

        message = new ChatMessage
        {
            scoreKey = tokens[0].Trim(),
            scoreValue = delta
        };
        return true;
    }

    private static bool TryParseResetScoreLine(string line, out ChatMessage message)
    {
        message = null;

        string rest = line.Substring("@resetscore".Length).Trim();
        if (string.IsNullOrWhiteSpace(rest))
            return false;

        message = new ChatMessage
        {
            scoreKey = rest,
            scoreValue = 0
        };
        return true;
    }
}