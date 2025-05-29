// Например, в файле Stat_reports/Utils/PeriodHelper.cs
using System;
using System.Globalization;
using Core.Enums; // Убедитесь, что пространство имен для DeadlineType верное

namespace Stat_reports.Utils // Или ваше основное пространство имен проекта
{
    public static class PeriodHelper
    {
        private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

        public static string FormatReportPeriod(DateTime periodDate, DeadlineType deadlineType)
        {
            int year = periodDate.Year;
            // DateTime.Month 1-январь, ..., 12-декабрь
            int month = periodDate.Month;

            switch (deadlineType)
            {
                case DeadlineType.Monthly:
                    // Получаем название месяца в именительном падеже и с заглавной буквы
                    string monthNameNominative = RussianCulture.DateTimeFormat.GetMonthName(month);
                    monthNameNominative = char.ToUpper(monthNameNominative[0], RussianCulture) + monthNameNominative.Substring(1);
                    return $"{monthNameNominative} {year} г. ";

                case DeadlineType.Quarterly:
                    string quarterStr;
                    string startMonthName, endMonthName;
                    if (month >= 1 && month <= 3) // Q1
                    {
                        startMonthName = RussianCulture.DateTimeFormat.GetMonthName(1);
                        endMonthName = RussianCulture.DateTimeFormat.GetMonthName(3);
                    }
                    else if (month >= 4 && month <= 6) // Q2
                    {
                        startMonthName = RussianCulture.DateTimeFormat.GetMonthName(4);
                        endMonthName = RussianCulture.DateTimeFormat.GetMonthName(6);
                    }
                    else if (month >= 7 && month <= 9) // Q3
                    {
                        startMonthName = RussianCulture.DateTimeFormat.GetMonthName(7);
                        endMonthName = RussianCulture.DateTimeFormat.GetMonthName(9);
                    }
                    else // Q4 (month >= 10 && month <= 12)
                    {
                        startMonthName = RussianCulture.DateTimeFormat.GetMonthName(10);
                        endMonthName = RussianCulture.DateTimeFormat.GetMonthName(12);
                    }
                    // Форматируем с заглавной буквы
                    startMonthName = char.ToUpper(startMonthName[0], RussianCulture) + startMonthName.Substring(1);
                    endMonthName = char.ToUpper(endMonthName[0], RussianCulture) + endMonthName.Substring(1);
                    quarterStr = $"Январь-{endMonthName}";
                    return $"{quarterStr} {year} г. " ;

                case DeadlineType.HalfYearly:
                    string halfYearStr = (month >= 1 && month <= 6) ? "Январь-Июнь" : "Январь-Декабрь";
                    return $"{halfYearStr} {year} г.";

                case DeadlineType.Yearly:
                    return $"{year} г.";

                default:
                    // Резервный вариант, если тип не определен
                    return periodDate.ToString("dd.MM.yyyy", RussianCulture);
            }
        }
    }
}