namespace DesafioApiRest.Api.Configuration;


public class FileServiceOptions
{
    public const string SectionName = "FileService";
    
    /// Caminho base onde os arquivos serão armazenados
    public string BasePath { get; set; } = "ArquivosSeguros";
    
    /// Tamanho máximo do arquivo em bytes (padrão: 10 MB)
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
}
