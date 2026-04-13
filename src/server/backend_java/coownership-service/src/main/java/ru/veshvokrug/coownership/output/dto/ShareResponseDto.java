package ru.veshvokrug.coownership.output.dto;

import ru.veshvokrug.coownership.model.ShareStatus;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record ShareResponseDto(
        String shareId,
        int percentage,
        ShareStatus status) {
}
