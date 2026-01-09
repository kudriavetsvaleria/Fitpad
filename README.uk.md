<div align="right">

[EN](./README.md) | [UA](./README.uk.md)

</div>

<div align="center">

# 🍃 Fitpad

**Ваш персональний помічник у фітнесі та харчуванні**

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows-lightgrey.svg)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
[![Firebase](https://img.shields.io/badge/Firebase-Firestore-orange.svg)](https://firebase.google.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

</div>

---

## 📋 Зміст

- [Про проєкт](#-про-проєкт)
- [Можливості](#-можливості)
- [Скріншоти](#-скріншоти)
- [Технології](#-технології)
- [Початок роботи](#-початок-роботи)
  - [Вимоги](#вимоги)
  - [Встановлення](#встановлення)
  - [Налаштування](#налаштування)
- [Безпека](#-безпека)
- [Структура проєкту](#-структура-проєкту)
- [Внесок у проєкт](#-внесок-у-проєкт)
- [Ліцензія](#-ліцензія)

---

## 🎯 Про проєкт

**Fitpad** — це комплексний застосунок для Windows, розроблений для допомоги користувачам у керуванні їхнім фітнес-шляхом, відстеженні харчування та досягненні цілей здоров'я. Побудований на WPF та підтримуваний Firebase Firestore, він пропонує безперебійний досвід для відстеження щоденних прийомів їжі, розрахунку харчової цінності та отримання новин про фітнес.

### Ключові особливості

- 📊 **Відстеження харчування**: Записуйте щоденні прийоми їжі та контролюйте калорійність
- 🍽️ **Керування рецептами**: Створюйте та керуйте власними стравами з харчовою інформацією
- 📰 **Новини про фітнес**: Будьте в курсі з автоматично перекладеними статтями
- 🎯 **Встановлення цілей**: Встановлюйте та відстежуйте свої фітнес-цілі
- 🔐 **Безпека**: Хешування паролів BCrypt та конфігурація на основі середовища

---

## ✨ Можливості

### Керування користувачами
- ✅ Безпечна реєстрація з валідацією email
- ✅ Хешування паролів BCrypt (work factor 12)
- ✅ Профілі користувачів з віком, зростом, вагою та рівнем активності
- ✅ Постійні сесії автентифікації

### Харчування та дієта
- 🥗 **Щоденник харчування**: Відстежуйте щоденні прийоми їжі з детальним розбиттям
- 📝 **Власні рецепти**: Створюйте страви з інгредієнтами та порціями
- 📊 **Калькулятор калорій**: Автоматичні щоденні рекомендації на основі:
  - Вік, стать, вага, зріст
  - Рівень активності (сидячий, помірний, активний, дуже активний)
  - Цілі (схуднення, підтримка ваги, набір м'язової маси)

### Новини та інформація
- 📰 **Стрічка новин**: Підібрані статті про фітнес та здоров'я
- 🌐 **Авто-переклад**: Інтеграція з Google Translate API (українська мова)
- 💾 **Розумне кешування**: Кеш новин на Firebase (2 години)

### Аналітика та відстеження
- 📈 **Графіки прогресу**: Візуалізація трендів харчування з LiveCharts
- 📊 **Панель управління**: Огляд щоденної/тижневої статистики
- 🎯 **Прогрес цілей**: Відстеження досягнень

---

## 📸 Скріншоти

<div align="center">

| Вхід | Панель управління | Харчовий щоденник |
|:----:|:-----------------:|:-----------------:|
| <img src=".github/screenshots/login.png" width="250"/> | <img src=".github/screenshots/dashboard.png" width="250"/> | <img src=".github/screenshots/food-diary.png" width="250"/> |

| Рецепти | Конструктор | Новини |
|:-------:|:-----------:|:------:|
| <img src=".github/screenshots/dishes.png" width="250"/> | <img src=".github/screenshots/constructor.png" width="250"/> | <img src=".github/screenshots/news.png" width="250"/> |

</div>
---

## 🛠️ Технології

### Фронтенд
- **WPF** (.NET Framework 4.7.2) - Сучасний Windows UI
- **XAML** - Декларативний дизайн UI
- **MVVM Pattern** - Чиста архітектура
- **LiveCharts** - Візуалізація даних

### Бекенд і сервіси
- **Firebase Firestore** - Хмарна NoSQL база даних
- **Google Cloud Translation API** - Переклад новин
- **NLog** - Структуроване логування з ротацією файлів

### Безпека
- **BCrypt.Net-Next** - Хешування паролів (work factor 12)
- **Змінні середовища** - Безпечне керування обліковими даними
- **SHA256 Fallback** - Зворотна сумісність для існуючих користувачів

### Дані та зберігання
- **Google.Cloud.Firestore** (v3.9.0) - SDK бази даних
- **Newtonsoft.Json** - JSON серіалізація
- **LINQ** - Запити до даних

---

## 🚀 Початок роботи

### Вимоги

- **Windows 10/11** (64-біт)
- **.NET Framework 4.7.2** або вище
- **Visual Studio 2019+** (для розробки)
- **Firebase проєкт** з увімкненим Firestore
- **Google Cloud Translation API** ключ (опціонально, для перекладу новин)

### Встановлення

1. **Клонуйте репозиторій**
   ```bash
   git clone https://github.com/yourusername/Fitpad.git
   cd Fitpad
   ```

2. **Відновіть NuGet пакети**
   ```bash
   nuget restore Fitpad.sln
   ```
   Або у Visual Studio: `ПКМ на Solution → Restore NuGet Packages`

3. **Зберіть рішення**
   ```bash
   msbuild Fitpad.sln /p:Configuration=Release
   ```
   Або у Visual Studio: `Build → Build Solution (Ctrl+Shift+B)`

### Налаштування

#### 1. Налаштування Firebase

1. Створіть Firebase проєкт на [console.firebase.google.com](https://console.firebase.google.com/)
2. Увімкніть **Firestore Database**
3. Згенеруйте **Service Account Key**:
   - Перейдіть до `Project Settings → Service Accounts`
   - Натисніть `Generate New Private Key`
   - Збережіть як `fitpad-YYYY-xxxxxxxx.json`

4. Розмістіть файл облікових даних:
   ```
   Fitpad/Fitpad/Resources/fitpad-YYYY-xxxxxxxx.json
   ```

#### 2. Створіть `secrets.config`

Створіть `Fitpad/Fitpad/secrets.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<appSettings>
  <add key="TranslatorApiKey" value="ВАШ_GOOGLE_TRANSLATE_API_КЛЮЧ" />
  <add key="GoogleCredentialsFileName" value="fitpad-YYYY-xxxxxxxx.json" />
</appSettings>
```

> ⚠️ **Важливо**: `secrets.config` та JSON файли облікових даних в `.gitignore` і **ніколи** не будуть закомічені в Git.

#### 3. Оновіть `App.config` (якщо потрібно)

`ProjectId` вже налаштовано в `App.config`:
```xml
<add key="ProjectId" value="fitpad-2025" />
```

Змініть його відповідно до вашого Firebase project ID, якщо інший.

#### 4. Правила безпеки Firestore

Налаштуйте правила Firestore для ваших колекцій:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Колекція користувачів
    match /Users/{userId} {
      allow read, write: if request.auth != null;
    }
    
    // Кеш новин (публічне читання)
    match /NewsCache/{document=**} {
      allow read: if true;
      allow write: if false; // Тільки на сервері
    }
    
    // Записи щоденника харчування
    match /FoodDiary/{userId}/{entry} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

---

## 🔐 Безпека

Fitpad реалізує кілька найкращих практик безпеки:

### Безпека паролів
- ✅ **BCrypt хешування** (work factor 12) - ~300мс на хеш
- ✅ **Автоматична генерація солі** для кожного пароля
- ✅ **SHA256 fallback** для міграції застарілих паролів
- ❌ **Немає паролів у відкритому вигляді** ніде не зберігаються

### Керування обліковими даними
- ✅ **Конфігурація на основі середовища** через `secrets.config`
- ✅ **Singleton паттерн** для Firebase облікових даних (`FirestoreDbProvider`)
- ✅ **`.gitignore` захист** для конфіденційних файлів
- ✅ **Немає захардкоджених секретів** у вихідному коді

### Логування та моніторинг
- ✅ **NLog структуроване логування** з ротацією файлів
- ✅ **Окремі логи помилок** (90-денне зберігання)
- ✅ **Рівні Debug/Info/Warning/Error/Fatal**
- ✅ **Автоматична архівація логів** (30-денні основні логи)

**Розташування логів**: `Fitpad/bin/Debug/Logs/`

---

## 📁 Структура проєкту

```
Fitpad/
├── Fitpad/
│   ├── Model/
│   │   ├── Entities/          # Моделі даних (UserModel, DishModel, тощо)
│   │   └── Repositories/      # Шар доступу до даних
│   ├── View/
│   │   ├── Pages/             # Основні сторінки застосунку
│   │   └── Components/        # Компоненти UI для повторного використання
│   ├── ViewModel/
│   │   └── PagesViewModels/   # MVVM ViewModels
│   ├── Services/
│   │   ├── FirestoreService.cs
│   │   ├── FirestoreDbProvider.cs  # Singleton менеджер облікових даних
│   │   ├── TranslatorService.cs
│   │   └── RegistrationService.cs
│   ├── Resources/
│   │   └── *.json             # Firebase облікові дані (в gitignore)
│   ├── App.config             # Конфігурація застосунку
│   ├── secrets.config         # API ключі (в gitignore)
│   └── NLog.config            # Конфігурація логування
├── packages/                  # NuGet пакети
└── Fitpad.sln                 # Visual Studio рішення
```

---

## 🤝 Внесок у проєкт

Внески вітаються! Будь ласка, дотримуйтесь цих рекомендацій:

1. **Форкніть** репозиторій
2. **Створіть гілку функції**: `git checkout -b feature/ДивовижнаФункція`
3. **Закомітте зміни**: `git commit -m 'Додати ДивовижнуФункцію'`
4. **Відправте в гілку**: `git push origin feature/ДивовижнаФункція`
5. **Відкрийте Pull Request**

### Рекомендації з розробки

- Дотримуйтесь **MVVM** паттерну
- Використовуйте **async/await** для всіх I/O операцій
- Додавайте **NLog логування** для помилок та важливих подій
- Пишіть **XML коментарі** для публічних API
- Оновлюйте **README** для нових функцій

---

<div align="center">

**Зроблено з ❤️ для ентузіастів фітнесу**

⭐ Поставте зірку цьому репозиторію, якщо він вам допомагає!

</div>
