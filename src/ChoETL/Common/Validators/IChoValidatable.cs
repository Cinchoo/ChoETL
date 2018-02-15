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
        bool TryValidate(object target, ICollection<ValidationResult> validationResults);
        bool TryValidateFor(object target, string memberName, ICollection<ValidationResult> validationResults);
    }
}
