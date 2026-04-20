package ru.veshvokrug.coownership.input.mapper;

import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.input.dto.ShareApplicationDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationResponseDto;
import ru.veshvokrug.coownership.model.entity.ShareApplication;

/**
 * Маппинг DTO, связанных с заявками на доли.
 */
@Component
public class ShareApplicationMapper {
    public ShareApplicationDto toDto(ShareApplication shareApplication) {
        return new ShareApplicationDto(
                shareApplication.getApplicantId(),
                shareApplication.getSharesCount(),
                shareApplication.getStatus()
        );
    }

    public ShareApplicationResponseDto toResponseDto(ShareApplication shareApplication) {
        return new ShareApplicationResponseDto(
                shareApplication.getId(),
                shareApplication.getListing().getId(),
                shareApplication.getApplicantId(),
                shareApplication.getSharesCount(),
                shareApplication.getStatus()
        );
    }
}
