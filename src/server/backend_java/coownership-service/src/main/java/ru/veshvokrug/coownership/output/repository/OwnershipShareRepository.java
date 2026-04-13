package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.OwnershipShare;

import java.util.UUID;

/**
 * @author Dmitrii Marchenko 13.04.2026
 */
public interface OwnershipShareRepository extends JpaRepository<OwnershipShare, UUID> {
}
