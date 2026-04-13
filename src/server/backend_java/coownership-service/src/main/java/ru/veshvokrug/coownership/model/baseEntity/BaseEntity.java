package ru.veshvokrug.coownership.model.baseEntity;

import jakarta.persistence.Id;
import jakarta.persistence.MappedSuperclass;
import org.hibernate.annotations.UuidGenerator;

import java.util.UUID;

/**
 * @author Dmitrii Marchenko 13.04.2026
 */
@MappedSuperclass
public abstract class BaseEntity {
    @Id
    @UuidGenerator(style = UuidGenerator.Style.TIME)
    private UUID id;
}
