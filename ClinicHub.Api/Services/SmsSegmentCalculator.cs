namespace ClinicHub.Api.Services;

/// <summary>Расчёт числа SMS-сегментов по кодировке (REQ-BALANCE, п.3.5 ТЗ).</summary>
public static class SmsSegmentCalculator
{
    private const int GsmSingle = 160;
    private const int GsmMultipart = 153;
    private const int UnicodeSingle = 70;
    private const int UnicodeMultipart = 67;

    private static readonly HashSet<char> Gsm7 = new(
        "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà");

    public static bool IsGsm7(string text)
    {
        return text.All(c => Gsm7.Contains(c));
    }

    public static int CountSegments(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var gsm7 = IsGsm7(text);
        var (single, multipart) = gsm7
            ? (GsmSingle, GsmMultipart)
            : (UnicodeSingle, UnicodeMultipart);

        if (text.Length <= single)
            return 1;

        return (int)Math.Ceiling(text.Length / (double)multipart);
    }
}