package ru.veshvokrug.coownership.input.dto;

import io.swagger.v3.oas.annotations.media.Schema;

import java.time.Instant;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
public record ErrorResponseDto(
    @Schema(
            description = "HTTP статус ошибки",
            example = "400")
    int status,
    @Schema(
            description = "Человекочитаемое сообщение об ошибке",
            example = "Дата окончания сбора должна быть от 30 дней " +
                    "до 1 года от текущей даты")
    String errorMessage,
    @Schema(
            description = "Время ошибки в UTC",
            example = "2026-04-19T18:15:30Z")
    Instant errorTime
) {
}
