using Android.App;
using Android.Content;
using Android.OS;
using Android.Telephony;
using ClinicHub.SmsAgent.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ClinicHub.SmsAgent;

/// <summary>Отправка SMS через SmsManager с учётом выбранной SIM (SubscriptionId). Каждый шаг пишется в журнал.</summary>
public sealed class SmsSender
{
    public const string ExtraToken = "token";

    private sealed class PendingSend
    {
        public int Remaining;
        public readonly TaskCompletionSource<(Result Code, string Message)> Tcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static readonly ConcurrentDictionary<string, PendingSend> Pending = new();

    /// <summary>Вызывается получателем системного ответа о каждой отправленной части сообщения.</summary>
    public static void CompleteSend(string token, Result code, string message)
    {
        if (!Pending.TryGetValue(token, out var p))
        {
            AgentLog.Write($"SMS: пришёл ответ системы (код {(int)code}: {message}), но ожидающей отправки нет — возможно, уже истёк таймаут.");
            return;
        }

        if (code != Result.Ok)
        {
            Pending.TryRemove(token, out _);
            p.Tcs.TrySetResult((code, message));
            return;
        }

        if (Interlocked.Decrement(ref p.Remaining) <= 0)
        {
            Pending.TryRemove(token, out _);
            p.Tcs.TrySetResult((Result.Ok, message));
        }
    }

    /// <summary>
    /// Отправляет текст. Возвращает (успех, человекочитаемая ошибка или "Отправлено").
    /// subscriptionId = null => SIM по умолчанию.
    /// </summary>
    public static async Task<(bool Success, string Message)> SendAsync(
        string phone, string text, int? subscriptionId, long requestId, CancellationToken ct)
    {
        var ctx = Android.App.Application.Context;

        if (!MainActivity.HasSendSmsPermission(ctx))
        {
            const string msg = "Нет разрешения на отправку SMS (SEND_SMS). Откройте Настройки → Приложения → ClinicHub.SmsAgent → Разрешения → SMS → «Разрешить». " +
                               "На Android 13+ для приложений, установленных из apk-файла, сначала в «О приложении» (три точки) нажмите «Разрешить ограниченные настройки».";
            AgentLog.Write("SMS: " + msg);
            return (false, msg);
        }

        var to = Regex.Replace(phone ?? "", @"[^\d+]", "");
        if (to.Length == 0)
        {
            AgentLog.Write($"SMS: пустой номер получателя (исходное значение «{phone}»).");
            return (false, "Пустой номер получателя");
        }
        if (string.IsNullOrEmpty(text))
        {
            AgentLog.Write("SMS: пустой текст сообщения.");
            return (false, "Пустой текст сообщения");
        }

        // Выбор менеджера SMS: конкретная SIM, иначе системный по умолчанию.
        SmsManager? sms = null;
        var how = "";
        if (subscriptionId.HasValue && subscriptionId.Value >= 0)
        {
            try
            {
                sms = SmsManager.GetSmsManagerForSubscriptionId(subscriptionId.Value);
                how = $"SIM subscriptionId={subscriptionId.Value}";
            }
            catch (Exception ex)
            {
                AgentLog.Write($"SMS: SIM subscriptionId={subscriptionId.Value} недоступна ({ex.Message}), беру SIM по умолчанию.");
            }
        }
        if (sms is null && Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            try
            {
                sms = ctx.GetSystemService(Java.Lang.Class.FromType(typeof(SmsManager))) as SmsManager;
                if (sms is not null)
                    how = "системный SmsManager (Android 12+)";
            }
            catch (Exception ex)
            {
                AgentLog.Write("SMS: системный SmsManager недоступен: " + ex.Message);
            }
        }
        if (sms is null)
        {
            sms = SmsManager.Default;
            how = "SmsManager.Default";
        }

        var parts = sms.DivideMessage(text);
        var partCount = Math.Max(parts.Count, 1);
        var token = $"{requestId}:{System.Environment.TickCount64}";

        var pending = new PendingSend { Remaining = partCount };
        Pending[token] = pending;

        AgentLog.Write($"SMS: задача #{requestId}: кому «{to}» (исходно «{phone}»), {text.Length} симв., частей {partCount}, способ: {how}.");

        PendingIntent MakeIntent()
        {
            var intent = new Intent(SmsSendStatusReceiver.Action)
                .SetPackage(Android.App.Application.Context.PackageName)
                .PutExtra(ExtraToken, token);
            return PendingIntent.GetBroadcast(
                Android.App.Application.Context, (int)(requestId % int.MaxValue), intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            if (partCount == 1)
            {
                sms.SendTextMessage(to, null, text, MakeIntent(), null);
            }
            else
            {
                var sentIntents = new List<PendingIntent>(partCount);
                for (var i = 0; i < partCount; i++)
                    sentIntents.Add(MakeIntent());
                sms.SendMultipartTextMessage(to, null, parts, sentIntents, null);
            }
            AgentLog.Write("SMS: система приняла запрос на отправку, жду подтверждения…");
        }
        catch (Exception ex)
        {
            Pending.TryRemove(token, out _);
            AgentLog.Write($"SMS: системный вызов отправки упал: {ex.GetType().Name}: {ex.Message}");
            return (false, "Ошибка вызова SMS API: " + ex.Message);
        }

        try
        {
            var (code, message) = await pending.Tcs.Task.WaitAsync(TimeSpan.FromSeconds(60), ct);
            AgentLog.Write($"SMS: подтверждение за {sw.ElapsedMilliseconds} мс — код {(int)code}: {message}.");
            return (code == Result.Ok, message);
        }
        catch (TimeoutException)
        {
            Pending.TryRemove(token, out _);
            AgentLog.Write("SMS: система не подтвердила отправку за 60 секунд (SMS, возможно, не ушло).");
            return (false, "Нет ответа от системы (таймаут)");
        }
        catch (System.OperationCanceledException)
        {
            Pending.TryRemove(token, out _);
            return (false, "Отменено");
        }
    }
}
