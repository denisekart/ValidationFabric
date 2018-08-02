using System.Collections.Generic;

namespace ValidationFabric
{
    public class ValidationResult
    {
        protected bool Equals(ValidationResult other)
        {
            return State == other.State && Equals(ErrorMessages, other.ErrorMessages);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) State * 397) ^ (ErrorMessages != null ? ErrorMessages.GetHashCode() : 0);
            }
        }

        public enum ValidationState
        {
            Indeterminate,
            Success,
            Failure,
        }

        public ValidationState State { get; set; }

        public List<string> ErrorMessages { get; }=new List<string>();

        public static ValidationResult Success => new ValidationResult {State = ValidationState.Success};
        public static ValidationResult Failure => new ValidationResult {State = ValidationState.Failure};
        public static ValidationResult Indeterminate => new ValidationResult {State = ValidationState.Indeterminate};

        public override bool Equals(object obj)
        {
            if (obj is ValidationResult vr)
            {
                if (vr.State != State)
                    return false;
                if (vr.State == ValidationState.Success)
                    return true;
                if (ErrorMessages.Count != vr.ErrorMessages.Count)
                    return false;
                for(int i=0; i<ErrorMessages.Count; i++)
                    if (!ErrorMessages[i].Equals(vr.ErrorMessages[i]))
                        return false;

                return true;
            }
            return false;

        }

        public static bool  operator == (ValidationResult obj1, ValidationResult obj2)
        {
            return obj1?.Equals(obj2)??false;
        }

        public static bool operator !=(ValidationResult obj1, ValidationResult obj2)
        {
            return !(obj1 == obj2);
        }
    }
}