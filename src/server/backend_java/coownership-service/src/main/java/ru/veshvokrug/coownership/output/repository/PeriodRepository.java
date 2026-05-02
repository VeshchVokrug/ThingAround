package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.PeriodStatus;
import ru.veshvokrug.coownership.model.entity.Period;

import java.time.LocalDate;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Репозиторий расчетных периодов совладения.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public interface PeriodRepository extends JpaRepository<Period, UUID> {
	Optional<Period> findByCoownershipListing_IdAndStatus(UUID coownershipListingId, PeriodStatus status);

	Optional<Period> findByRentalListingIdAndStatus(UUID rentalListingId, PeriodStatus status);

	List<Period> findByStatusAndEndDateBefore(PeriodStatus status, LocalDate date);
}