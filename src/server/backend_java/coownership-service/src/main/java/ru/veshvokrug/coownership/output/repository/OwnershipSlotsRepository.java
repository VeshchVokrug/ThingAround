package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.OwnershipSlot;

import java.util.UUID;

public interface OwnershipSlotsRepository extends JpaRepository<OwnershipSlot, UUID> {
}