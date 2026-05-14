using PSVCalc.Core.Models;

namespace PSVCalc.Core.Interfaces;

public interface IValidationCaseStore
{
    ValidationCaseSet LoadFromFile(string path);
    void SaveToFile(string path, ValidationCaseSet caseSet);
    string EnsureTemplate(string destinationDirectory, string fileName = "onsite-cases.template.json");
}

