package ru.veshvokrug.coownership.input.dto;

import java.time.Instant;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
public record ErrorResponseDto(
    int status,
    String errorMessage,
    Instant errorTime
) {
}
