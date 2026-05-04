package ru.veshvokrug.coownership.config;

import org.springframework.context.annotation.Configuration;
import org.springframework.data.jpa.repository.config.EnableJpaAuditing;

/**
 * Конфигурация JPA-аудита для доменных сущностей.
 *
 * @author Dmitrii Marchenko 13.04.2026
 */
@Configuration
@EnableJpaAuditing
public class JpaConfig {
}
