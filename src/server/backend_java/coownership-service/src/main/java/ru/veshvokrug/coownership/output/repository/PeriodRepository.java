package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.Period;

import java.util.UUID;

public interface PeriodRepository extends JpaRepository<Period, UUID> {
}