using ClosedXML.Excel;
using Core.Interfaces;
using System;
using System.Collections.Generic;
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

            targetWorksheet.Columns().AdjustToContents();
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
            var sourceRange = signatureSheet.RangeUsed();

            int rowOffset = insertRow - sourceRange.FirstRow().RowNumber();
            int colOffset = 0;

            // 1. Копируем значения, стили, высоту строк и ширину столбцов
            foreach (var sourceRow in sourceRange.Rows())
            {
                // Получаем IXLRow (не IXLRangeRow)
                var sourceWorksheetRow = signatureSheet.Row(sourceRow.RowNumber());
                var targetRow = lastWorksheet.Row(sourceRow.RowNumber() + rowOffset);
                targetRow.Height = sourceWorksheetRow.Height;

                foreach (var sourceCell in sourceRow.Cells())
                {
                    var targetCell = lastWorksheet.Cell(targetRow.RowNumber(), sourceCell.Address.ColumnNumber + colOffset);
                    targetCell.Value = sourceCell.Value;
                    targetCell.Style = sourceCell.Style;

                    // Копируем ширину столбца (один раз)
                    var targetCol = sourceCell.Address.ColumnNumber + colOffset;
                    lastWorksheet.Column(targetCol).Width = signatureSheet.Column(sourceCell.Address.ColumnNumber).Width;
                }
            }

            // 2. Копируем объединённые ячейки
            // Внимание: берем объединения именно из signatureSheet, фильтруя те, что внутри sourceRange
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


        private void InsertPeriodDescription(XLWorkbook workbook, int year, int? month, int? quarter, int? halfYear)
        {
            // Реализуйте при необходимости
        }

        private enum ColumnAction { Key, Copy, Sum, TakeHighest }

        private class ColumnProcessingInfo
        {
            public ColumnAction Action { get; set; }
            public bool IsKey { get; set; }
        }
    }
}
