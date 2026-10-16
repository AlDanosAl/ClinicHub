# Схема модулей и интеграций

Рисованная версия со всеми подписями лежит в `system.html` (артефакт в claude.ai). Ниже то же самое в Mermaid: GitHub рендерит его прямо в браузере.

```mermaid
flowchart TB
  subgraph NET[Интернет]
    PSMS[Телефоны пациентов · SMS]
    PWEB[Пациент · браузер]
    TG[Telegram<br/>бот + канал]
    VK[ВКонтакте<br/>группа, сообщество, канал]
    MAX[MAX<br/>бот + канал]
    PD[ПроДокторов]
    MIS[МИС<br/>облако вендора или локально]
    DK[Dikidi]
    GS[Google-таблица]
  end

  RELAY[Реле / белый IP<br/>только HTTPS сайта, данных нет]

  subgraph CLINIC[Клиника · LAN]
    PHONES[Android-телефоны<br/>SmsAgent]
    ADMINPC[ПК администратора]
    subgraph HUB[Сервер-хаб · ClinicHub .NET 10]
      WEB[Web<br/>сайт, кабинет, веб-админка]
      MSG[Messaging<br/>кампании, маршрутизация]
      CNT[Content<br/>посты, метрики, inbox]
      INT[Integrations<br/>МИС, Dikidi, Sheets]
      Q[Очередь SMS<br/>лимиты SIM]
      CONS[Patients + Consents]
      DB[(PostgreSQL)]
      BK[Резервные копии]
    end
  end

  PWEB -- HTTPS --> RELAY -- туннель --> WEB
  ADMINPC -- LAN/VPN --> WEB
  WEB -- слоты, бронь --> INT
  MSG -- проверка согласий --> CONS
  MSG -- SMS-задачи --> Q
  Q -- задачи, телефон опрашивает --> PHONES
  PHONES -- SMS через оператора --> PSMS
  MSG <-- "Bot API, long polling" --> TG & VK & MAX
  CNT -- посты, метрики, комментарии --> TG & VK & MAX
  INT <-- "API: пациенты, записи, слоты" --> MIS
  MIS <-- штатная синхронизация --> PD
  INT -. только чтение, временно .-> DK
  GS -- импорт --> INT
  DB -- "pg_dump, шифрование" --> BK
```

## Маршрут личного сообщения

```mermaid
flowchart LR
  C[Кампания или авто-правило] --> S{Цель?}
  S -- Marketing --> K{Есть согласие на канал?}
  S -- Service --> R
  K -- нет --> X[Пропуск, в отчёт]
  K -- да --> R[Маршрутизатор]
  R --> T1{Привязан Telegram?} -- да --> TG[Telegram]
  T1 -- нет --> T2{MAX?} -- да --> MX[MAX]
  T2 -- нет --> T3{VK?} -- да --> V[VK]
  T3 -- нет --> SMS[SMS с SIM]
  TG & MX & V -. не доставлено за N ч, критичное .-> SMS
```
