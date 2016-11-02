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
        void Validate(object target);
        bool TryValidate(object target, ICollection<ValidationResult> validationResults);
        void ValidateFor(object target, string memberName);
        bool TryValidateFor(object target, string memberName, ICollection<ValidationResult> validationResults);
    }
}
