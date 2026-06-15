package ru.veshvokrug.coownership.output.repository;

import jakarta.persistence.LockModeType;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;
import ru.veshvokrug.coownership.model.CoownershipStatus;
import ru.veshvokrug.coownership.model.entity.CoownershipListing;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 13.04.2026
 */
public interface CoownershipListingRepository extends JpaRepository<CoownershipListing, UUID> {
	Optional<CoownershipListing> findByCatalogListingId(UUID catalogListingId);

	Page<CoownershipListing> findByStatusOrderByCreatedAtDesc(CoownershipStatus status, Pageable pageable);

	List<CoownershipListing> findByStatusAndFundingDeadlineBefore(CoownershipStatus status, LocalDate date);

	@Lock(LockModeType.PESSIMISTIC_READ)
	@Query("""
			select l from CoownershipListing l
			where l.id = :id
			""")
	Optional<CoownershipListing> findWithLockingById(@Param("id") UUID id);

	@Lock(LockModeType.PESSIMISTIC_WRITE)
	@Query("""
			select l from CoownershipListing l
			where l.id = :id
			""")
	Optional<CoownershipListing> findWithWriteLockingById(@Param("id") UUID id);
}
