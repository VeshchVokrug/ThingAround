package ru.veshvokrug.coownership.input.mapper;

import org.springframework.stereotype.Component;
import ru.veshvokrug.coownership.input.dto.CoownershipListingCreateResponseDto;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

/**
 * @author Dmitrii Marchenko 19.04.2026
 */
@Component
public class CoownershipListingMapper {
	public CoownershipListingCreateResponseDto toCreateResponseDto(
			CoownershipListing listing) {
		return new CoownershipListingCreateResponseDto(
				listing.getId(),
				listing.getFundingDeadline(),
				listing.getStatus()
		);
	}
}
