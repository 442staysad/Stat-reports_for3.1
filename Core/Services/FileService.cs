using NPOI.SS.UserModel; // Common interfaces like IWorkbook, ISheet, IRow, ICell
using NPOI.HSSF.UserModel; // For .xls files
using NPOI.XSSF.UserModel; // For .xlsx files
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Core.Interfaces;
using System.Collections.Generic; // Добавьте, если используете LINQ
//using NPOI.SS.Util; // Might be needed for advanced cell copying

namespace Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly string _rootPath;
        private readonly Dictionary<short, ICellStyle> _cellStyleMap = new Dictionary<short, ICellStyle>();
        public FileService()
        {
            _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports");

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string baseFolder, string branchName = null, int year = 0, string templateName = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Файл отсутствует или пуст");

            // Проверяем, является ли файл Excel по расширению
            string originalFileName = Path.GetFileName(file.FileName);
            string originalExtension = Path.GetExtension(originalFileName).ToLowerInvariant();

            if (originalExtension != ".xls" && originalExtension != ".xlsx")
            {
                // Можно выбросить ошибку или просто сохранить как есть, в зависимости от требований
                // throw new ArgumentException($"Файл имеет недопустимое расширение: {originalExtension}. Ожидаются .xls или .xlsx.");
                // Если нужно сохранить другие типы, то эта часть кода будет просто сохранять файл без конвертации
                // В рамках задачи "конвертировать xls в xlsx" мы предполагаем, что загружаются только Excel файлы
            }

            try
            {
                string folderPath;

                if (baseFolder == "Reports" && branchName != null && year > 0 && templateName != null)
                {
                    // Путь для отчетов: Reports/НазваниеФилиала/Год/НазваниеШаблона
                    folderPath = Path.Combine(_rootPath, baseFolder, branchName, year.ToString(), templateName);
                }
                else if (baseFolder == "Templates")
                {
                    // Путь для шаблонов: Reports/Templates
                    folderPath = Path.Combine(_rootPath, baseFolder);
                }
                else
                {
                    throw new ArgumentException("Некорректные параметры пути сохранения файла.");
                }


                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Целевое имя файла всегда с расширением .xlsx
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                string targetFileName = baseFolder == "Reports" && !string.IsNullOrEmpty(branchName)
                   ? $"{branchName}_{fileNameWithoutExtension}.xlsx"
                   : $"{fileNameWithoutExtension}.xlsx"; // Даже для шаблонов сохраняем в xlsx, если требуется


                string fullPath = Path.Combine(folderPath, targetFileName);

                // Используем MemoryStream для обработки файла в памяти
                using (var outputStream = new MemoryStream())
                {
                    using (var fileStream = file.OpenReadStream())
                    {
                        IWorkbook workbook;
                        try
                        {
                            // Читаем файл с помощью WorkbookFactory, который сам определяет формат (xls или xlsx)
                            workbook = WorkbookFactory.Create(fileStream);
                        }
                        catch (Exception ex)
                        {
                            // Ошибка чтения Excel файла (например, поврежден)
                            throw new InvalidOperationException($"Ошибка при чтении загруженного файла Excel: {ex.Message}", ex);
                        }

                        IWorkbook targetWorkbook;

                        // Если исходный файл XLS (HSSF), нужно скопировать его содержимое в новую книгу XLSX (XSSF)
                        if (workbook is HSSFWorkbook hssfWorkbook)
                        {
                            targetWorkbook = new XSSFWorkbook(); // Создаем новую книгу XLSX

                            // --- СЛОЖНАЯ ЧАСТЬ: Копирование содержимого ---
                            // Это упрощенный пример копирования только значений ячеек и структуры листов.
                            // Полное копирование форматирования, формул, изображений, графиков и т.д.
                            // значительно сложнее и требует более детальной логики или вспомогательных методов.
                            for (int i = 0; i < hssfWorkbook.NumberOfSheets; i++)
                            {
                                ISheet sourceSheet = hssfWorkbook.GetSheetAt(i);
                                ISheet destSheet = targetWorkbook.CreateSheet(sourceSheet.SheetName);

                                // Копируем объединенные ячейки (упрощенно)
                                for (int j = 0; j < sourceSheet.NumMergedRegions; j++)
                                {
                                    destSheet.AddMergedRegion(sourceSheet.GetMergedRegion(j));
                                }

                                // Копируем строки и ячейки
                                for (int rowIdx = sourceSheet.FirstRowNum; rowIdx <= sourceSheet.LastRowNum; rowIdx++)
                                {
                                    IRow sourceRow = sourceSheet.GetRow(rowIdx);
                                    if (sourceRow == null) continue;

                                    IRow destRow = destSheet.CreateRow(rowIdx);
                                    destRow.Height = sourceRow.Height; // Копируем высоту строки

                                    for (int cellIdx = sourceRow.FirstCellNum; cellIdx < sourceRow.LastCellNum; cellIdx++)
                                    {
                                        ICell sourceCell = sourceRow.GetCell(cellIdx);
                                        if (sourceCell == null) continue;

                                        ICell destCell = destRow.CreateCell(cellIdx, sourceCell.CellType);

                                        // Копируем значение ячейки в зависимости от типа
                                        switch (sourceCell.CellType)
                                        {
                                            case CellType.String:
                                                destCell.SetCellValue(sourceCell.StringCellValue);
                                                break;
                                            case CellType.Numeric:
                                                destCell.SetCellValue(sourceCell.NumericCellValue);
                                                break;
                                            case CellType.Boolean:
                                                destCell.SetCellValue(sourceCell.BooleanCellValue);
                                                break;
                                            case CellType.Formula:
                                                // Копирование формул может потребовать пересчета или быть сложным
                                                try { destCell.SetCellFormula(sourceCell.CellFormula); }
                                                catch { /* Игнорируем ошибку, если формула несовместима */ }
                                                break;
                                            case CellType.Error:
                                                destCell.SetCellErrorValue(sourceCell.ErrorCellValue);
                                                break;
                                            case CellType.Blank:
                                                // Пустая ячейка
                                                break;
                                            default:
                                                // Обработка других типов, если необходимо
                                                break;
                                        }
                                        if (sourceCell.CellStyle != null)
                                        {
                                            // Передаем ИСХОДНУЮ КНИГУ (hssfWorkbook) и ЦЕЛЕВУЮ КНИГУ (targetWorkbook)
                                            destCell.CellStyle = CopyCellStyle(hssfWorkbook, targetWorkbook, sourceCell.CellStyle);
                                        }
                                        // Копирование стилей ячеек, шрифтов и т.д. требует отдельной логики
                                        // destCell.CellStyle = sourceCell.CellStyle; // Простое присвоение может не работать с разными книгами
                                        // Придется маппировать или копировать стили явно
                                    }
                                    // Копируем ширину столбцов
                                    const int MaxColumnsToCopyWidth = 100; // Или больше/меньше по необходимости
                                    for (int colIdx = 0; colIdx < MaxColumnsToCopyWidth; colIdx++)
                                    {
                                        // GetColumnWidth вернет ширину по умолчанию, если она не задана явно для этого столбца
                                        destSheet.SetColumnWidth(colIdx, sourceSheet.GetColumnWidth(colIdx));
                                    }
                                }
                            }
                            targetWorkbook = targetWorkbook; // Теперь targetWorkbook - это новая книга XLSX с данными
                                                             // Закрываем исходную книгу, она больше не нужна
                            hssfWorkbook.Close(); 

                        }
                        else if (workbook is XSSFWorkbook xssfWorkbook)
                        {
                            // Исходный файл уже XLSX, просто используем его
                            targetWorkbook = xssfWorkbook;
                        }
                        else
                        {
                            // Неожиданный тип книги, хотя WorkbookFactory должен вернуть HSSF или XSSF для xls/xlsx
                            throw new InvalidOperationException("Не удалось определить тип книги Excel.");
                        }

                        // Сохраняем целевую книгу (теперь всегда XLSX) в MemoryStream
                        targetWorkbook.Write(outputStream, true); // true = dispose stream after writing

                        // Закрываем рабочую книгу после записи
                        targetWorkbook.Close(); // Dispose the workbook object itself

                    } // fileStream is closed

                    // Записываем байты из MemoryStream в файл на диске
                    await System.IO.File.WriteAllBytesAsync(fullPath, outputStream.ToArray());

                } // outputStream is closed

                return fullPath; // Возвращаем полный путь к сохраненному .xlsx файлу
            }
            catch (Exception ex)
            {
                // Логирование ошибки ex
                throw new InvalidOperationException($"Ошибка при обработке и сохранении файла Excel: {ex.Message}", ex);
            }
        }

        private ICellStyle CopyCellStyle(IWorkbook sourceWorkbook, IWorkbook targetWorkbook, ICellStyle sourceStyle)
        {
            if (sourceStyle == null) return null;

            // Идентификатор стиля в исходной книге (используется для кеширования)
            short sourceStyleIndex = sourceStyle.Index;

            // 1. Проверяем кеш: если стиль уже был скопирован, возвращаем его
            if (_cellStyleMap.ContainsKey(sourceStyleIndex))
            {
                return _cellStyleMap[sourceStyleIndex];
            }

            // 2. Создаем новый стиль в целевой книге
            ICellStyle newStyle = targetWorkbook.CreateCellStyle();

            // 3. Копируем все основные свойства стиля
            newStyle.Alignment = sourceStyle.Alignment;
            newStyle.VerticalAlignment = sourceStyle.VerticalAlignment;
            newStyle.BorderBottom = sourceStyle.BorderBottom;
            newStyle.BorderLeft = sourceStyle.BorderLeft;
            newStyle.BorderRight = sourceStyle.BorderRight;
            newStyle.BorderTop = sourceStyle.BorderTop;
            newStyle.BottomBorderColor = sourceStyle.BottomBorderColor;
            newStyle.LeftBorderColor = sourceStyle.LeftBorderColor;
            newStyle.RightBorderColor = sourceStyle.RightBorderColor;
            newStyle.TopBorderColor = sourceStyle.TopBorderColor;
            newStyle.FillForegroundColor = sourceStyle.FillForegroundColor;
            newStyle.FillBackgroundColor = sourceStyle.FillBackgroundColor;
            newStyle.FillPattern = sourceStyle.FillPattern;
            newStyle.DataFormat = sourceStyle.DataFormat;
            newStyle.IsHidden = sourceStyle.IsHidden;
            newStyle.IsLocked = sourceStyle.IsLocked;
            newStyle.Indention = sourceStyle.Indention;
            newStyle.WrapText = sourceStyle.WrapText;

            // 4. Копируем ШРИФТ (ИСПРАВЛЕНИЕ InvalidCastException)
            // Получаем шрифт, используя ИСХОДНУЮ книгу (HSSFWorkbook)
            IFont sourceFont = sourceStyle.GetFont(sourceWorkbook);

            if (sourceFont != null)
            {
                // Создаем новый шрифт в ЦЕЛЕВОЙ книге (XSSFWorkbook) на основе свойств исходного шрифта
                IFont newFont = targetWorkbook.CreateFont();
                newFont.IsBold = sourceFont.IsBold;
                newFont.Color = sourceFont.Color;
                newFont.FontHeightInPoints = sourceFont.FontHeightInPoints;
                newFont.FontName = sourceFont.FontName;
                newFont.IsItalic = sourceFont.IsItalic;
                newFont.IsStrikeout = sourceFont.IsStrikeout;
                newFont.TypeOffset = sourceFont.TypeOffset;
                newFont.Underline = sourceFont.Underline;

                newStyle.SetFont(newFont);
            }

            // 5. Сохраняем новый стиль в кеше перед возвратом
            _cellStyleMap.Add(sourceStyleIndex, newStyle);

            return newStyle;
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
           
            var fullPath = Path.Combine(_rootPath, filePath);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public Task<bool> DeleteFileAsync(string filePath)
        {
            // Этот метод остается прежним
            var fullPath = Path.Combine(_rootPath, filePath);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    return Task.FromResult(true);
                }
                catch
                {
                    // Ошибка удаления (например, файл используется)
                    return Task.FromResult(false); // Вернуть false при ошибке
                }
            }
            return Task.FromResult(false);
        }
    }
}