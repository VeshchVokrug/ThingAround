package ru.veshvokrug.coownership.controller.dto;

import ru.veshvokrug.coownership.controller.ShareStatus;

/**
 * @author Dmitrii Marchenko 06.04.2026
 */
public record ShareResponseDto(String shareId, int percentage, ShareStatus status) {
}
