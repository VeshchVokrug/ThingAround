package ru.veshvokrug.coownership.service;

import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.input.dto.ShareApplicationCreateRequestDto;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

/**
 * Валидатор бизнес-правил для создания и обработки заявок на доли.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
@Component
public class ShareApplicationValidator {

    /**
     * Валидирует возможность создания заявки на листинг.
     *
     * @param listing    CoownershipListing
     * @param requestDto CreateRequestDto
     * @throws ServiceException если валидация не пройдена
     */
    public void validateCanCreateApplication(
            CoownershipListing listing,
            ShareApplicationCreateRequestDto requestDto) {
        if (listing.getStatus() != CoownershipStatus.OPEN) {
            throw ServiceException.conflict("Листинг уже закрыт для заявок");
        }

        if (listing.getOwnerId().equals(requestDto.applicantId())) {
            throw ServiceException.badRequest("Владелец не может подать заявку в свой листинг");
        }
    }

    /**
     * Валидирует что заявка может быть одобрена владельцем.
     *
     * @param listing      CoownershipListing
     * @param ownerId      ID владельца из запроса
     * @throws ServiceException если не владелец
     */
    public void validateOwnerCanApprove(CoownershipListing listing, java.util.UUID ownerId) {
        if (!listing.getOwnerId().equals(ownerId)) {
            throw ServiceException.forbidden("Подтверждать заявку может только владелец листинга");
        }
    }

    /**
     * Валидирует что заявка может быть отклонена владельцем.
     *
     * @param listing      CoownershipListing
     * @param ownerId      ID владельца из запроса
     * @throws ServiceException если не владелец
     */
    public void validateOwnerCanReject(CoownershipListing listing, java.util.UUID ownerId) {
        if (!listing.getOwnerId().equals(ownerId)) {
            throw ServiceException.forbidden("Отклонять заявку может только владелец листинга");
        }
    }
}
