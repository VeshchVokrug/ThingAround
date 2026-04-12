package ru.veshvokrug.coownenship.controller.dto;

import ru.veshvokrug.coownenship.controller.ShareStatus;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record ShareResponseDto(String shareId,
                               int percentage,
                               ShareStatus status) {
}
