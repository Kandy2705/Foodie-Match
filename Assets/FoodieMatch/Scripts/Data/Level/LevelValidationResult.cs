using System.Collections.Generic;

namespace FoodieMatch.Data.Level
{
    public sealed class LevelValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public bool IsValid => _errors.Count == 0;
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;

        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public void AddWarning(string message)
        {
            _warnings.Add(message);
        }
    }
}
