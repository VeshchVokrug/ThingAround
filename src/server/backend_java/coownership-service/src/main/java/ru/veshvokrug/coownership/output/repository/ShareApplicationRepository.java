package ru.veshvokrug.coownership.output.repository;

import jakarta.persistence.LockModeType;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import ru.veshvokrug.coownership.model.ShareApplicationStatus;
import ru.veshvokrug.coownership.model.entity.ShareApplication;

import java.util.List;
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

    /**
     * Есть ли у пользователя заявка на листинг в статусе, отличном от указанного.
     * Используется для запрета дубликатов: REJECTED-заявки не блокируют повторную подачу.
     */
    boolean existsByListing_IdAndApplicantIdAndStatusNot(UUID listingId,
                                                         UUID applicantId,
                                                         ShareApplicationStatus status);

    List<ShareApplication> findByListing_IdAndStatus(UUID listingId, ShareApplicationStatus status);

    java.util.Optional<ShareApplication> findByListing_IdAndApplicantIdAndStatus(
            UUID listingId, UUID applicantId, ShareApplicationStatus status);
}