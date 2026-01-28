using DesafioApiRest.Api.Dtos.Request;
using FluentValidation;

namespace DesafioApiRest.Api.Validators;

public class FileWriteRequestDtoValidator : AbstractValidator<FileWriteRequestDto>
{
    private const int MaxContentLength = 10 * 1024 * 1024; // 10 MB
    
    public FileWriteRequestDtoValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty().WithMessage("O caminho do arquivo é obrigatório.")
            .MaximumLength(260).WithMessage("O caminho do arquivo deve ter no máximo 260 caracteres.")
            .Must(BeAValidPath).WithMessage("O caminho do arquivo contém caracteres inválidos.");

        RuleFor(x => x.Content)
            .NotNull().WithMessage("O conteúdo não pode ser nulo.")
            .Must(content => content == null || content.Length <= MaxContentLength)
            .WithMessage($"O conteúdo não pode exceder {MaxContentLength / 1024 / 1024} MB.");
    }

    private bool BeAValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        
        var invalidChars = Path.GetInvalidPathChars();
        return !path.Any(c => invalidChars.Contains(c));
    }
}
