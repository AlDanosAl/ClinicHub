namespace ClinicHub.Core;

/// <summary>
/// Приведение российских мобильных номеров к формату E.164 (+79991234567).
/// Используется сайтом (онлайн-запись), позже пригодится импорту таблицы пациентов.
/// </summary>
public static class PhoneNumbers
{
    /// <summary>
    /// Пытается разобрать номер, введённый человеком: «8 (999) 123-45-67», «+7 999 1234567», «9991234567».
    /// Возвращает true и номер в E.164 только для российских мобильных номеров (+79…).
    /// </summary>
    public static bool TryNormalizeRuMobile(string? input, out string e164)
    {
        e164 = "";
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var digits = new char[20];
        var n = 0;
        foreach (var c in input)
        {
            if (char.IsAsciiDigit(c))
            {
                if (n == digits.Length)
                    return false;
                digits[n++] = c;
            }
            else if (c is not (' ' or '-' or '(' or ')' or '+' or '.' or ' '))
            {
                return false;
            }
        }

        string national;
        if (n == 11 && (digits[0] == '7' || digits[0] == '8'))
            national = new string(digits, 1, 10);
        else if (n == 10)
            national = new string(digits, 0, 10);
        else
            return false;

        if (national[0] != '9')
            return false;

        e164 = "+7" + national;
        return true;
    }

    /// <summary>Номер для показа человеку: +7 999 123-45-67. Ожидает E.164.</summary>
    public static string FormatRu(string e164) =>
        e164.Length == 12 && e164.StartsWith("+7")
            ? $"+7 {e164[2..5]} {e164[5..8]}-{e164[8..10]}-{e164[10..12]}"
            : e164;
}
