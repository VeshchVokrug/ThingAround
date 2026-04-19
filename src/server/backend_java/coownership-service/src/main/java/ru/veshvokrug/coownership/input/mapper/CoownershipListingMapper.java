package ru.veshvokrug.coownership.input.mapper;

import org.springframework.stereotype.Component;
import org.springframework.web.util.HtmlUtils;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateResponseDto;
import ru.veshvokrug.coownership.input.dto.CoownershipListingDto;
import ru.veshvokrug.coownership.input.dto.ShareApplicationDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;
import ru.veshvokrug.coownership.model.entity.ShareApplication;

import java.util.List;
import java.util.stream.Collectors;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@Component
public class CoownershipListingMapper {
	public CoownershipListingCreateResponseDto toCreateResponseDto(CoownershipListing listing) {
		return new CoownershipListingCreateResponseDto(
				listing.getId(),
				listing.getFundingDeadline(),
				listing.getStatus()
		);
	}

	public CoownershipListingDto toDto(CoownershipListing listing, List<ShareApplication> shareApplications) {
		List<ShareApplicationDto> shareSlots = shareApplications == null
				? null
				: shareApplications.stream().map(this::toShareApplicationDto).collect(Collectors.toList());

		return new CoownershipListingDto(
				HtmlUtils.htmlEscape(listing.getName()),
				HtmlUtils.htmlEscape(listing.getDescription()),
				listing.getPrice(),
				listing.getOwnerId(),
				listing.getTotalShares(),
				shareSlots
		);
	}

	public ShareApplicationDto toShareApplicationDto(ShareApplication shareApplication) {
		return new ShareApplicationDto(
				shareApplication.getApplicantId(),
				shareApplication.getSharesCount(),
				shareApplication.getStatus()
		);
	}
}
