using ClosedXML.Excel;
using Core.Interfaces;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Core.Services
{
    public class ExcelSplitterService : IExcelSplitterService
    {
        public byte[] ProcessReports(List<string> filePaths, string templatePath, int year, int? month, int? quarter, int? halfYear)
        {
            // Создаем результатирующий файл Excel из шаблона
            using (var resultWorkbook = new XLWorkbook(templatePath))
            {
                // Загружаем все файлы с отчетами
                foreach (var filePath in filePaths)
                {
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        // Проходим по всем листам в файле отчета
                        foreach (var worksheet in workbook.Worksheets)
                        {
                            var resultWorksheet = resultWorkbook.Worksheet(worksheet.Name);

                            // Суммируем данные с этого листа и записываем в итоговый отчет
                            SumAndWriteData(worksheet, resultWorksheet);
                        }
                    }
                }
                InsertPeriodDescription(resultWorkbook, year, month, quarter, halfYear);
                // Создаем поток в памяти для сохранения итогового Excel файла
                using (var memoryStream = new MemoryStream())
                {
                    resultWorkbook.SaveAs(memoryStream);
                    return memoryStream.ToArray(); // Возвращаем файл как byte[]
                }
            }
        }

        private void SumAndWriteData(IXLWorksheet sourceWorksheet, IXLWorksheet targetWorksheet)
        {
            int firstNumericRow = -1;
            int firstNumericColumn = -1;

            // Находим первое числовое значение на листе и его строку и столбец
            foreach (var row in sourceWorksheet.RowsUsed())
            {
                foreach (var cell in row.CellsUsed())
                {
                    if (double.TryParse(cell.Value.ToString(), out _))
                    {
                        firstNumericRow = row.RowNumber();
                        firstNumericColumn = cell.Address.ColumnNumber;
                        break;
                    }
                }
                if (firstNumericRow != -1) break; // Прерываем, как только нашли первое число
            }

            // Если мы нашли числовое значение, начинаем с пропуска первой строки
            if (firstNumericRow != -1 && firstNumericColumn != -1)
            {

                foreach (var row in sourceWorksheet.RowsUsed())
                {
                    if (row.RowNumber() <= firstNumericRow) continue; // Пропускаем все строки ДО первой числовой строки

                    foreach (var cell in row.CellsUsed().Skip(2)) // Пропускаем первые два столбца (код, ед. измерения)
                    {
                        // Пробуем распарсить значение ячейки как число
                        if (double.TryParse(cell.GetValue<string>().Replace(",", ".").Replace("\u00A0", "")
                                    .Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out double sourceValue))
                        {
                            var targetCell = targetWorksheet.Cell(cell.Address);

                            // Проверяем, является ли целевая ячейка числом (иначе там может быть текст)
                            if (double.TryParse(targetCell.GetValue<string>().Replace(",", ".").Replace("\u00A0", "")
                                    .Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out double targetValue))
                            {
                                targetCell.Value = targetValue + sourceValue; // Суммируем
                            }
                            else
                            {
                                targetCell.Value = sourceValue; // Записываем новое значение, если ранее было пусто
                            }


                        }
                    }
                }
            }
        }
        private void InsertPeriodDescription(IXLWorkbook workbook, int year, int? month, int? quarter, int? halfYear)
        {
            var worksheet = workbook.Worksheet(1); // Первый лист
            string periodText = GetPeriodText(year, month, quarter, halfYear);

            // Найдем первую строку с текстом "Отчет о", и заменим/допишем
            var cell = worksheet.CellsUsed()
            .FirstOrDefault(c => c.GetString().Trim().EndsWith(" за"));

            if (cell != null)
            {
                // Заменим текст
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
            else if (quarter != null)
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
            else if (halfYear != null)
            {
                return halfYear == 1 ? $"январь-июнь {year}" : $"январь-декабрь {year}";
            }

            return $"{year} год";
        }
    }
}