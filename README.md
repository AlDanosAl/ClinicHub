# ClinicHub — рассылка SMS для клиники

Из чего состоит:

| Проект | Что это |
|---|---|
| `ClinicHub.Api` | Сервер (ASP.NET Core + PostgreSQL). Хранит пациентов и заявки, раздаёт SMS телефонам. |
| `ClinicHub.Admin` | Программа администратора для Windows (заявки, пациенты, телефоны). |
| `ClinicHub.SmsAgent` | Приложение для Android: подключается к серверу и отправляет SMS с SIM телефона. |
| `ClinicHub.Core` | Общие классы (DTO и сущности). |

## 1. Запуск сервера на компьютере

### 1.1. PostgreSQL (один раз)

Сервер хранит данные в PostgreSQL ([ADR-0006](docs/decisions/0006-database.md)).

1. Установите PostgreSQL 17 для Windows (установщик EDB с postgresql.org). При установке запомните пароль пользователя `postgres`, порт оставьте 5432.
2. Создайте пользователя и базу. В PowerShell (путь поправьте под свою версию):

```powershell
cd "C:\Program Files\PostgreSQL\17\bin"
.\psql.exe -U postgres -c "CREATE ROLE clinichub LOGIN PASSWORD 'придумайте-пароль';"
.\psql.exe -U postgres -c "CREATE DATABASE clinichub OWNER clinichub;"
```

### 1.2. Секреты (один раз на компьютер)

Пароли и ключи в git не хранятся. На компьютере разработчика их кладут в `dotnet user-secrets`:

```powershell
cd <папка репозитория>\ClinicHub.Api
dotnet user-secrets set "ConnectionStrings:ClinicHub" "Host=localhost;Database=clinichub;Username=clinichub;Password=придумайте-пароль"
dotnet user-secrets set "Jwt:Secret" "<случайная строка не короче 32 символов>"
```

Все возможные настройки перечислены в `ClinicHub.Api/appsettings.example.json`.
На сервере клиники вместо user-secrets задаются переменные окружения: `ConnectionStrings__ClinicHub`, `Jwt__Secret`.
Без строки подключения сервер не запустится, без `Jwt:Secret` — запустится только в режиме разработки (со случайным ключом до перезапуска).

### 1.3. Запуск

```powershell
cd <папка репозитория>\ClinicHub.Api
dotnet run --launch-profile http
```

Сервер слушает `http://0.0.0.0:5112`. Таблицы создаются сами при первом запуске (миграции EF).

**Первый вход в Admin.** Пароля по умолчанию нет. В пустой базе сервер создаёт администратора `admin`
со случайным паролем и один раз пишет его в консоль и в журнал (`logs/clinichub.log`), ищите строку «Создан первый администратор».
Свой пароль можно задать заранее: `dotnet user-secrets set "Seed:AdminPassword" "..."` или переменная `SEED_ADMIN_PASSWORD`.

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
