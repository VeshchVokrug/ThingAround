package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.OwnershipSlots;

import java.util.UUID;

public interface OwnershipSlotsRepository extends JpaRepository<OwnershipSlots, UUID> {
}