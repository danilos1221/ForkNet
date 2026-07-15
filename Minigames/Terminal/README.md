# 🖥️ Киношный Терминал - Система Миниигры

## 📋 概要

Стилизованный "киношный" терминал для визуального романа. Позволяет игроку вводить красивые команды для разблокировки информации, фото, чата и музыки.

## 🎯 Основные компоненты

### 1. **TerminalData.cs**
- `TerminalCommand` - структура команды (prompt, response, delay)
- `TerminalReward` - структура награды (type, id)
- `TerminalSequence` - последовательность команд
- `TerminalDatabase` - загрузка из JSON

### 2. **TerminalMinigame.cs**
- Главный контроллер миниигры
- Управляет последовательностью команд
- Проверяет ввод игрока
- Обрабатывает награды

### 3. **TerminalUI.cs**
- UI отображение терминала
- Вывод текста в консоль
- Приём ввода от игрока
- Цветовое оформление (зелёный=система, белый=ввод, красный=ошибка, голубой=приглашение)

## 🎮 Как использовать

### Установка в Unity сцене

1. **Создать Canvas для терминала**
   - Добавить Panel (TerminalPanel)
   - Добавить TextMeshPro для вывода (outputText)
   - Добавить InputField (inputField)
   - Добавить TextMeshPro для приглашения (promptText)
   - Добавить ScrollRect для скролла

2. **Добавить компоненты**
   - На Panel добавить `TerminalUI` компонент
   - На сцену добавить `TerminalMinigame` компонент
   - На сцену добавить `TerminalDatabase` (или просто загружается автоматически)

3. **Привязать в инспекторе**
   ```
   TerminalUI:
   - Terminal Panel → Panel
   - Output Text → TextMeshPro (для вывода)
   - Input Field → InputField
   - Prompt Text → TextMeshPro (для "приглашения")
   - Scroll View → ScrollRect
   ```

### Вызов из диалога

В ScenarioManager есть метод `OpenTerminal(string terminalId)`:

```csharp
// Открыть терминал "archive_decrypt" из диалога
scenarioManager.OpenTerminal("archive_decrypt");

// После завершения терминала диалог продолжится автоматически
```

### Структура JSON (Resources/Terminals/terminals.json)

```json
{
  "terminals": [
    {
      "id": "archive_decrypt",
      "title": "Archive Decryption",
      "description": "Decrypt the archive",
      "commands": [
        {
          "prompt": "connect archive_node",
          "response": "Connecting...\n[SUCCESS] Connected",
          "delay": 1.5
        },
        {
          "prompt": "decrypt -fast",
          "response": "Decrypting...\n[SUCCESS] Complete",
          "delay": 2.0
        }
      ],
      "reward": {
        "type": "Image",
        "id": "secret_photo_01"
      },
      "onCompleteMessage": "[SYSTEM] Archive unlocked"
    }
  ]
}
```

## 🎨 Типы команд

### Точное совпадение
Игрок должен ввести ТОЧНО как в `prompt`:

```json
{
  "prompt": "connect archive_node",
  "response": "Connected",
  "delay": 1.5
}
```

### Форматирование ответа
Можно использовать `\n` для перевода строки:

```json
{
  "response": "Line 1\n[STATUS] Line 2\n[SUCCESS] Line 3"
}
```

## 💰 Типы наград

```json
{
  "reward": {
    "type": "Image",
    "id": "secret_photo_01"
  }
}
```

Доступные типы:
- `"Image"` - разблокирует предмет галереи
- `"Message"` - показывает текстовое сообщение
- `"Music"` - разблокирует музыку
- `"Unlock"` - разблокирует объект

## 🌈 Цветовая схема

По умолчанию:
- **Зелёный** - системные сообщения
- **Белый** - ввод игрока
- **Красный** - ошибки
- **Голубой** - приглашение (prompt)

Меняется в инспекторе TerminalUI:
```
System Color = Green
Input Color = White
Error Color = Red
Prompt Color = Cyan
```

## 📝 Примеры терминалов

В `terminals.json` уже заготовлены:

1. **archive_decrypt** - разблокировка фото
2. **chat_recover** - восстановление чата
3. **music_unlock** - разблокировка музыки

## 🔧 Расширение

Для добавления новой команды:

1. Добавить в `terminals.json`:
```json
{
  "id": "my_terminal",
  "title": "My Terminal",
  "commands": [...]
}
```

2. Вызвать из диалога:
```csharp
scenarioManager.OpenTerminal("my_terminal");
```

## 🐛 Отладка

Смотреть консоль Unity для логов:
- `Игра сохранена в слот 0`
- `Терминал archive_decrypt не найден`
- `TerminalMinigame не найден на сцене`
