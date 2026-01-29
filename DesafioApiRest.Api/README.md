# Desafio API REST - Sistema de Autenticação JWT e Leitura de Arquivos

API RESTful desenvolvida em .NET 8.0 com autenticação JWT e sistema seguro de leitura de arquivos.

## 🚀 Funcionalidades

- **Autenticação JWT**: Sistema de login com geração de tokens JWT
- **Leitura Segura de Arquivos**: Endpoint protegido para leitura de arquivos com proteção contra path traversal
- **Swagger/OpenAPI**: Documentação interativa da API
- **Testes Unitários**: Cobertura de testes com xUnit e Moq

## 🛠️ Tecnologias Utilizadas

- **.NET 8.0**
- **ASP.NET Core Web API**
- **JWT (JSON Web Tokens)** para autenticação
- **Swagger/OpenAPI** para documentação
- **xUnit** para testes
- **Moq** para mocks em testes

## 📦 Pacotes NuGet

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.22" />
```

## 🏗️ Estrutura do Projeto

```
DesafioApiRest.Api/
├── Controllers/
│   ├── AuthController.cs      # Endpoint de autenticação
│   └── FileController.cs      # Endpoint de leitura de arquivos
├── Services/
│   ├── AuthService.cs         # Lógica de autenticação e geração de tokens
│   └── FileService.cs         # Lógica de leitura segura de arquivos
├── Interfaces/
│   ├── IAuthService.cs
│   └── IFileService.cs
├── Dtos/
│   ├── LoginDtos.cs          # DTOs de login
│   └── FileDtos.cs           # DTOs de arquivo
├── Program.cs                 # Configuração da aplicação
└── appsettings.json          # Configurações (chave JWT)

DesafioApiRest.Tests/
└── FileControllerTests.cs     # Testes unitários
```

## ⚙️ Configuração

### Pré-requisitos

- .NET SDK 8.0 ou superior
- Editor de código (VS Code, Visual Studio, Rider, etc.)

### Instalação

1. Clone o repositório:
```bash
git clone <url-do-repositorio>
cd DesafioApiRest
```

2. Restaure os pacotes:
```bash
cd DesafioApiRest.Api
dotnet restore
```

3. Configure a chave JWT no `appsettings.json` (opcional, já está configurada):
```json
{
  "Jwt": {
    "Key": "chave_super_secreta_para_teste_local_256bits"
  }
}
```

4. Execute a aplicação:
```bash
dotnet run
```

A API estará disponível em: `http://localhost:5235`

## 📝 Endpoints

### 1. Autenticação - Login

**POST** `/api/auth/login`

Gera um token JWT para autenticação.

**Request Body:**
```json
{
  "username": "admin",
  "password": "123456"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Usuário ou senha inválidos."
}
```

### 2. Leitura de Arquivos (Protegido 🔒)

**GET** `/api/file/read?path=teste.txt`

Lê o conteúdo de um arquivo. **Requer autenticação JWT**.

**Headers:**
```
Authorization: Bearer {seu_token_jwt}
```

**Query Parameters:**
- `path` (string, obrigatório): Caminho relativo do arquivo

**Response (200 OK):**
```json
{
  "fileName": "teste.txt",
  "content": "Olá! Este é o conteúdo do arquivo seguro."
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Token inválido ou ausente."
}
```

**Response (404 Not Found):**
```json
{
  "message": "Arquivo solicitado não existe."
}
```

**Response (403 Forbidden):**
Tentativa de acesso a arquivo fora da pasta permitida (path traversal bloqueado).

## 🧪 Testes

### Executar Testes

```bash
cd DesafioApiRest.Tests
dotnet test
```

### Cobertura de Testes

- ✅ `ReadFile_DeveRetornarOk_QuandoArquivoExiste`: Valida leitura bem-sucedida de arquivo

## 🔐 Segurança

### Autenticação JWT

- Token gerado com algoritmo HMAC SHA-256
- Validade de 1 hora
- Chave simétrica configurável

### Proteção de Arquivos

- Pasta base isolada: `ArquivosSeguros/`
- Proteção contra **path traversal** (tentativas de `../` são bloqueadas)
- Validação de existência de arquivo
- Tratamento de exceções de acesso

## 📖 Como Usar no Swagger

1. **Acesse o Swagger**: `http://localhost:5235/swagger`

2. **Obtenha o Token**:
   - Expanda `POST /api/auth/login`
   - Clique em "Try it out"
   - Use as credenciais:
     ```json
     {
       "username": "admin",
       "password": "123456"
     }
     ```
   - Execute e copie o `token`

3. **Autorize no Swagger**:
   - Clique no botão **🔒 Authorize** (canto superior direito)
   - Digite: `Bearer {seu_token_aqui}`
   - Clique "Authorize"

4. **Teste a Leitura de Arquivo**:
   - Expanda `GET /api/file/read`
   - Clique "Try it out"
   - Digite `path`: `teste.txt`
   - Execute → Retornará 200 OK com o conteúdo

## 🗂️ Pasta de Arquivos

A aplicação cria automaticamente uma pasta `ArquivosSeguros/` no diretório da aplicação, com um arquivo de teste `teste.txt`.

Para adicionar mais arquivos, coloque-os nesta pasta:
```
DesafioApiRest.Api/ArquivosSeguros/
└── teste.txt
└── seu_arquivo.txt
```

## 🚀 Deploy

### Build de Produção

```bash
dotnet publish -c Release -o ./publish
```

### Variáveis de Ambiente

Configure a chave JWT via variável de ambiente em produção:
```bash
export Jwt__Key="sua_chave_super_secreta_aqui_256bits"
```

## 📄 Licença

Este projeto foi desenvolvido como desafio técnico para fins educacionais.

## 👨‍💻 Autor

Desenvolvido como parte do Desafio API REST.

---

**Nota**: As credenciais padrão (`admin/123456`) são apenas para fins de demonstração. Em produção, utilize um sistema de autenticação adequado com banco de dados e hash de senhas.
