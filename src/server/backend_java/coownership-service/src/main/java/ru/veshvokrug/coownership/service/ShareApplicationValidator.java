package ru.veshvokrug.coownership.service;

import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.web.server.ResponseStatusException;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

/**
 * Валидатор для создания заявок на участие в долевом владении.
 * <p>
 * Отвечает за:
 * - Проверку статуса листинга
 * - Проверку конфликтов (лишний владелец подает заявку на свой листинг)
 * <p>
 * Это разделение ответственности - отделяет бизнес-правила от ListingService.
 */
@Component
public class ShareApplicationValidator {

    /**
     * Валидирует возможность создания заявки на листинг.
     *
     * @param listing    CoownershipListing
     * @param requestDto CreateRequestDto
     * @throws ResponseStatusException если валидация не пройдена
     */
    public void validateCanCreateApplication(CoownershipListing listing, ShareApplicationCreateRequestDto requestDto) {
        if (listing.getStatus() != CoownershipStatus.OPEN) {
            throw new ResponseStatusException(HttpStatus.CONFLICT, "Листинг уже закрыт для заявок");
        }

        if (listing.getOwnerId().equals(requestDto.applicantId())) {
            throw new ResponseStatusException(HttpStatus.BAD_REQUEST, "Владелец не может подать заявку в свой листинг");
        }
    }

    /**
     * Валидирует что заявка может быть одобрена владельцем.
     *
     * @param listing      CoownershipListing
     * @param ownerId      ID владельца из запроса
     * @throws ResponseStatusException если не владелец
     */
    public void validateOwnerCanApprove(CoownershipListing listing, java.util.UUID ownerId) {
        if (!listing.getOwnerId().equals(ownerId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Подтверждать заявку может только владелец листинга");
        }
    }

    /**
     * Валидирует что заявка может быть отклонена владельцем.
     *
     * @param listing      CoownershipListing
     * @param ownerId      ID владельца из запроса
     * @throws ResponseStatusException если не владелец
     */
    public void validateOwnerCanReject(CoownershipListing listing, java.util.UUID ownerId) {
        if (!listing.getOwnerId().equals(ownerId)) {
            throw new ResponseStatusException(HttpStatus.FORBIDDEN, "Отклонять заявку может только владелец листинга");
        }
    }
}
