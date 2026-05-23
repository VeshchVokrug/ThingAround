using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Core.S3.Service;
using Gateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers.Files;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IS3StorageService _storageService;
    private readonly Random _random;
    
    private static readonly HashSet<string> AllowedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "catalog",
        "profile"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public FilesController(IS3StorageService storageService)
    {
        _storageService = storageService;
        _random = new Random();
    }

    /// <summary>
    /// Генерирует временную ссылку для прямой загрузки изображения на S3/MinIO и постоянную публичную ссылку.
    /// </summary>
    /// <param name="request">Модель запроса генерации ссылок.</param>
    [HttpPost("upload-url")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadUrlResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UploadUrlResponse> GetUploadUrl([FromBody] UploadUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest("Имя файла должно быть указано.");
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return BadRequest($"Недопустимый формат файла. Разрешены только изображения: {string.Join(", ", AllowedExtensions)}");
        }
        
        if (string.IsNullOrWhiteSpace(request.Folder) || !AllowedFolders.Contains(request.Folder))
        {
            return BadRequest($"Недопустимая папка назначения. Допустимые варианты: {string.Join(", ", AllowedFolders)}");
        }
        
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        
        var uniqueFileName = $"{_random.Next()}{extension.ToLower()}";
        var objectKey = $"users/{userId}/{request.Folder.ToLower()}/{uniqueFileName}";
        
        var (uploadUrl, publicUrl) = _storageService.GenerateUploadUrls(objectKey);

        return Ok(new UploadUrlResponse
        {
            UploadUrl = uploadUrl,
            PublicUrl = publicUrl
        });
    }
}