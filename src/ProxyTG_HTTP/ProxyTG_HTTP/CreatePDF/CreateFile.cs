using Microsoft.Extensions.Logging;
using ProxyTG_HTTP.ExceptionBase;
using ProxyTG_HTTP.ModelData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraPDF.Core;
using TerraPDF.Helpers; 

namespace ProxyTG_HTTP.CreatePDF
{
    public class CreateFile
    {
        private readonly ILogger<CreateFile> _logger;

        public CreateFile(ILogger<CreateFile> logger)
        {
            _logger = logger;
        }

        public bool CreatePdfWithTwoColumns(List<LogModel> data, string filePath)
        { 
            try
            {
                if (data.Count == 0)
                    return false;

                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSize.A4);
                        page.Margin(2, Unit.Centimetre);

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.HeaderRow(header =>
                            {
                                header.Cell().Text("Log");
                                header.Cell().Text("Time");
                            });

                            foreach (var item in data)
                            {
                                table.Row(row =>
                                {
                                    row.Cell().Text(item.LogText);
                                    row.Cell().Text(item.LogDate);
                                });
                            }
                        });                 
                    });
                }).PublishPdf(filePath);
                return true;
            }
            catch(Exception ex)
            {
                ExceptionLog.LogError(ex, _logger);
                return false;
            }
        }
    }
}
