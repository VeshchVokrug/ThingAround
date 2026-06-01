namespace Gateway.Models;

/// <summary>
/// Унифицированная модель ошибки HTTP API.
/// </summary>
/// <param name="StatusCode">HTTP-код ошибки.</param>
/// <param name="Message">Текстовое описание причины ошибки.</param>
public sealed record ApiErrorResponse(
	int StatusCode,
	string Message);
