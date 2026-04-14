package ru.veshvokrug.coownership.output.dto;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record ShareResponseDto(
        String shareId,
        int percentage,
        ShareStatus status) {
}
