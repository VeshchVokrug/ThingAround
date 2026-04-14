package ru.veshvokrug.coownership.output.repository;

import org.springframework.data.jpa.repository.JpaRepository;
import ru.veshvokrug.coownership.model.entity.ShareApplication;

import java.util.UUID;

public interface ShareApplicationRepository extends JpaRepository<ShareApplication, UUID> {
}