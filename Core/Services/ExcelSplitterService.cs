using ClosedXML.Excel;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions; // Необходимо для работы с JSON

namespace Core.Services
{
    // Классы для десериализации JSON-конфигурации
    public class ColumnRule
    {
        public string HeaderName { get; set; }
        public string Action { get; set; } // "TakeHighest", "Copy"
    }

    public class ReportConfig
    {
        public string DefaultKeyColumnIdentifier { get; set; } = "Б";
        public string DefaultKeyColumnName { get; set; } = "Код";
        public List<ColumnRule> ColumnRules { get; set; } = new List<ColumnRule>();
    }

    public class ExcelSplitterService : IExcelSplitterService
    {
        private readonly ReportConfig _config;
        private const int HeaderRow1 = 7;
        private const int HeaderRow2 = 8;
        private const int DataStartRow = 9; // Данные начинаются с 9-й строки (или 10-й, если заголовков 2)

        public ExcelSplitterService()
        {
            // Загружаем конфигурацию из файла. Убедитесь, что файл существует.
            // Путь к файлу можно вынести в appsettings.json
            try
            {
                var configJson = File.ReadAllText("column_config.json");
                _config = JsonSerializer.Deserialize<ReportConfig>(configJson);
            }
            catch (Exception)
            {
                // Если файл не найден или ошибка, используем конфигурацию по умолчанию
                _config = new ReportConfig();
            }
        }

        public byte[] ProcessReports(List<string> filePaths, string templatePath, int year, int? month, int? quarter, int? halfYear, string signatureFilePath)
        {
            using (var resultWorkbook = new XLWorkbook(templatePath))
            {
                // Данные всегда начинаются со второго листа
                var sheetNamesToProcess = resultWorkbook.Worksheets
                    .Skip(1)
                    .ToDictionary(ws => ws.Name, ws => ws);

                foreach (var filePath in filePaths)
                {
                    using (var sourceWorkbook = new XLWorkbook(filePath))
                    {
                        // Проходим по всем листам в файле отчета, кроме первого
                        foreach (var sourceWorksheet in sourceWorkbook.Worksheets.Skip(1))
                        {
                            if (sheetNamesToProcess.TryGetValue(sourceWorksheet.Name, out var targetWorksheet))
                            {
                                ConsolidateData(sourceWorksheet, targetWorksheet);
                            }
                        }
                    }
                }

                InsertPeriodDescription(resultWorkbook, year, month, quarter, halfYear);
                InsertSignature(resultWorkbook, signatureFilePath);

                using (var memoryStream = new MemoryStream())
                {
                    resultWorkbook.SaveAs(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        private void ConsolidateData(IXLWorksheet sourceWorksheet, IXLWorksheet targetWorksheet)
        {
            var columnMap = MapColumns(sourceWorksheet);
            int keyColumn = columnMap.FirstOrDefault(c => c.Value.IsKey).Key;
            if (keyColumn == 0) return;

            // Определяем строку заголовков и начала данных в шаблоне
            int templateHeaderRow = DetectHeaderRow(targetWorksheet);
            int templateDataStartRow = templateHeaderRow + 1;

            // Карта строк по ключу в шаблоне
            var targetDataMap = new Dictionary<string, IXLRow>();
            var targetRows = targetWorksheet.RowsUsed().Where(r => r.RowNumber() >= templateDataStartRow);

            foreach (var row in targetRows)
            {
                var keyValue = row.Cell(keyColumn).GetString().Trim();
                if (!string.IsNullOrEmpty(keyValue) && !targetDataMap.ContainsKey(keyValue))
                    targetDataMap[keyValue] = row;
            }

            // Поиск строки для добавления новых записей
            int lastRow = targetWorksheet.LastRowUsed().RowNumber();

            // Определяем строку заголовков и начала данных в источнике
            int headerRow = DetectHeaderRow(sourceWorksheet);
            int dataStartRow = headerRow + 1;
            var sourceRows = sourceWorksheet.RowsUsed(r => r.RowNumber() >= dataStartRow);

            foreach (var sourceRow in sourceRows)
            {
                var sourceKey = sourceRow.Cell(keyColumn).GetString().Trim();
                if (string.IsNullOrEmpty(sourceKey)) continue;

                if (targetDataMap.TryGetValue(sourceKey, out var targetRow))
                {
                    // Обновляем существующую строку
                    foreach (var col in columnMap)
                    {
                        var colNum = col.Key;
                        var action = col.Value.Action;
                        var sourceCell = sourceRow.Cell(colNum);
                        var targetCell = targetRow.Cell(colNum);

                        switch (action)
                        {
                            case ColumnAction.Sum:
                                if (sourceCell.TryGetValue(out double sourceVal))
                                {
                                    targetCell.TryGetValue(out double targetVal);
                                    targetCell.Value = targetVal + sourceVal;
                                }
                                break;

                            case ColumnAction.TakeHighest:
                                if (sourceCell.TryGetValue(out double newVal))
                                {
                                    targetCell.TryGetValue(out double oldVal);
                                    targetCell.Value = Math.Max(oldVal, newVal);
                                }
                                break;

                                // 'Copy' и 'Key' игнорируем при обновлении
                        }
                    }
                }
                else
                {
                    // Ключа нет — добавляем в конец таблицы
                    lastRow++;
                    targetRow = targetWorksheet.Row(lastRow);

                    foreach (var cell in sourceRow.Cells())
                    {
                        var targetCell = targetRow.Cell(cell.Address.ColumnNumber);

                        // Копируем значение
                        targetCell.Value = cell.Value;

                        // Копируем стили
                        targetCell.Style = cell.Style;

                        // Копируем границы
                        targetCell.Style.Border.TopBorder = cell.Style.Border.TopBorder;
                        targetCell.Style.Border.BottomBorder = cell.Style.Border.BottomBorder;
                        targetCell.Style.Border.LeftBorder = cell.Style.Border.LeftBorder;
                        targetCell.Style.Border.RightBorder = cell.Style.Border.RightBorder;

                        // Цвета границ (если используются)
                        targetCell.Style.Border.TopBorderColor = cell.Style.Border.TopBorderColor;
                        targetCell.Style.Border.BottomBorderColor = cell.Style.Border.BottomBorderColor;
                        targetCell.Style.Border.LeftBorderColor = cell.Style.Border.LeftBorderColor;
                        targetCell.Style.Border.RightBorderColor = cell.Style.Border.RightBorderColor;
                    }

                }
            }

                targetWorksheet.Columns().AdjustToContents();
        }

        

        private Dictionary<int, ColumnProcessingInfo> MapColumns(IXLWorksheet worksheet)
        {
            var map = new Dictionary<int, ColumnProcessingInfo>();
            // Определяем строку с порядковыми номерами (А, Б, В... или 1, 2, 3...)
            // Предполагаем, что это 8-я или 9-я строка
            var identifierRow = worksheet.Row(HeaderRow2);
            if (identifierRow.CellsUsed().All(c => c.IsEmpty()))
            {
                identifierRow = worksheet.Row(DataStartRow - 1);
            }

            foreach (var cell in identifierRow.CellsUsed())
            {
                int colNum = cell.Address.ColumnNumber;
                string headerText = worksheet.Cell(HeaderRow1, colNum).GetString().Trim();
                if (string.IsNullOrEmpty(headerText))
                {
                    headerText = worksheet.Cell(HeaderRow2, colNum).GetString().Trim();
                }

                var info = new ColumnProcessingInfo();
                var rule = _config.ColumnRules.FirstOrDefault(r => r.HeaderName.Equals(headerText, StringComparison.OrdinalIgnoreCase));

                // Проверяем, является ли столбец ключевым
                if (cell.GetString().Trim().Equals(_config.DefaultKeyColumnIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    headerText.Equals(_config.DefaultKeyColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    info.Action = ColumnAction.Key;
                    info.IsKey = true;
                }
                // Проверяем, есть ли для столбца особое правило в конфиге
                else if (rule != null)
                {
                    info.Action = Enum.TryParse<ColumnAction>(rule.Action, true, out var action) ? action : ColumnAction.Copy;
                }
                // Определяем действие по типу идентификатора (буква или цифра)
                else if (double.TryParse(cell.GetString(), out _))
                {
                    info.Action = ColumnAction.Sum; // Цифра - суммировать
                }
                else
                {
                    info.Action = ColumnAction.Copy; // Буква - копировать
                }
                map[colNum] = info;
            }
            return map;
        }
        private int DetectHeaderRow(IXLWorksheet worksheet)
        {
            for (int i = 7; i <= 10; i++) // диапазон, в котором обычно бывают заголовки
            {
                var row = worksheet.Row(i);
                var nonEmptyCells = row.CellsUsed().ToList();

                if (nonEmptyCells.Count >= 3) // хотя бы 3 колонки, иначе это не заголовок
                {
                    bool isHeaderRow = nonEmptyCells.All(cell =>
                    {
                        var value = cell.GetString().Trim();
                        return Regex.IsMatch(value, @"^[А-Яа-яA-Za-z]+$") || Regex.IsMatch(value, @"^\d+$");
                    });

                    if (isHeaderRow)
                        return i;
                }
            }

            return 9; // если не нашли — по умолчанию 9
        }
        private void InsertSignature(IXLWorkbook workbook, string signatureFilePath)
        {
            if (!File.Exists(signatureFilePath)) return;

            var lastWorksheet = workbook.Worksheets.Last();
            // Определяем целевую ячейку с отступом в 2 пустые строки
            var targetCell = lastWorksheet.Cell(lastWorksheet.LastRowUsed().RowNumber() + 3, 1);

            using (var signatureWb = new XLWorkbook(signatureFilePath))
            {
                var signatureSheet = signatureWb.Worksheet(1);
                var sourceRange = signatureSheet.RangeUsed();

                // --- ИСПРАВЛЕНИЕ ---
                // Копируем исходный диапазон в целевую ячейку
                sourceRange.CopyTo(targetCell);
            }
        }

        // --- Вспомогательные классы и перечисления ---
        private enum ColumnAction { Key, Copy, Sum, TakeHighest }
        private class ColumnProcessingInfo
        {
            public ColumnAction Action { get; set; }
            public bool IsKey { get; set; }
        }

        // --- Ваши существующие методы без изменений ---
        private void InsertPeriodDescription(IXLWorkbook workbook, int year, int? month, int? quarter, int? halfYear)
        {
            var worksheet = workbook.Worksheet(1); // Первый лист
            string periodText = GetPeriodText(year, month, quarter, halfYear);
            var cell = worksheet.CellsUsed()
                .FirstOrDefault(c => c.GetString().Trim().EndsWith(" за"));

            if (cell != null)
            {
                cell.Value = $"{cell.GetString().Split("за")[0].Trim()} за {periodText}";
            }
        }

        private string GetPeriodText(int year, int? month, int? quarter, int? halfYear)
        {
            if (month != null)
            {
                string monthName = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month.Value);
                return $"{monthName} {year}";
            }
            if (quarter != null)
            {
                return quarter switch
                {
                    1 => $"январь-март {year}",
                    2 => $"январь-июнь {year}",
                    3 => $"январь-сентябрь {year}",
                    4 => $"январь-декабрь {year}",
                    _ => $"квартал {quarter} {year}"
                };
            }
            if (halfYear != null)
            {
                return halfYear == 1 ? $"январь-июнь {year}" : $"январь-декабрь {year}";
            }
            return $"{year} год";
        }
    }
}