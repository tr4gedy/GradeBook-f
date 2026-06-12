// Services/PdfExporter.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace GradeBook.Services
{
    public class PdfExporter : IPdfExporter
    {
        public Task ExportAsync(string path, string groupName, IEnumerable<StudentReportRow> rows)
        {
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text($"Ведомость успеваемости: {groupName}")
                        .SemiBold().FontSize(16);

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Студент");
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Дисциплина");
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Оценка");
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Среднее");
                        });

                        foreach (var r in rows)
                        {
                            table.Cell().Padding(4).Text(r.FullName);
                            table.Cell().Padding(4).Text(r.Subject);
                            table.Cell().Padding(4).Text(r.Grade.ToString());
                            table.Cell().Padding(4).Text(r.Average.ToString("F2"));
                        }
                    });
                });
            }).GeneratePdf(path);

            return Task.CompletedTask;
        }
    }
}