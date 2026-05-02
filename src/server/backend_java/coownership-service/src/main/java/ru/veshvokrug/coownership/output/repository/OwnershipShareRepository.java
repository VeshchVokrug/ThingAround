package ru.veshvokrug.coownership.output.repository;

import jakarta.persistence.LockModeType;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import ru.veshvokrug.coownership.model.entity.OwnershipShare;

import java.util.List;
import java.util.UUID;

/**
 * @author Dmitrii Marchenko 13.04.2026
 */
public interface OwnershipShareRepository extends JpaRepository<OwnershipShare, UUID> {
	long countByCoownershipListing_IdAndOwnerIdIsNull(UUID coownershipListingId);

	List<OwnershipShare> findByCoownershipListing_Id(UUID coownershipListingId);

	List<OwnershipShare> findByCoownershipListing_IdAndOwnerIdIsNotNullOrderByIdAsc(UUID coownershipListingId);

	@Lock(LockModeType.PESSIMISTIC_WRITE)
	@Query("""
			select s from OwnershipShare s
			where s.coownershipListing.id = :listingId and s.ownerId is null
			order by s.id
			""")
	List<OwnershipShare> findFreeSharesForUpdate(@Param("listingId") UUID listingId, Pageable pageable);
}
