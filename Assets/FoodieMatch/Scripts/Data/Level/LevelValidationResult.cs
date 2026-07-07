using System.Collections.Generic;

namespace FoodieMatch.Data.Level
{
    public sealed class LevelValidationResult
    {
        private readonly List<string> _errors = new();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors;

        public void AddError(string message)
        {
            _errors.Add(message);
        }
    }
}
