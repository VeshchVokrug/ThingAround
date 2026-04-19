package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

import java.util.Optional;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 13.04.2026
 */
public interface CoownershipListingRepository extends JpaRepository<CoownershipListing, UUID> {
	Optional<CoownershipListing> findByCatalogListingId(UUID catalogListingId);
}
