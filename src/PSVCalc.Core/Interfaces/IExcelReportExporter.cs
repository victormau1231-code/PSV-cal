using PSVCalc.Core.Enums;
using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface IExcelReportExporter
{
    string Export(ProjectRecord record, UiLanguage language, string? preferredFileName = null);
}

