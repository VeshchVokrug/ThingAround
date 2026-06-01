package ru.veshvokrug.coownership.input.http.dto;

import java.util.List;

/**
 * Пагинированный ответ публичного каталога листингов совладения.
 */
public record PublicListingPageResponseDto(
        List<PublicListingResponseDto> items,
        int page,
        int size,
        long totalElements,
        int totalPages
) {
}

