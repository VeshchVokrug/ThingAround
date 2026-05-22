package ru.veshvokrug.coownership.output.repository;

import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import ru.veshvokrug.coownership.model.entity.ShareApplication;

import java.util.Optional;
import java.util.UUID;

/**
 * Репозиторий заявок на покупку долей в листинге.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public interface ShareApplicationRepository extends JpaRepository<ShareApplication, UUID> {
    @Lock(LockModeType.PESSIMISTIC_WRITE)
    Optional<ShareApplication> findWithLockingById(UUID id);

    Optional<ShareApplication> findByListing_IdAndApplicantId(UUID listingId, UUID applicantId);
}