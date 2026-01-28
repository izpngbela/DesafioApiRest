using DesafioApiRest.Api.Dtos.Request;
using FluentValidation;

namespace DesafioApiRest.Api.Validators;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("O nome de usuário é obrigatório.")
            .MinimumLength(3).WithMessage("O nome de usuário deve ter no mínimo 3 caracteres.")
            .MaximumLength(50).WithMessage("O nome de usuário deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres.")
            .MaximumLength(100).WithMessage("A senha deve ter no máximo 100 caracteres.");
    }
}
