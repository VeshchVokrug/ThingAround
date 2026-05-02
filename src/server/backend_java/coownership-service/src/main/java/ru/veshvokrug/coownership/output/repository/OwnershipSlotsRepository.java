package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import ru.veshvokrug.coownership.model.entity.OwnershipSlot;

import java.time.LocalDate;
import java.util.List;
import java.util.UUID;

/**
 * Репозиторий слотов владения внутри расчетного периода.
 *
 * @author Dmitrii Marchenko 27.04.2026
 */
public interface OwnershipSlotsRepository extends JpaRepository<OwnershipSlot, UUID> {
    List<OwnershipSlot> findByPeriod_IdAndDateBetweenOrderByDateAsc(UUID periodId, LocalDate from, LocalDate to);

    @Query("""
            select s.ownerId as ownerId, count(s) as slotsCount
            from OwnershipSlot s
            where s.period.id = :periodId and s.status = ru.veshvokrug.coownership.model.OwnershipSlotStatus.BOOKED
            group by s.ownerId
            """)
    List<BookedSlotsByOwnerProjection> countBookedSlotsByOwner(@Param("periodId") UUID periodId);

    interface BookedSlotsByOwnerProjection {
        UUID getOwnerId();

        long getSlotsCount();
    }
}