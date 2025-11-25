using ClosedXML.Excel;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Core.Services
{
    public class ColumnRule
    {
        public string HeaderName { get; set; }
        public string Action { get; set; }
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

        public ExcelSplitterService()
        {
            try
            {
                var configJson = File.ReadAllText("column_config.json");
                _config = JsonSerializer.Deserialize<ReportConfig>(configJson);
            }
            catch
            {
                _config = new ReportConfig();
            }
        }

        public byte[] ProcessReports(List<string> filePaths, string templatePath, int year, int? month, int? quarter, int? halfYear, string signatureFilePath)
        {
            using var resultWorkbook = new XLWorkbook(templatePath);
            var sheetNamesToProcess = resultWorkbook.Worksheets.Skip(1).ToDictionary(ws => ws.Name, ws => ws);

            foreach (var filePath in filePaths)
            {
                using var sourceWorkbook = new XLWorkbook(filePath);
                foreach (var sourceWorksheet in sourceWorkbook.Worksheets.Skip(1))
                {
                    if (sheetNamesToProcess.TryGetValue(sourceWorksheet.Name, out var targetWorksheet))
                    {
                        ConsolidateData(sourceWorksheet, targetWorksheet);
                    }
                }
            }

            InsertPeriodDescription(resultWorkbook, year, month, quarter, halfYear);
            InsertSignature(resultWorkbook, signatureFilePath);

            using var memoryStream = new MemoryStream();
            resultWorkbook.SaveAs(memoryStream);
            return memoryStream.ToArray();
        }

        private void ConsolidateData(IXLWorksheet sourceWorksheet, IXLWorksheet targetWorksheet)
        {
            var headerRow = 7;
            var identifierRow = DetectIdentifierRow(sourceWorksheet);
            var dataStartRow = identifierRow + 1;

            var columnMap = MapColumns(sourceWorksheet, identifierRow);
            int keyColumn = columnMap.FirstOrDefault(c => c.Value.IsKey).Key;
            if (keyColumn == 0) return;

            int targetIdentifierRow = DetectIdentifierRow(targetWorksheet);
            int targetDataStartRow = targetIdentifierRow + 1;

            var targetDataMap = targetWorksheet.RowsUsed().Where(r => r.RowNumber() >= targetDataStartRow)
                .GroupBy(r => r.Cell(keyColumn).GetString().Trim())
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.First());

            int lastRow = targetWorksheet.LastRowUsed().RowNumber();

            var sourceRows = sourceWorksheet.RowsUsed().Where(r => r.RowNumber() >= dataStartRow);
            foreach (var sourceRow in sourceRows)
            {
                var sourceKey = sourceRow.Cell(keyColumn).GetString().Trim();
                if (string.IsNullOrEmpty(sourceKey)) continue;

                if (targetDataMap.TryGetValue(sourceKey, out var targetRow))
                {
                    foreach (var col in columnMap)
                    {
                        var colNum = col.Key;
                        var action = col.Value.Action;
                        var sourceCell = sourceRow.Cell(colNum);
                        var targetCell = targetRow.Cell(colNum);

                        switch (action)
                        {
                            case ColumnAction.Sum:
                                if (sourceCell.TryGetValue(out double sVal))
                                {
                                    targetCell.TryGetValue(out double tVal);
                                    targetCell.Value = tVal + sVal;
                                }
                                break;
                            case ColumnAction.TakeHighest:
                                if (sourceCell.TryGetValue(out double nVal))
                                {
                                    targetCell.TryGetValue(out double oVal);
                                    targetCell.Value = Math.Max(oVal, nVal);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    lastRow++;
                    var newRow = targetWorksheet.Row(lastRow);
                    foreach (var cell in sourceRow.Cells())
                    {
                        var tCell = newRow.Cell(cell.Address.ColumnNumber);
                        tCell.Value = cell.Value;
                        tCell.Style = cell.Style;
                        tCell.Style.Border = cell.Style.Border;
                    }
                }
            }

            //targetWorksheet.Columns().AdjustToContents();
        }

        private int DetectIdentifierRow(IXLWorksheet worksheet)
        {
            for (int i = 8; i <= 10; i++)
            {
                var row = worksheet.Row(i);
                var values = row.CellsUsed().Select(c => c.GetString().Trim()).ToList();
                if (values.Count >= 2 && values.Any(v => Regex.IsMatch(v, "^[А-ЯA-Zа-яa-z]$")) && values.Any(v => Regex.IsMatch(v, @"^\d+$")))
                    return i;
            }
            return 9;
        }

        private Dictionary<int, ColumnProcessingInfo> MapColumns(IXLWorksheet worksheet, int identifierRowNum)
        {
            var map = new Dictionary<int, ColumnProcessingInfo>();
            var identifierRow = worksheet.Row(identifierRowNum);
            var headerRow = worksheet.Row(7); // заголовки всегда на 7 строке

            foreach (var cell in identifierRow.CellsUsed())
            {
                int colNum = cell.Address.ColumnNumber;
                string headerText = headerRow.Cell(colNum).GetString().Trim();
                string identifierText = cell.GetString().Trim(); // Получаем текст идентификатора

                var info = new ColumnProcessingInfo();

                // 1. Проверяем, является ли столбец ключевым по DefaultKeyColumnIdentifier
                if (identifierText.Equals(_config.DefaultKeyColumnIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    info.Action = ColumnAction.Key;
                    info.IsKey = true;
                }
                // 2. Иначе, проверяем правила из ColumnRules по HeaderName
                else
                {
                    var rule = _config.ColumnRules.FirstOrDefault(r => r.HeaderName.Equals(headerText, StringComparison.OrdinalIgnoreCase));
                    if (rule != null)
                    {
                        info.Action = Enum.TryParse(rule.Action, true, out ColumnAction parsed) ? parsed : ColumnAction.Copy;
                    }
                    // 3. Иначе, если идентификатор является числом, применяем Sum
                    else if (double.TryParse(identifierText, out _))
                    {
                        info.Action = ColumnAction.Sum;
                    }
                    // 4. По умолчанию - копируем
                    else
                    {
                        info.Action = ColumnAction.Copy;
                    }
                }
                map[colNum] = info;
            }
            return map;
        }

        private void InsertSignature(IXLWorkbook workbook, string signatureFilePath)
        {
            if (!File.Exists(signatureFilePath)) return;

            var lastWorksheet = workbook.Worksheets.Last();
            var insertRow = lastWorksheet.LastRowUsed()?.RowNumber() + 3 ?? 3;

            using var signatureWb = new XLWorkbook(signatureFilePath);
            var signatureSheet = signatureWb.Worksheet(1);
            var rangeUsed = signatureSheet.RangeUsed();
            var firstCell = rangeUsed.FirstCell();
            var lastCell = signatureSheet.Cell(rangeUsed.LastRow().RowNumber(), rangeUsed.LastColumn().ColumnNumber() + 1);
            var sourceRange = signatureSheet.Range(firstCell, lastCell);


            int rowOffset = insertRow - sourceRange.FirstRow().RowNumber();
            int colOffset = 0;
            for (int col = 1; col <= sourceRange.ColumnCount(); col++)
            {
                var targetColNumber = col + colOffset;
                double templateWidth = lastWorksheet.Column(targetColNumber).Width;

                // Принудительно задаём ширину колонки в подписи равной ширине колонки в шаблоне
                signatureSheet.Column(col).Width = templateWidth;
            }
            // 1. Копируем значения, стили, высоту строк и ширину столбцов
            foreach (var sourceRow in sourceRange.Rows())
            {
                // Получаем IXLRow (не IXLRangeRow)
                var sourceWorksheetRow = signatureSheet.Row(sourceRow.RowNumber());
                var targetRow = lastWorksheet.Row(sourceRow.RowNumber() + rowOffset);
                targetRow.Height = sourceWorksheetRow.Height; // Высота строк копируется из подписи

                foreach (var sourceCell in sourceRow.Cells())
                {
                    var targetCell = lastWorksheet.Cell(targetRow.RowNumber(), sourceCell.Address.ColumnNumber + colOffset);
                    targetCell.Value = sourceCell.Value; // Значение копируется из подписи
                    targetCell.Style = sourceCell.Style; // Стили (включая границы) копируются из подписи


                    // var targetCol = sourceCell.Address.ColumnNumber + colOffset;
                    // lastWorksheet.Column(targetCol).Width = signatureSheet.Column(sourceCell.Address.ColumnNumber).Width;
                }
            }

            // 2. Копируем объединённые ячейки
            var mergedRanges = signatureSheet.MergedRanges
                .Where(r => r.RangeAddress.Intersects(sourceRange.RangeAddress));

            foreach (var mergedRange in mergedRanges)
            {
                var first = mergedRange.FirstCell();
                var last = mergedRange.LastCell();

                var newFirst = lastWorksheet.Cell(first.Address.RowNumber + rowOffset, first.Address.ColumnNumber + colOffset);
                var newLast = lastWorksheet.Cell(last.Address.RowNumber + rowOffset, last.Address.ColumnNumber + colOffset);

                lastWorksheet.Range(newFirst, newLast).Merge();
            }
        }

        public byte[] ProcessFixedStructureReport(List<string> filePaths, string templatePath, int year, int month, string signatureFilePath)
        {
            using var resultWorkbook = new XLWorkbook(templatePath);
            var targetWorksheet = resultWorkbook.Worksheet(1);

            int startRow = 2;
            int endRow = 48;
            int sourceColIndex = 4;   // Столбец D
            int targetColIndex = 4;   // Столбец D


            // Если список файлов пуст, возвращаем пустой шаблон.
            if (filePaths == null || filePaths.Count == 0)
            {
                using var tempMs = new MemoryStream();
                resultWorkbook.SaveAs(tempMs);
                return tempMs.ToArray();
            }

            foreach (var filePath in filePaths)
            {

                if (!File.Exists(filePath))
                {
                    continue;
                }

                using var sourceWorkbook = new XLWorkbook(filePath);
                var sourceWorksheet = sourceWorkbook.Worksheet(1);

                if (sourceWorksheet == null) continue;

                for (int row = startRow; row <= endRow; row++)
                {
                    var sourceCell = sourceWorksheet.Cell(row, sourceColIndex);
                    var sourceValue = sourceCell.Value;


                    if (!sourceValue.IsBlank)
                    {
                        var targetCell = targetWorksheet.Cell(row, targetColIndex);
                        targetCell.Value = sourceValue;
                        targetCell.Style = sourceCell.Style;
                    }
                }

                targetColIndex++;
            }


            // --- НОВАЯ ЛОГИКА: УСТАНОВКА ГРАНИЦ ДЛЯ ВСЕЙ ОБЛАСТИ ДАННЫХ ---

            // Последний заполненный столбец - это targetColIndex - 1, 
            // поскольку targetColIndex указывает на следующий свободный столбец.
            int lastFilledCol = targetColIndex - 1;
            int firstCol = 1; // Столбец А

            // Создаем диапазон, например, от A2 до (последний столбец)48
            var fullRangeWithBorders = targetWorksheet.Range(startRow, firstCol, endRow, lastFilledCol);

            // Устанавливаем тонкие границы для всего диапазона (снаружи и внутри)
            fullRangeWithBorders.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            fullRangeWithBorders.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // --- КОНЕЦ НОВОЙ ЛОГИКИ ---


            InsertPeriodDescription(resultWorkbook, year, month, null, null);

            // ЕДИНСТВЕННОЕ правильное место для объявления ms
            using var ms = new MemoryStream();
            resultWorkbook.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ProcessSummaryExcelReport(
            List<string> filePaths,
            string templatePath,
            int year,
            int month,
            string signatureFilePath)
        {
            // Предполагается, что у вас есть using ClosedXML.Excel;
            using var resultWorkbook = new XLWorkbook(templatePath);
            var targetWorksheet = resultWorkbook.Worksheet(1);

            int startRow = 2;  // Строка заголовка / начала данных
            int endRow = 48;   // Строка знаменателя
            int sourceColIndex = 4; // Столбец D в исходном отчете

            // Переменная для отслеживания текущего столбца вставки данных
            int currentTargetColIndex = 4;

            // Список для сохранения индексов столбцов, куда были вставлены данные (D, F, H, ...)
            var dataColumnIndexes = new List<int>();

            // Если список файлов пуст, вернуть пустой шаблон
            if (filePaths == null || filePaths.Count == 0)
            {
                using var tempMs = new MemoryStream();
                resultWorkbook.SaveAs(tempMs);
                return tempMs.ToArray();
            }

            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                using var sourceWorkbook = new XLWorkbook(filePath);
                var sourceWorksheet = sourceWorkbook.Worksheet(1);

                if (sourceWorksheet == null)
                    continue;

                // !!! Запоминаем индекс столбца с данными, перед тем как его сдвинуть !!!
                dataColumnIndexes.Add(currentTargetColIndex);

                // 1. Копирование данных
                for (int row = startRow; row <= endRow; row++)
                {
                    var sourceCell = sourceWorksheet.Cell(row, sourceColIndex);
                    var sourceValue = sourceCell.Value;

                    // Копируем значение и стиль, включая заголовок (R2) и знаменатель (R48)
                    if (!sourceValue.IsBlank || row == startRow)
                    {
                        var targetCell = targetWorksheet.Cell(row, currentTargetColIndex);
                        targetCell.Value = sourceValue;
                        targetCell.Style = sourceCell.Style;
                    }
                }

                // 2. Вставка заголовка и формул в следующий столбец (проценты)
                int dataCol = currentTargetColIndex;
                int formulaCol = currentTargetColIndex + 1; // Столбец для формул (E, G, I, ...)

                // Установка ширины столбца для процентов
                targetWorksheet.Column(formulaCol).Width = 12;

                // Получаем значение заголовка из текущего столбца данных (R2)
                var dataHeaderCell = targetWorksheet.Cell(startRow, dataCol);
                var formulaHeaderCell = targetWorksheet.Cell(startRow, formulaCol);

                // Копируем текст и добавляем "%"
                formulaHeaderCell.Value = $"{dataHeaderCell.GetText()} %";

                // Копируем стиль заголовка (шрифт, цвет и т.д.)
                formulaHeaderCell.Style = dataHeaderCell.Style;

                // Получаем букву столбца, куда были скопированы данные (D, F, H, ...)
                string dataColLetter = ClosedXML.Excel.XLHelper.GetColumnLetterFromNumber(dataCol);

                // --- Диапазоны 3-47 с формулами SUM/D48 ---
                string formula1 = $"=SUM({dataColLetter}3:{dataColLetter}14)/{dataColLetter}48";
                targetWorksheet.Range(3, formulaCol, 14, formulaCol).Merge();
                targetWorksheet.Cell(3, formulaCol).FormulaA1 = formula1;

                string formula2 = $"=SUM({dataColLetter}15:{dataColLetter}21)/{dataColLetter}48";
                targetWorksheet.Range(15, formulaCol, 21, formulaCol).Merge();
                targetWorksheet.Cell(15, formulaCol).FormulaA1 = formula2;

                string formula3 = $"=SUM({dataColLetter}22:{dataColLetter}47)/{dataColLetter}48";
                targetWorksheet.Range(22, formulaCol, 47, formulaCol).Merge();
                targetWorksheet.Cell(22, formulaCol).FormulaA1 = formula3;

                // Применение форматирования
                var formulaRange = targetWorksheet.Range(3, formulaCol, 47, formulaCol);
                formulaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                formulaRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                formulaRange.Style.NumberFormat.Format = "0.00%";

                // 3. Переход к следующему столбцу для данных (пропускаем столбец с формулами)
                currentTargetColIndex += 2;
            }

            // --- ЛОГИКА: ФОРМУЛЫ СУММИРОВАНИЯ В СТОЛБЦЕ B ---

            int startDataRow = 3;
            int endDataRowForSum = 47;
            int targetSumCol = 2; // Столбец B

            for (int row = startDataRow; row <= endDataRowForSum; row++)
            {
                var formulaParts = dataColumnIndexes
                    .Select(colIndex => ClosedXML.Excel.XLHelper.GetColumnLetterFromNumber(colIndex) + row)
                    .ToList();

                string sumFormula = $"=SUM({string.Join(",", formulaParts)})";

                var targetCell = targetWorksheet.Cell(row, targetSumCol);
                targetCell.FormulaA1 = sumFormula;
                targetCell.Style = targetWorksheet.Cell(row, 1).Style;
                // УСТАНОВКА ШРИФТА 14
                targetCell.Style.Font.FontSize = 14;
            }

            // Добавляем формулу для знаменателя (B48)
            var denominatorFormulaParts = dataColumnIndexes
                .Select(colIndex => ClosedXML.Excel.XLHelper.GetColumnLetterFromNumber(colIndex) + endRow)
                .ToList();

            string denominatorSumFormula = $"=SUM({string.Join(",", denominatorFormulaParts)})";
            targetWorksheet.Cell(endRow, targetSumCol).FormulaA1 = denominatorSumFormula;
            targetWorksheet.Cell(endRow, targetSumCol).Style = targetWorksheet.Cell(endRow, 1).Style;


            // --- НОВАЯ ЛОГИКА: УСТАНОВКА ГРАНИЦ ДЛЯ ВСЕЙ ОБЛАСТИ ДАННЫХ ---

            // Последний заполненный столбец - это столбец с процентами последнего отчета,
            // который находится перед текущим (незаполненным) currentTargetColIndex.
            int lastFilledCol = currentTargetColIndex - 1;
            int firstCol = 1; // Столбец А

            // Создаем диапазон, например, от A2 до H48
            var fullRangeWithBorders = targetWorksheet.Range(startRow, firstCol, endRow, lastFilledCol);

            // Устанавливаем тонкие границы для всего диапазона (снаружи и внутри)
            fullRangeWithBorders.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            fullRangeWithBorders.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // --- КОНЕЦ НОВОЙ ЛОГИКИ ---


            InsertPeriodDescription(resultWorkbook, year, month, null, null);

            using var ms = new MemoryStream();
            resultWorkbook.SaveAs(ms);
            return ms.ToArray();
        }


        private void InsertPeriodDescription(IXLWorkbook workbook, int year, int? month, int? quarter, int? halfYear)
        {
            var worksheet = workbook.Worksheet(1); // Первый лист
            string periodText = GetPeriodText(year, month, quarter, halfYear);

            // Ищем ячейку, строка которой заканчивается на " за"
            var cell = worksheet.CellsUsed()
                .FirstOrDefault(c => c.GetString().Trim().EndsWith(" за"));

            if (cell != null)
            {
                string originalText = cell.GetString();
                string targetSubstring = " за";

                // Находим позицию ПОСЛЕДНЕГО вхождения " за"
                // StringComparison.OrdinalIgnoreCase позволяет искать без учета регистра
                int lastIndex = originalText.LastIndexOf(targetSubstring, StringComparison.OrdinalIgnoreCase);

                if (lastIndex != -1)
                {
                    // 1. Берем часть строки ДО " за"
                    string partBefore = originalText.Substring(0, lastIndex).Trim();

                    // 2. Формируем новую строку
                    cell.Value = $"{partBefore} за {periodText}";
                }
                else
                {
                    // Если " за" не найдено (хотя по условию поиска в FirstOrDefault должно быть),
                    // можно просто добавить период в конец (или ничего не делать)
                    cell.Value = $"{originalText} {periodText}";
                }
            }
        }

        private string GetPeriodText(int year, int? month, int? quarter, int? halfYear)
        {
            if (month != null)
            {
                string monthName = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetMonthName(month.Value);
                return $"{monthName.ToLower()} {year}";
            }
            else if (quarter != null)
            {
                return quarter switch
                {
                    1 => $"январь-март {year}",
                    2 => $"апрель-июнь {year}",
                    3 => $"июль-сентябрь {year}",
                    4 => $"октябрь-декабрь {year}",
                    _ => $"квартал {quarter} {year}"
                };
            }
            else if (halfYear != null)
            {
                return halfYear == 1 ? $"январь-июнь {year}" : $"июль-декабрь {year}";
            }

            return $"{year} год";
        }

        private enum ColumnAction { Key, Copy, Sum, TakeHighest }

        private class ColumnProcessingInfo
        {
            public ColumnAction Action { get; set; }
            public bool IsKey { get; set; }
        }
    }
}
