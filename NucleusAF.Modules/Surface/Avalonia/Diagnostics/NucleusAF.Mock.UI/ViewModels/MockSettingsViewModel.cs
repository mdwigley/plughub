using NucleusAF.Avalonia.ViewModels;
using NucleusAF.Interfaces.Services.Configuration;
using System.ComponentModel.DataAnnotations;

namespace NucleusAF.Mock.UI.ViewModels
{
    public partial class MockSettingsViewModel : BaseViewModel
    {
        private readonly IConfigAccessorFor<NucleusMockConfig> accessor;
        private int answerToEverything = 42;

        [CustomValidation(typeof(MockSettingsViewModel), nameof(ValidateAnswer))]
        public int AnswerToEverything
        {
            get => this.answerToEverything;
            set
            {
                if (this.SetProperty(ref this.answerToEverything, value, validate: true))
                {
                    this.accessor.Set("AnswerToEverything", value);
                    this.accessor.Save();
                }
            }
        }

        public Func<object, object> ExceptionConverter { get; } = new Func<object, object>(o =>
        {
            return o is Exception ex
                ? new ValidationResult("I'm afraid that's not quite right, but don't panic, even the universe didn't have all the answers at once.", [nameof(AnswerToEverything)])
                : o;
        });


        public MockSettingsViewModel(IConfigAccessorFor<NucleusMockConfig> accessor)
        {
            this.accessor = accessor;
            this.AnswerToEverything = accessor.Get<int>(nameof(this.AnswerToEverything));
        }

        public static ValidationResult? ValidateAnswer(string? value, ValidationContext context)
        {
            return value != "42"
                ? new ValidationResult("I'm afraid that's not quite right, but don't panic, even the universe didn't have all the answers at once.", [nameof(AnswerToEverything)])
                : ValidationResult.Success;
        }
    }
}