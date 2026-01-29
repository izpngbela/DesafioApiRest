using DesafioApiRest.Api.Dtos.Request;
using FluentValidation;

namespace DesafioApiRest.Api.Validators;

public class FileReadRequestDtoValidator : AbstractValidator<FileReadRequestDto>
{
    public FileReadRequestDtoValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("O caminho do arquivo é obrigatório.")
            .MaximumLength(260).WithMessage("O caminho do arquivo deve ter no máximo 260 caracteres.")
            .Must(BeAValidPath).WithMessage("O caminho do arquivo contém caracteres inválidos.");
    }

    private bool BeAValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        
        var invalidChars = Path.GetInvalidPathChars();
        return !path.Any(c => invalidChars.Contains(c));
    }
}
