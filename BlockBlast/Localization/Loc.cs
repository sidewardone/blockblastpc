using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlockBlast.Localization;

public static class Loc
{
    public static Language Current { get; private set; } = DetectSystemLanguage();

    public static event Action? LanguageChanged;

    private static Language DetectSystemLanguage()
    {
        var name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return name == "ru" ? Language.Russian : Language.English;
    }

    public static void Toggle()
    {
        Current = Current == Language.English ? Language.Russian : Language.English;
        LanguageChanged?.Invoke();
    }

    private static readonly Dictionary<string, (string En, string Ru)> Strings = new()
    {
        ["Title"] = ("BlockBlast!", "BlockBlast!"),
        ["Score"] = ("SCORE", "ОЧКИ"),
        ["Best"] = ("BEST", "РЕКОРД"),
        ["GameOver"] = ("Game Over", "Игра окончена"),
        ["YourScore"] = ("Your score", "Ваш счёт"),
        ["BestScoreLabel"] = ("Best score", "Лучший результат"),
        ["Restart"] = ("Start again", "Начать заново"),
        ["Combo"] = ("Combo x{0}", "Комбо x{0}"),
        ["NewBest"] = ("New record!", "Новый рекорд!"),
        ["BoardSize"] = ("Board size", "Размер поля"),
        ["BoardSizeHint"] = ("Starts a new game", "Начинает новую игру"),
        ["Cancel"] = ("Cancel", "Отмена"),
    };

    public static string Get(string key)
    {
        if (!Strings.TryGetValue(key, out var value))
        {
            return key;
        }
        return Current == Language.Russian ? value.Ru : value.En;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
