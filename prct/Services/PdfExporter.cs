using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Threading.Tasks;
using GradeBook.Services; // Убедитесь, что неймспейс совпадает

namespace GradeBook.Services
{
    public class PdfExporter : IPdfExporter
    {
        public Task ExportAsync(string path, string groupName, IEnumerable<StudentReportRow> rows)
        {
            // Установка лицензии (Community бесплатна для обучения/личного использования)
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // Заголовок документа
                    page.Header().Text($"Ведомость успеваемости группы: {groupName}")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    // Контент (Таблица)
                    page.Content().PaddingTop(10).Table(table =>
                    {
                        // Определение 4-х колонок
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3); // Студент
                            c.RelativeColumn(3); // Предмет
                            c.RelativeColumn(4); // Все оценки
                            c.RelativeColumn(2); // Средний балл
                        });

                        // Шапка таблицы
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Студент").SemiBold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Предмет").SemiBold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Все оценки").SemiBold();
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Средний").SemiBold();
                        });

                        // Заполнение данными
                        foreach (var r in rows)
                        {
                            table.Cell().BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(r.FullName);
                            table.Cell().BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(r.Subject);
                            table.Cell().BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(r.AllGrades);
                            table.Cell().BorderBottom(0.5f, Unit.Point).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(r.Average.ToString("F2"));
                        }
                    });

                    // Футер страницы с номером
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(path);

            return Task.CompletedTask;
        }
    }
}