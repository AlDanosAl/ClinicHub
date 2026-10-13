using Android.App;
using Android.Content;

namespace ClinicHub.SmsAgent;

/// <summary>Получает результат отправки SMS (зарегистрирован в манифесте).</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
[IntentFilter(new[] { "ClinicHub.SmsAgent.SMS_SENT" })]
public class SmsSendStatusReceiver : BroadcastReceiver
{
    public const string Action = "ClinicHub.SmsAgent.SMS_SENT";

    public override void OnReceive(Context? context, Intent? intent)
    {
        var code = ResultCode;
        var token = intent?.GetStringExtra("token") ?? "";
        ClinicHub.SmsAgent.Services.AgentLog.Write($"SMS: системный ответ об отправке — код {(int)code}.");
        SmsSender.CompleteSend(token, code, ResultCodeToText(code));
    }

    public static string ResultCodeToText(Result code)
    {
        if (code == Result.Ok)
            return "Отправлено";

        return (int)code switch
        {
            1 => "Общая ошибка отправки (RESULT_ERROR_GENERIC_FAILURE): часто — не выбрана SIM для SMS по умолчанию, запрет оператора или нет денег на счёте",
            2 => "Радио выключено (режим «В самолёте»?)",
            3 => "Пустое сообщение/адресат (PDU)",
            4 => "Нет сети оператора (нет сигнала)",
            5 => "Превышен лимит SMS, заданный системой Android",
            6 => "Номер запрещён (FDN)",
            7 => "Системе запрещены короткие номера",
            8 => "Отправка на этот номер запрещена системой",
            9 => "Радио недоступно",
            10 => "Сеть отклонила SMS",
            11 => "Неверные параметры",
            12 => "Неверное состояние модема",
            13 => "Нет памяти",
            14 => "Неверный формат SMS",
            15 => "Системная ошибка",
            16 => "Ошибка модема",
            17 => "Ошибка сети",
            18 => "Ошибка кодировки",
            19 => "Неверный адрес SMS-центра (SMSC)",
            20 => "Операция запрещена",
            21 => "Внутренняя ошибка",
            22 => "Нет ресурсов",
            23 => "Отменено",
            24 => "Не поддерживается",
            _ => $"Ошибка (код {(int)code})",
        };
    }
}
