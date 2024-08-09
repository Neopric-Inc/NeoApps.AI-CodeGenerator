using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nkv.MicroService.Utility
{
    public class FieldError
    {
        public String FieldName { get; set; }
        public String FieldMessage { get; set; }
    }
    public class ValidationResult
    {
        public bool IsError { get; set; }
        public string Message { get; set; } = string.Empty;

        public List<FieldError> FieldErrors { get; set; } = new List<FieldError>();

        public void AddFieldError(string fieldName, string fieldMessage)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Empty field name");

            if (string.IsNullOrWhiteSpace(fieldMessage))
                throw new ArgumentException("Empty field message");

            // appending error to existing one, if field already contains a message
            var existingFieldError = FieldErrors.FirstOrDefault(e => e.FieldName.Equals(fieldName));
            if (existingFieldError == null)
                FieldErrors.Add(new FieldError { FieldName = fieldName, FieldMessage = fieldMessage });
            else
                existingFieldError.FieldMessage = $"{existingFieldError.FieldMessage}. {fieldMessage}";

            IsError = true;
        }

        public void AddEmptyFieldError(string fieldName, string contextInfo = null)
        {
            AddFieldError(fieldName, $"No value provided for field. Context info: {contextInfo}");
        }
    }
}
