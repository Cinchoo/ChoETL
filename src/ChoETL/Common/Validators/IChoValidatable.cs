using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoValidatable
    {
        void Validate();
        bool TryValidate(ICollection<ValidationResult> validationResults);
        void ValidateFor(string memberName);
        bool TryValidateFor(string memberName, ICollection<ValidationResult> validationResults);
    }
}
