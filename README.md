# ClinicHub — рассылка SMS для клиники

Из чего состоит:

| Проект | Что это |
|---|---|
| `ClinicHub.Api` | Сервер (ASP.NET Core + SQL Server LocalDB). Хранит пациентов и заявки, раздаёт SMS телефонам. |
| `ClinicHub.Admin` | Программа администратора для Windows (заявки, пациенты, телефоны). |
| `ClinicHub.SmsAgent` | Приложение для Android: подключается к серверу и отправляет SMS с SIM телефона. |
| `ClinicHub.Core` | Общие классы (DTO и сущности). |
| `ClinicHub.Web` | Сайт клиники (Blazor, черновик): главная и онлайн-запись. См. раздел 6. |

## 1. Запуск сервера на компьютере

```powershell
cd C:\Users\xcx7276\source\repos\ClinicHub.Api
dotnet run --launch-profile http
```

Сервер слушает `http://0.0.0.0:5112`. База создаётся сама при первом запуске.
Вход в Admin по умолчанию: `admin` / `Admin123!`.

Один раз (PowerShell от имени администратора) откройте порты в брандмауэре:

```powershell
netsh advfirewall firewall add rule name="ClinicHub API" dir=in action=allow protocol=TCP localport=5112 profile=any
netsh advfirewall firewall add rule name="ClinicHub discovery" dir=in action=allow protocol=UDP localport=45454 profile=any
```

## 2. Запуск программы администратора

Visual Studio: выберите проект `ClinicHub.Admin`, цель Windows Machine, нажмите «Запуск».
Адрес сервера по умолчанию: `http://localhost:5112`.

## 3. Сборка APK для телефона

```powershell
cd C:\Users\xcx7276\source\repos\ClinicHub.SmsAgent
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

Готовый файл (устанавливайте именно `-Signed`):

```
C:\Users\xcx7276\source\repos\ClinicHub.SmsAgent\bin\Release\net10.0-android\publish\com.companyname.clinichub.smsagent-Signed.apk
```

Скопируйте его на телефон и установите (разрешите установку из неизвестных источников).

## 4. Подключение телефона

1. Телефон и компьютер в одной Wi-Fi сети, сервер запущен.
2. Откройте приложение, нажмите «Подключиться», разрешите SMS и уведомления.
3. Телефон появится на вкладке «Телефоны». Дальше приложение работает в фоне.

На Android 13+ для приложения из apk-файла может понадобиться: «О приложении» → три точки → «Разрешить ограниченные настройки», затем выдать разрешение на SMS.

## 5. Журналы и отладка

- Общий журнал сервера и телефонов: `ClinicHub.Api\logs\clinichub.log`.
- С самого компьютера: `http://localhost:5112/api/debug/log` и `http://localhost:5112/api/debug/state`.
- Проверка доступности сервера с телефона (в браузере): `http://<IP компьютера>:5112/api/health` — должно быть `ok`.

## 6. Сайт (`ClinicHub.Web`)

```powershell
cd ClinicHub.Web
dotnet run --launch-profile http
```

Сайт откроется на `http://localhost:5242`. Он пока не связан с сервером: онлайн-запись работает в демо-режиме
(выдуманное расписание, заявка никуда не уходит, на странице висит предупреждение).

- Тексты, услуги, врачи, контакты лежат в `ClinicHub.Web/site-content.json`. Значения в `[КВАДРАТНЫХ СКОБКАХ]` — заглушки.
  Файл перечитывается на лету, перезапуск не нужен.
- Источник свободного времени — интерфейс `IBookingSource`. Сейчас `DemoBookingSource`, после выбора МИС будет её адаптер.
- Публичные страницы работают без JavaScript (static SSR), поэтому их нормально индексируют поисковики.
